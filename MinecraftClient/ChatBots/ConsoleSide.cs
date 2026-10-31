using System;
using System.Linq;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Side events (docs/agent-bridge.md, "Client 4.2.5: side events"): the console's looks and the server's dialogs for
    /// a host that keeps the classic console. With <see cref="EnvVar"/> listing <c>looks</c> and/or <c>dialogs</c>,
    /// the launcher bridge and a valid bridge nonce, the console prints exactly what it prints without them, and the
    /// client adds lines the host never shows: a chat line's looks right before that line
    /// (<c>&lt;nonce&gt;@@MCC@@{"e":"looks","text",...}</c>, only when the printed line is that very text), and the dialog
    /// events of <see cref="ConsoleDialogs"/>. Console kinds, when they are on, already carry both: then this is off.
    /// </summary>
    internal static class ConsoleSide
    {
        public const string EnvVar = "MCC_CONSOLE_SIDE";

        public const string LooksPart = "looks";
        public const string DialogsPart = "dialogs";

        private static readonly (bool Looks, bool Dialogs) requested = Parse(
            Environment.GetEnvironmentVariable(EnvVar),
            Environment.GetEnvironmentVariable(LauncherBridge.EnableEnvVar),
            Environment.GetEnvironmentVariable(LauncherBridge.NonceEnvVar));

        /// <summary>A chat line's looks go before it, for a classic console.</summary>
        public static bool Looks { get; internal set; } = requested.Looks;

        /// <summary>The server's dialogs are reported (<see cref="ConsoleDialogs.Enabled"/>), for a classic console.</summary>
        public static bool Dialogs { get; internal set; } = requested.Dialogs;

        /// <summary>The looks of the chat line this thread is about to log, until it is printed or the scope ends.</summary>
        [ThreadStatic] private static Pending? pending;

        private sealed record Pending(string Text, ConsoleLooks.Result Looks);

        /// <summary>
        /// What <paramref name="side"/> asks for, only with the bridge and a valid nonce (the host tells a side line
        /// from text by the nonce): its comma-separated parts, unknown parts ignored.
        /// </summary>
        internal static (bool Looks, bool Dialogs) Parse(string? side, string? bridge, string? nonce)
        {
            if (string.IsNullOrEmpty(side) || string.IsNullOrEmpty(bridge) || !LauncherBridge.IsValidNonce(nonce))
                return (false, false);
            string[] parts = side.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return (parts.Contains(LooksPart), parts.Contains(DialogsPart));
        }

        /// <summary>
        /// The chat line about to be logged on this thread (<paramref name="messageText"/>, as the client renders it,
        /// after the client's signature <paramref name="mark"/>, <c>§7▌§r</c> for a system line and the like): its looks
        /// wait for the console's print of that line. The mark is part of the printed line, so the looks cover it too,
        /// in its own colour. Nothing while console kinds are on, the looks are off or the line has none.
        /// </summary>
        public static PendingScope Expect(ChatMessage message, string messageText, string mark = "")
        {
            if (!Looks || ConsoleKinds.Enabled)
                return default;
            string text = ConsoleKinds.Plain(messageText, ConsoleKinds.MaxTextLength);
            if (text.Length == 0)
                return default;
            ConsoleLooks.Result? looks;
            try
            {
                looks = ConsoleLooks.OfChat(message, messageText, text);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                looks = null;
            }

            if (looks is null)
                return default;
            if (mark.Length > 0)
            {
                if (WithMark(mark, text, looks) is not { } marked)
                    return default;
                (text, looks) = marked;
            }

            pending = new Pending(text, looks);
            return new PendingScope(true);
        }

        /// <summary>
        /// The line and its looks with the client's signature mark in front (<c>§&lt;colour&gt;▌§r</c>): the mark one
        /// span of its colour before the line's own. None when the mark is not one colour and one character, or the
        /// marked line would pass the cap.
        /// </summary>
        internal static (string Text, ConsoleLooks.Result Looks)? WithMark(string mark, string text, ConsoleLooks.Result looks)
        {
            string shown = ConsoleKinds.Plain(mark, 8);
            if (mark.Length < 3 || mark[0] != '§' || !Uri.IsHexDigit(mark[1]) || shown.Length != 1
                || text.Length + 1 > ConsoleKinds.MaxTextLength || !looks.Spans.StartsWith('[') || looks.Spans.Length < 3)
                return null;
            string code = char.ToLowerInvariant(mark[1]).ToString();
            return (shown + text, looks with { Spans = "[[1,\"" + code + "\"]," + looks.Spans[1..] });
        }

        /// <summary>Ends <see cref="Expect"/>: looks the console did not print with their line are dropped.</summary>
        public readonly struct PendingScope : IDisposable
        {
            private readonly bool set;

            internal PendingScope(bool set) => this.set = set;

            public void Dispose()
            {
                if (set)
                    pending = null;
            }
        }

        /// <summary>
        /// The console is about to print <paramref name="printed"/> (as it goes to stdout): the waiting looks go first,
        /// only when that line, as the host reads it, is exactly the text they were worked out for (a timestamp, a
        /// signature mark or a cut changes it, and then the line goes without looks). Either way they are used up.
        /// </summary>
        internal static void BeforePrint(string printed)
        {
            if (pending is not { } waiting)
                return;
            pending = null;
            if (string.Equals(ConsoleKinds.Plain(printed, ConsoleKinds.MaxTextLength), waiting.Text, StringComparison.Ordinal))
                ConsoleKinds.Output(ConsoleKinds.LinePrefix + ConsoleKinds.LooksEvent(waiting.Text, waiting.Looks));
        }
    }
}
