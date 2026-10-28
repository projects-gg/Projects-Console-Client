using System;
using System.Linq;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Lean mode, for many small and mostly idle clients on one machine (the cloud's afk-host sets
    /// <see cref="EnvVar"/>=1; docs/agent-bridge.md, "Lean mode"). It changes nothing the client does in game or
    /// shows; it only skips work whose result nothing in such a profile reads:
    /// <list type="bullet">
    /// <item><c>palettes</c>: the block, entity and item palettes of a feature that is off (terrain, entity handling,
    /// inventory) are built on first use instead of at every connect.</item>
    /// <item><c>socket</c>: the packet reader waits on the socket (up to <see cref="SocketWaitMilliseconds"/>)
    /// instead of waking every 10 ms to look for data.</item>
    /// <item><c>book</c>: advancements and the recipe book are neither parsed nor kept (only the
    /// <c>/achievement</c> and <c>/recipebook</c> commands and scripts read them).</item>
    /// </list>
    /// <c>MCC_LEAN=1</c> turns every part on, a comma separated list (<c>palettes,socket</c>) only the parts it names;
    /// unset (the desktop launcher's clients) the client behaves as before.
    /// </summary>
    internal static class LeanMode
    {
        public const string EnvVar = "MCC_LEAN";

        /// <summary>
        /// A developer switch for measurements: with <c>MCC_LEAN_TRACE=1</c> a palette left for later writes one
        /// technical line to stderr when it is built after all. Never set by the host.
        /// </summary>
        public const string TraceEnvVar = "MCC_LEAN_TRACE";

        /// <summary>The longest the packet reader waits on the socket before it looks again (cancellation).</summary>
        public const int SocketWaitMilliseconds = 100;

        [Flags]
        internal enum Parts
        {
            None = 0,
            Palettes = 1,
            Socket = 2,
            Book = 4,
            All = Palettes | Socket | Book,
        }

        /// <summary>The parts on for this process (tests set it).</summary>
        internal static Parts Enabled { get; set; } = Parse(Environment.GetEnvironmentVariable(EnvVar));

        internal static bool Trace { get; set; } = Environment.GetEnvironmentVariable(TraceEnvVar) == "1";

        public static bool LazyPalettes => (Enabled & Parts.Palettes) != 0;

        public static bool SocketWait => (Enabled & Parts.Socket) != 0;

        public static bool SkipBook => (Enabled & Parts.Book) != 0;

        /// <summary><c>1</c>: every part; otherwise the parts a comma separated list names (unknown names are ignored).</summary>
        internal static Parts Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Parts.None;
            if (value.Trim() == "1")
                return Parts.All;

            Parts parts = Parts.None;
            foreach (string part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                parts |= part switch
                {
                    "palettes" => Parts.Palettes,
                    "socket" => Parts.Socket,
                    "book" => Parts.Book,
                    _ => Parts.None,
                };
            }

            return parts;
        }

        /// <summary>
        /// A palette, built now (as before), or on its first use when <paramref name="lazy"/> (a palette of a feature
        /// that is off, with <c>palettes</c> on).
        /// </summary>
        internal static Lazy<T> Palette<T>(bool lazy, string name, Func<T> create) where T : class =>
            lazy
                ? new Lazy<T>(() => Built(name, create()), LazyThreadSafetyMode.ExecutionAndPublication)
                : new Lazy<T>(create());

        /// <summary>A palette left for later is built now (traced with the frames that asked for it).</summary>
        internal static T Built<T>(string name, T palette)
        {
            if (Trace)
            {
                string[] frames = Environment.StackTrace.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Console.Error.WriteLine("[lean] " + name + " palette built: " + string.Join(" < ", frames.Skip(2).Take(6)));
            }
            return palette;
        }
    }
}
