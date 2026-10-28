using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The process-wide out-of-band channel of <see cref="AgentBridge"/> (docs/agent-bridge.md). Events leave as one
    /// JSON object per line on the descriptor named by <see cref="FdOutEnvVar"/>; operations arrive the same way on
    /// <see cref="FdInEnvVar"/>. Nothing a server sends can reach these descriptors except as JSON-escaped field values.
    ///
    /// The channel outlives reconnects (each session gets a new client and a new bot), so event sequence numbers and
    /// operation arrival numbers only grow within a process. A slow or stuck host never blocks the game thread:
    /// events wait in a bounded queue and are dropped (and counted) when it is full.
    /// </summary>
    internal sealed class AgentBridgeChannel
    {
        public const string FdOutEnvVar = "MCC_AGENT_FD_OUT";
        public const string FdInEnvVar = "MCC_AGENT_FD_IN";

        /// <summary>Largest event line, newline excluded. Bigger events are replaced by an "err" event.</summary>
        public const int MaxEventBytes = 256 * 1024;

        /// <summary>Largest operation line, newline excluded. Longer lines are refused unread.</summary>
        public const int MaxOperationBytes = 4 * 1024;

        private const int MaxQueuedEvents = 4096;
        private const int MaxQueuedOperations = 64;

        public static AgentBridgeChannel Instance { get; } = new();

        /// <summary>Use <see cref="Instance"/>; separate instances exist for tests.</summary>
        internal AgentBridgeChannel()
        {
        }

        private static readonly JsonWriterOptions writerOptions = new()
        {
            // The reader is a program, not a browser: keep non-ASCII text readable, JSON still escapes controls.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
        };

        private readonly Lock startLock = new();
        private readonly Lock emitLock = new();
        private readonly BlockingCollection<byte[]> outbox = new(new ConcurrentQueue<byte[]>(), MaxQueuedEvents);
        private readonly ConcurrentQueue<AgentOperation> inbox = new();
        private Stream? output;
        private Stream? input;
        private bool started;
        private long sequence;
        private long arrivals;
        private long latestStop;
        private int droppedEvents;
        private volatile bool outputClosed;

        /// <summary>Arrival number of the last <c>stop</c> read (0 before the first): it stops movement at the next tick.</summary>
        public long LatestStopArrival => Interlocked.Read(ref latestStop);

        /// <summary>Both descriptors are named in the environment.</summary>
        public static bool Configured => TryGetDescriptor(FdOutEnvVar, out _) && TryGetDescriptor(FdInEnvVar, out _);

        /// <summary>The host still reads events.</summary>
        public bool IsOpen => started && !outputClosed;

        /// <summary>Sequence number of the last event put in the queue.</summary>
        public long LastSequence => Interlocked.Read(ref sequence);

        /// <summary>Arrival number of the last operation read (0 before the first).</summary>
        public long LastArrival => Interlocked.Read(ref arrivals);

        /// <summary>
        /// Open both descriptors and start the reader and writer threads. Safe to call again: the channel is opened
        /// once per process. Returns false (with a reason) when a descriptor cannot be opened.
        /// </summary>
        public bool TryStart(out string? error)
        {
            error = null;
            if (started)
                return true;

            if (!TryGetDescriptor(FdOutEnvVar, out int outFd) || !TryGetDescriptor(FdInEnvVar, out int inFd))
            {
                error = "not_configured";
                return false;
            }

            return TryStart(outFd, inFd, out error);
        }

        /// <summary>Open the given descriptors (see <see cref="TryStart(out string?)"/>).</summary>
        internal bool TryStart(int outFd, int inFd, out string? error)
        {
            lock (startLock)
            {
                error = null;
                if (started)
                    return true;

                Stream events, operations;
                try
                {
                    events = OpenDescriptor(outFd, FileAccess.Write);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
                                              or NotSupportedException)
                {
                    error = e.GetType().Name;
                    return false;
                }

                try
                {
                    operations = OpenDescriptor(inFd, FileAccess.Read);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
                                              or NotSupportedException)
                {
                    events.Dispose();
                    error = e.GetType().Name;
                    return false;
                }

                Start(events, operations);
                return true;
            }
        }

        /// <summary>Run the channel over two streams (tests use in-process pipes).</summary>
        internal void Start(Stream events, Stream operations)
        {
            lock (startLock)
            {
                if (started)
                    return;

                output = events;
                input = operations;
                started = true;
                new Thread(WriteLoop) { IsBackground = true, Name = "MCC agent bridge writer" }.Start();
                new Thread(ReadLoop) { IsBackground = true, Name = "MCC agent bridge reader" }.Start();
            }
        }

        /// <summary>
        /// Queue one event. <paramref name="body"/> writes the event's own fields; "e", "seq" and "at" come first.
        /// Safe from any thread. Returns the event's sequence number, or 0 when it was dropped. A dropped event
        /// still uses its number, so the host sees the gap.
        /// </summary>
        public long Emit(string name, Action<Utf8JsonWriter>? body = null)
        {
            if (!started || outputClosed)
                return 0;

            lock (emitLock)
            {
                long seq = ++sequence;
                byte[]? line = Serialize(name, seq, body);
                if (line is null)
                {
                    // An event that does not fit becomes an error event; the host can ask for a snapshot.
                    line = Serialize("err", seq, writer =>
                    {
                        writer.WriteString("op", "emit");
                        writer.WriteString("reason", "too_large");
                        writer.WriteString("event", name);
                    })!;
                }

                if (!outbox.TryAdd(line))
                {
                    Interlocked.Increment(ref droppedEvents);
                    return 0;
                }

                return seq;
            }
        }

        /// <summary>Take the next operation that arrived, if any.</summary>
        public bool TryTakeOperation([MaybeNullWhen(false)] out AgentOperation operation) => inbox.TryDequeue(out operation);

        /// <summary>Discard every queued operation (after a reconnect they no longer mean anything).</summary>
        public int DiscardOperations(Action<AgentOperation> onDiscarded)
        {
            int count = 0;
            while (inbox.TryDequeue(out AgentOperation? operation))
            {
                onDiscarded(operation);
                count++;
            }

            return count;
        }

        private static byte[]? Serialize(string name, long seq, Action<Utf8JsonWriter>? body)
        {
            ArrayBufferWriterWithLimit buffer = new(MaxEventBytes);
            try
            {
                using (Utf8JsonWriter writer = new(buffer, writerOptions))
                {
                    writer.WriteStartObject();
                    writer.WriteString("e", name);
                    writer.WriteNumber("seq", seq);
                    writer.WriteNumber("at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    body?.Invoke(writer);
                    writer.WriteEndObject();
                }
            }
            catch (EventTooLargeException)
            {
                return null;
            }

            byte[] line = new byte[buffer.WrittenCount + 1];
            buffer.WrittenSpan.CopyTo(line);
            line[^1] = (byte)'\n';
            return line;
        }

        private void WriteLoop()
        {
            Stream stream = output!;
            try
            {
                foreach (byte[] line in outbox.GetConsumingEnumerable())
                {
                    stream.Write(line, 0, line.Length);
                    if (outbox.Count == 0)
                    {
                        stream.Flush();
                        int dropped = Interlocked.Exchange(ref droppedEvents, 0);
                        if (dropped > 0)
                        {
                            Emit("err", writer =>
                            {
                                writer.WriteString("op", "emit");
                                writer.WriteString("reason", "overflow");
                                writer.WriteNumber("dropped", dropped);
                            });
                        }
                    }
                }
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException or NotSupportedException)
            {
                // The host is gone. The game goes on; events are discarded from now on.
                outputClosed = true;
                Debug.WriteLine(e);
            }
        }

        private void ReadLoop()
        {
            Stream stream = input!;
            byte[] lineBuffer = new byte[MaxOperationBytes];
            byte[] chunk = new byte[4096];
            int length = 0;
            bool skipping = false;

            try
            {
                while (true)
                {
                    int read = stream.Read(chunk, 0, chunk.Length);
                    if (read <= 0)
                        return; // the host closed its end: no more operations

                    for (int i = 0; i < read; i++)
                    {
                        byte b = chunk[i];
                        if (b == (byte)'\n')
                        {
                            if (skipping)
                                Refuse(null, "?", "too_long");
                            else if (length > 0)
                                Accept(lineBuffer.AsSpan(0, length));
                            length = 0;
                            skipping = false;
                        }
                        else if (!skipping)
                        {
                            if (length == lineBuffer.Length)
                                skipping = true;
                            else
                                lineBuffer[length++] = b;
                        }
                    }
                }
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException or NotSupportedException)
            {
                Debug.WriteLine(e);
            }
        }

        private void Accept(ReadOnlySpan<byte> line)
        {
            if (line.Length > 0 && line[^1] == (byte)'\r')
                line = line[..^1];
            if (line.IsEmpty)
                return;

            long arrival = Interlocked.Increment(ref arrivals);
            if (!AgentOperation.TryParse(line, arrival, out AgentOperation? operation, out string? opName,
                    out string? id, out string? reason))
            {
                Refuse(id, opName ?? "?", reason ?? "bad_request");
                return;
            }

            // A stop is never "busy": it gets in while the queue holds up to twice its size, and it stops movement at the
            // next tick, ahead of what waits (version 3).
            bool stop = operation.Op == AgentOperation.Stop;
            if (inbox.Count >= MaxQueuedOperations && !(stop && inbox.Count < 2 * MaxQueuedOperations))
            {
                Refuse(operation.Id, operation.Op, "busy");
                return;
            }

            if (stop)
                Interlocked.Exchange(ref latestStop, arrival);
            inbox.Enqueue(operation);
        }

        /// <summary>Report a refused operation.</summary>
        public void Refuse(string? id, string op, string reason)
        {
            Emit("err", writer =>
            {
                if (id is not null)
                    writer.WriteString("id", id);
                writer.WriteString("op", op);
                writer.WriteString("reason", reason);
            });
        }

        private static bool TryGetDescriptor(string envVar, out int fd)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(envVar), NumberStyles.None,
                       CultureInfo.InvariantCulture, out fd)
                   && fd > 2;
        }

        internal static FileStream OpenDescriptor(int fd, FileAccess access)
        {
            IntPtr handle = OperatingSystem.IsWindows() ? WindowsInheritedDescriptors.Resolve(fd) : fd;
            SafeFileHandle safeHandle = new(handle, ownsHandle: true);
            if (safeHandle.IsInvalid)
                throw new IOException("invalid descriptor");
            // No isAsync argument: the stream follows the handle's own mode (a parent may pass an overlapped pipe).
            // Buffer size 0: no buffering, every write goes straight to the descriptor.
            return new FileStream(safeHandle, access, 0);
        }

        private sealed class EventTooLargeException : Exception
        {
        }

        /// <summary>An output buffer that refuses to grow past a limit.</summary>
        private sealed class ArrayBufferWriterWithLimit(int limit) : System.Buffers.IBufferWriter<byte>
        {
            private byte[] buffer = new byte[1024];
            public int WrittenCount { get; private set; }
            public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, WrittenCount);

            public void Advance(int count) => WrittenCount += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return buffer.AsMemory(WrittenCount);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return buffer.AsSpan(WrittenCount);
            }

            private void Ensure(int sizeHint)
            {
                int needed = WrittenCount + Math.Max(sizeHint, 1);
                if (needed > limit)
                    throw new EventTooLargeException();
                if (needed <= buffer.Length)
                    return;

                int size = Math.Min(limit, Math.Max(needed, buffer.Length * 2));
                Array.Resize(ref buffer, size);
            }
        }
    }

    /// <summary>
    /// On Windows a parent hands extra descriptors to a child the way the C runtime does (Node.js and libuv do
    /// this for stdio entries past 2): a table in STARTUPINFO.lpReserved2 of
    /// <c>int count; byte flags[count]; HANDLE handles[count]</c>, unaligned. The agent bridge only runs this way
    /// on a developer machine; on Linux the descriptors are plain inherited fds.
    /// </summary>
    internal static class WindowsInheritedDescriptors
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetStartupInfoW")]
        private static extern void GetStartupInfo(out StartupInfo info);

        /// <summary>
        /// The OS handle a C runtime would know as <paramref name="fd"/>; when no table was passed, the number is
        /// taken as a raw inheritable handle value.
        /// </summary>
        public static IntPtr Resolve(int fd)
        {
            GetStartupInfo(out StartupInfo info);
            int tableBytes = (ushort)info.cbReserved2;
            if (info.lpReserved2 == IntPtr.Zero || tableBytes < sizeof(int))
                return fd;

            int count = Marshal.ReadInt32(info.lpReserved2);
            if (count <= fd || count > 2048 || tableBytes < sizeof(int) + count * (1 + IntPtr.Size))
                return fd;

            int offset = sizeof(int) + count + fd * IntPtr.Size;
            IntPtr handle = Marshal.ReadIntPtr(info.lpReserved2, offset);
            return handle == IntPtr.Zero || handle == new IntPtr(-1) ? fd : handle;
        }
    }
}
