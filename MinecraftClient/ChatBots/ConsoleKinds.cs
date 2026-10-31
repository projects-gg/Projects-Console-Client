using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Console kinds (docs/agent-bridge.md, "Version 2"). When the host sets <see cref="EnvVar"/>=1 together with the
    /// launcher bridge and a valid bridge nonce, every console write leaves stdout as one event line,
    /// <c>&lt;nonce&gt;@@MCC@@{"e":"line","src",...}</c>, that says where the client saw it, and boss bar packets
    /// become <c>{"e":"boss",...}</c> lines. The client only reports what it saw (the source, the chat type, the
    /// sender, its own chat parse); the host decides what kind of line it is.
    ///
    /// A server cannot forge either event: every write is one stdout line (<see cref="ConsoleIO.OneLinePerWrite"/>,
    /// line breaks as "⏎"), JSON escapes what is left, and the server never learns the nonce.
    /// </summary>
    internal static class ConsoleKinds
    {
        public const string EnvVar = "MCC_CONSOLE_KINDS";

        public const string SourceSystem = "system";
        public const string SourcePlayer = "player";
        public const string SourceOverlay = "overlay";
        public const string SourceTitle = "title";
        public const string SourceSubtitle = "subtitle";
        public const string SourceClient = "client";
        public const string SourceError = "error";

        /// <summary>Longest <c>text</c> of a line event, in UTF-16 code units; a longer line is cut and ends with "…".</summary>
        public const int MaxTextLength = 8192;

        /// <summary>Longest boss bar text, in UTF-16 code units.</summary>
        public const int MaxBossTextLength = 256;

        /// <summary>Longest <c>who</c>, in UTF-16 code units (the host keeps it only when it is a player name).</summary>
        public const int MaxNameLength = 64;

        private static readonly JsonWriterOptions jsonOptions = new()
        {
            // The reader is a program: keep non-ASCII text readable; JSON still escapes quotes and controls.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
        };

        /// <summary>
        /// Console writes are events. On only with <see cref="EnvVar"/>=1, the launcher bridge and a valid nonce (the
        /// host can tell an event from text only by the nonce).
        /// </summary>
        public static bool Enabled { get; internal set; } = IsRequested(
            Environment.GetEnvironmentVariable(EnvVar),
            Environment.GetEnvironmentVariable(LauncherBridge.EnableEnvVar),
            Environment.GetEnvironmentVariable(LauncherBridge.NonceEnvVar));

        /// <summary>Where event lines go (the launcher bridge's stdout writer; tests replace it).</summary>
        internal static Action<string> Output { get; set; } = LauncherBridge.WriteEventLine;

        /// <summary>What starts every event line: the nonce and the bridge prefix.</summary>
        internal static string LinePrefix { get; set; } = LauncherBridge.LinePrefix;

        [ThreadStatic] private static string? source;

        internal static bool IsRequested(string? kinds, string? bridge, string? nonce) =>
            kinds == "1" && !string.IsNullOrEmpty(bridge) && LauncherBridge.IsValidNonce(nonce);

        /// <summary>
        /// Console writes made on this thread until the scope is disposed come from <paramref name="src"/> (the
        /// logger's errors use <see cref="SourceError"/>); anything else is <see cref="SourceClient"/>.
        /// </summary>
        public static SourceScope Source(string src) => new(src);

        public readonly struct SourceScope : IDisposable
        {
            private readonly string? previous;

            internal SourceScope(string src)
            {
                previous = source;
                source = src;
            }

            public void Dispose() => source = previous;
        }

        /// <summary>One console write (the client's own text), as it would be printed; its looks from its formatting codes.</summary>
        public static void WriteConsole(string printed)
        {
            string text = Plain(printed, MaxTextLength);
            if (text.Length > 0)
                Output(LinePrefix + LineEvent(source ?? SourceClient, text,
                    looks: Looks(() => ConsoleLooks.OfText(printed, text))));
        }

        /// <summary>
        /// A chat, system or action bar line from the server, whether the console settings would show it or not.
        /// <paramref name="playerName"/> gives the tab list name of a sender's UUID, if known.
        /// </summary>
        public static void WriteChat(ChatMessage message, string messageText, Func<Guid, string?> playerName)
        {
            string text = Plain(messageText, MaxTextLength);
            if (text.Length == 0)
                return;

            ChatSource seen = Describe(message, messageText, playerName, ChatParser.ChatTypeName);
            Output(LinePrefix + LineEvent(seen.Src, text, seen.Ct, seen.Who, seen.Guess,
                Looks(() => ConsoleLooks.OfChat(message, messageText, text))));
        }

        /// <summary>
        /// A title or subtitle (empty texts clear the screen and are not reported); <paramref name="raw"/>, its component
        /// as the server sent it, when the protocol read one.
        /// </summary>
        public static void WriteTitle(bool subtitle, string? title, object? raw = null)
        {
            string text = Plain(title, MaxTextLength);
            if (text.Length > 0)
                Output(LinePrefix + LineEvent(subtitle ? SourceSubtitle : SourceTitle, text,
                    looks: Looks(() => ConsoleLooks.OfRaw(raw, title, text))));
        }

        /// <summary>The action bar of the protocols that send it apart from chat.</summary>
        public static void WriteActionBar(string? actionBar)
        {
            string text = Plain(actionBar, MaxTextLength);
            if (text.Length > 0)
                Output(LinePrefix + LineEvent(SourceOverlay, text,
                    looks: Looks(() => ConsoleLooks.OfText(actionBar, text))));
        }

        /// <summary>
        /// <paramref name="raw"/> (a text component as the server sent it) while the console kinds or the side looks
        /// (client 4.2.5) are on, for the line's looks with its clicks and hovers; null otherwise, so nothing is kept that
        /// nobody reads.
        /// </summary>
        internal static object? Keep(object? raw) => Enabled || ConsoleSide.Looks ? raw : null;

        /// <summary>A line's looks; none when working them out fails (the line itself is written all the same).</summary>
        private static ConsoleLooks.Result? Looks(Func<ConsoleLooks.Result?> looks)
        {
            try
            {
                return looks();
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                return null;
            }
        }

        /// <summary>A change of a boss bar (nothing for changes the host does not show, such as its flags).</summary>
        public static void WriteBoss(BossBarEvent? bossBar)
        {
            if (bossBar is not null)
                Output(LinePrefix + BossEvent(bossBar));
        }

        /// <summary>What the client saw of one chat line: its source, chat type, sender and its own parse.</summary>
        internal readonly record struct ChatSource(string Src, string? Ct, string? Who, string? Guess);

        /// <param name="message">The line as the protocol read it</param>
        /// <param name="messageText">Its text as the client renders it</param>
        /// <param name="playerName">The tab list name of a UUID, if known</param>
        /// <param name="chatTypeName">The registry id of a chat type id the server sent, if known</param>
        internal static ChatSource Describe(ChatMessage message, string messageText, Func<Guid, string?> playerName,
            Func<int, string?> chatTypeName)
        {
            string src = SourceOf(message);
            if (src == SourcePlayer)
            {
                string? ct = message.isSignedChat && message.chatTypeId >= 0
                    ? chatTypeName(message.chatTypeId)
                    : null;
                return new ChatSource(src, ct, Sender(message, ct, playerName), null);
            }

            if (src != SourceSystem)
                return new ChatSource(src, null, null, null);

            // The client's own parse, in its auto-replies' order (AutoRespond's "from").
            string verbatim = ChatBot.GetVerbatim(messageText);
            string body = string.Empty, sender = string.Empty;
            try
            {
                if (ChatBot.IsChatMessage(verbatim, ref body, ref sender))
                    return new ChatSource(src, null, Name(sender), "chat");
                sender = string.Empty;
                if (ChatBot.IsPrivateMessage(verbatim, ref body, ref sender))
                    return new ChatSource(src, null, Name(sender), "pm");
            }
            catch (Exception e) when (e is ArgumentException or RegexMatchTimeoutException)
            {
                // A chat format the client cannot read: no guess.
            }

            return new ChatSource(src, null, null, null);
        }

        /// <summary>system: a server message; player: a player's chat line (signed or not); overlay: the action bar.</summary>
        internal static string SourceOf(ChatMessage message)
        {
            if (message.isOverlay)
                return SourceOverlay;
            if (message.isSignedChat)
            {
                if (message.isSystemChat && !message.isProfileless)
                    return message.chatTypeId == 2 ? SourceOverlay : SourceSystem; // 1.19-1.19.2: 2 is game info
                return SourcePlayer;
            }

            // Before 1.19: the chat position byte (0 chat, 1 system, 2 game info).
            return message.chatTypeId switch
            {
                1 => SourceSystem,
                2 => SourceOverlay,
                _ => SourcePlayer,
            };
        }

        /// <summary>The sender of a player's line; of the bot's own private message, its recipient.</summary>
        private static string? Sender(ChatMessage message, string? ct, Func<Guid, string?> playerName)
        {
            if (ct == "minecraft:msg_command_outgoing")
                return Name(message.teamName);
            if (message.senderUUID != Guid.Empty && playerName(message.senderUUID) is { Length: > 0 } listed)
                return Name(listed);
            return Name(message.displayName);
        }

        private static string? Name(string? name)
        {
            string plain = Plain(name, MaxNameLength).Trim();
            return plain.Length > 0 ? plain : null;
        }

        internal static string LineEvent(string src, string text, string? ct = null, string? who = null, string? guess = null,
            ConsoleLooks.Result? looks = null)
        {
            return Json(writer =>
            {
                writer.WriteString("e", "line");
                writer.WriteString("src", src);
                writer.WriteString("text", text);
                if (ct is not null)
                    writer.WriteString("ct", AgentBridgeItems.WellFormed(ct));
                if (who is not null)
                    writer.WriteString("who", who);
                if (guess is not null)
                    writer.WriteString("guess", guess);
                if (looks is null)
                    return;
                // Client 4.2.4: the line's looks (docs/agent-bridge.md, "Client 4.2.4: console line looks").
                writer.WritePropertyName("st");
                writer.WriteRawValue(looks.Spans, skipInputValidation: true);
                if (looks.Events is not null)
                {
                    writer.WritePropertyName("ev");
                    writer.WriteRawValue(looks.Events, skipInputValidation: true);
                }
            });
        }

        /// <summary>
        /// A side event (client 4.2.5, <see cref="ConsoleSide"/>): the looks of the classic console line that follows,
        /// whose text is <paramref name="text"/>.
        /// </summary>
        internal static string LooksEvent(string text, ConsoleLooks.Result looks)
        {
            return Json(writer =>
            {
                writer.WriteString("e", "looks");
                writer.WriteString("text", text);
                writer.WritePropertyName("st");
                writer.WriteRawValue(looks.Spans, skipInputValidation: true);
                if (looks.Events is not null)
                {
                    writer.WritePropertyName("ev");
                    writer.WriteRawValue(looks.Events, skipInputValidation: true);
                }
            });
        }

        internal static string BossEvent(BossBarEvent bossBar)
        {
            return Json(writer =>
            {
                writer.WriteString("e", "boss");
                writer.WriteString("op", bossBar.Op);
                writer.WriteString("id", bossBar.Id.ToString("D", CultureInfo.InvariantCulture));
                if (bossBar.Text is not null)
                    writer.WriteString("text", Plain(bossBar.Text, MaxBossTextLength));
                if (bossBar.Progress is { } progress)
                    writer.WriteNumber("progress", float.IsFinite(progress) ? Math.Round(Math.Clamp(progress, 0f, 1f), 4) : 0);
                if (bossBar.Color is not null)
                    writer.WriteString("color", bossBar.Color);
            });
        }

        private static string Json(Action<Utf8JsonWriter> body)
        {
            ArrayBufferWriter<byte> buffer = new(256);
            using (Utf8JsonWriter writer = new(buffer, jsonOptions))
            {
                writer.WriteStartObject();
                body(writer);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        /// <summary>
        /// The text as the console prints it: no formatting codes, one line (breaks as "⏎"), well-formed UTF-16, at
        /// most <paramref name="maxLength"/> code units (a cut text ends with "…").
        /// </summary>
        internal static string Plain(string? text, int maxLength)
        {
            return CutLine(AgentBridgeItems.WellFormed(ConsoleIO.KeepOnOneLine(ChatBot.GetVerbatim(text))), maxLength);
        }

        /// <summary>
        /// <paramref name="plain"/> at most <paramref name="maxLength"/> code units: a longer text is cut and ends with
        /// "…", never between the halves of a surrogate pair.
        /// </summary>
        internal static string CutLine(string plain, int maxLength)
        {
            if (plain.Length <= maxLength)
                return plain;

            int end = maxLength - 1;
            if (char.IsHighSurrogate(plain[end - 1]))
                end--; // never split a surrogate pair
            return string.Concat(plain.AsSpan(0, end), "…");
        }

        /// <summary>The game's name of a boss bar color id (unknown ids are white, as the game draws them).</summary>
        internal static string BossColorName(int color) => color switch
        {
            0 => "pink",
            1 => "blue",
            2 => "red",
            3 => "green",
            4 => "yellow",
            5 => "purple",
            _ => "white",
        };
    }

    /// <summary>
    /// One boss bar change the console kinds report: <c>add</c> (text, progress, color), <c>update</c> (only what
    /// changed: progress, text or color) or <c>remove</c>.
    /// </summary>
    internal sealed record BossBarEvent(string Op, Guid Id, string? Text = null, float? Progress = null, string? Color = null);
}
