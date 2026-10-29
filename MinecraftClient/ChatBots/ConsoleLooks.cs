using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol.Message;
using static MinecraftClient.Settings;
using Look = MinecraftClient.ChatBots.AgentBridgeText.Look;
using PartEvent = MinecraftClient.ChatBots.AgentBridgeText.PartEvent;
using Walk = MinecraftClient.ChatBots.AgentBridgeText.Walk;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The looks of a console line (docs/agent-bridge.md, "Client 4.2.4: console line looks"): <c>st</c>, spans over the
    /// line's <c>text</c> in the looks the game draws it in, and <c>ev</c>, the clicks and hovers of its parts. Both are
    /// optional and carry no text of the line: <c>text</c> stays exactly as before and is the only text a host matches,
    /// masks or searches. The line is walked from the components the server sent (with clicks and hovers) when the walk
    /// comes to exactly <c>text</c>; else from the client's own formatted text (looks only); else it has none.
    /// </summary>
    internal static partial class ConsoleLooks
    {
        /// <summary>Most spans of a line; a line with more has neither <c>st</c> nor <c>ev</c>.</summary>
        public const int MaxSpans = 64;

        /// <summary>Most distinct events of a line; the parts of any further one have no event.</summary>
        public const int MaxEvents = 8;

        /// <summary>Longest click value in UTF-16 code units (a longer one is never cut: its click is left out).</summary>
        public const int MaxValueLength = 256;

        /// <summary>Longest hover text in UTF-16 code units, its ellipsis included.</summary>
        public const int MaxHoverText = 256;

        /// <summary>Most spans of a hover text or an entity's name.</summary>
        public const int MaxHoverSpans = 16;

        /// <summary>Most lines of a hover text.</summary>
        public const int MaxHoverLines = 8;

        /// <summary>Longest entity name in UTF-16 code units, its ellipsis included.</summary>
        public const int MaxEntityName = 64;

        /// <summary>Most bytes of a line's <c>st</c> and <c>ev</c> JSON together; past it hovers, then events, then spans go.</summary>
        public const int MaxLooksBytes = 4096;

        /// <summary>Most bytes of a hover's item (afk-host's item budget): past it the looks, then lore lines from the end go.</summary>
        public const int MaxHoverItemBytes = 2048;

        /// <summary>Most distinct click and hover pairs of a line that are read.</summary>
        private const int MaxEventsRead = 32;

        private static readonly JsonWriterOptions jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
        };

        [GeneratedRegex("^[a-z0-9_.-]+:[a-z0-9_./-]+$")]
        private static partial Regex RegistryId();

        /// <summary>A line's looks: <c>st</c> and, when a part has a click or hover, <c>ev</c>, as JSON arrays.</summary>
        internal sealed record Result(string Spans, string? Events);

        /// <summary>One event as written: its click and hover objects (JSON), either or both.</summary>
        private readonly record struct Written(string? Click, string? Hover)
        {
            public bool IsEmpty => Click is null && Hover is null;
        }

        /// <summary>
        /// A chat, system or action bar line: walked from the server's components the way <c>ParseSignedChat</c> and
        /// <c>ParseText</c> put it together, else from <paramref name="messageText"/>, the client's formatted text.
        /// </summary>
        public static Result? OfChat(ChatMessage message, string? messageText, string text)
        {
            try
            {
                Walk composed = new(events: true);
                if (Compose(composed, message) && Line(composed, text) is { } looks)
                    return looks;
            }
            catch (Exception e) when (e is InvalidCastException or ArgumentException or FormatException
                                          or InvalidOperationException or KeyNotFoundException or JsonException)
            {
                // A component the composition cannot read: the client's own text is still there.
            }

            return OfText(messageText, text);
        }

        /// <summary>A title or subtitle: walked from <paramref name="raw"/> (its NBT tree or JSON text), else from <paramref name="rendered"/>.</summary>
        public static Result? OfRaw(object? raw, string? rendered, string text)
        {
            if (raw is not null)
            {
                Walk walk = new(events: true);
                walk.Piece(raw, Look.None, null, 0);
                if (Line(walk, text) is { } looks)
                    return looks;
            }

            return OfText(rendered, text);
        }

        /// <summary>Text with formatting codes (the client's own lines, a text without its component): looks only.</summary>
        public static Result? OfText(string? printed, string text)
        {
            if (string.IsNullOrEmpty(printed))
                return null;
            Walk walk = new();
            walk.Literal(printed, Look.None);
            return Line(walk, text);
        }

        /// <summary>
        /// Puts a signed message together as <see cref="ChatParser.ParseSignedChat"/> does (its decoration, else its
        /// message type), or an older line from its JSON; false when there is nothing to walk.
        /// </summary>
        private static bool Compose(Walk walk, ChatMessage message)
        {
            if (!message.isSignedChat)
            {
                walk.Piece(message.isJson ? message.content : (object)new AgentBridgeText.PlainText(message.content),
                    Look.None, null, 0);
                return true;
            }

            object sender = message.isSenderJson
                ? message.displayName ?? string.Empty
                : message.rawSender ?? new AgentBridgeText.PlainText(message.displayName ?? string.Empty);
            object content;
            if (Config.Signature.ShowModifiedChat && message.unsignedContent is not null)
                content = message.rawUnsigned ?? message.unsignedContent;
            else
                content = message.rawContent ?? (message.isJson
                    ? message.content
                    : (object)new AgentBridgeText.PlainText(message.content));
            object target = message.rawTarget ?? new AgentBridgeText.PlainText(message.teamName ?? string.Empty);

            if (ChatParser.DecorationOf(message) is { } decoration)
            {
                Look style = decoration.Style is { } styleNbt ? Walk.OwnNbt(styleNbt) : Look.None;
                List<object?> arguments = decoration.Parameters.Select(parameter => parameter switch
                {
                    ChatParser.ChatTypeParameter.Sender => sender,
                    ChatParser.ChatTypeParameter.Target => target,
                    ChatParser.ChatTypeParameter.Content => content,
                    _ => (object?)new AgentBridgeText.PlainText(string.Empty),
                }).ToList();
                walk.Translate(decoration.TranslationKey, arguments, style);
                return true;
            }

            switch (ChatParser.SignedChatType(message))
            {
                case null:
                    return false;
                case ChatParser.MessageType.RAW_MSG:
                    walk.Piece(content, Look.None, null, 0);
                    return true;
                case ChatParser.MessageType.SAY_COMMAND:
                    walk.Translate("chat.type.announcement", [sender, content], Look.None);
                    return true;
                case ChatParser.MessageType.MSG_COMMAND_INCOMING:
                    walk.Translate("commands.message.display.incoming", [sender, content], Look.None);
                    return true;
                case ChatParser.MessageType.MSG_COMMAND_OUTGOING:
                    walk.Translate("commands.message.display.outgoing", [sender, content], Look.None);
                    return true;
                case ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING:
                    walk.Translate("chat.type.team.text", [target, sender, content], Look.None);
                    return true;
                case ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING:
                    walk.Translate("chat.type.team.sent", [target, sender, content], Look.None);
                    return true;
                case ChatParser.MessageType.EMOTE_COMMAND:
                    walk.Translate("chat.type.emote", [sender, content], Look.None);
                    return true;
                default:
                    walk.Translate("chat.type.text", [sender, content], Look.None);
                    return true;
            }
        }

        /// <summary>
        /// The looks of a walked line over <paramref name="text"/> (the line's plain text, cut as <c>Plain()</c> cuts
        /// it): null when the walk does not come to exactly that text, when it has more than <see cref="MaxSpans"/>
        /// spans, or when its one span has no look and no event.
        /// </summary>
        internal static Result? Line(Walk walk, string text)
        {
            List<(int Length, string Code, PartEvent? Event)>? runs =
                AgentBridgeText.SpanRuns(walk, text, joined => ConsoleKinds.CutLine(joined, ConsoleKinds.MaxTextLength));
            if (runs is null)
                return null;

            // The events in reading order: each distinct pair read once, at most MaxEventsRead of them.
            Dictionary<PartEvent, Written> read = new();
            foreach ((_, _, PartEvent? ev) in runs)
            {
                if (ev is null || read.ContainsKey(ev))
                    continue;
                read[ev] = read.Count < MaxEventsRead ? Write(ev) : default;
            }

            List<Written> events = [];
            List<(int Length, string Code, int Event)> spans = [];
            foreach ((int length, string code, PartEvent? ev) in runs)
            {
                int index = -1;
                if (ev is not null && read[ev] is { IsEmpty: false } written)
                {
                    index = events.IndexOf(written);
                    if (index < 0 && events.Count < MaxEvents)
                    {
                        events.Add(written);
                        index = events.Count - 1;
                    }
                }

                Add(spans, length, code, index);
            }

            return Fit(spans, events);
        }

        private static void Add(List<(int Length, string Code, int Event)> spans, int length, string code, int index)
        {
            if (spans.Count > 0 && spans[^1].Code == code && spans[^1].Event == index)
                spans[^1] = (spans[^1].Length + length, code, index);
            else
                spans.Add((length, code, index));
        }

        /// <summary>The looks within the caps: past <see cref="MaxLooksBytes"/> the hovers go, then the events, then the spans.</summary>
        private static Result? Fit(List<(int Length, string Code, int Event)> spans, List<Written> events)
        {
            if (spans.Count > MaxSpans)
            {
                // Fewer spans without the events' borders can still be too many: the line then has no looks.
                return null;
            }

            if (Fits(spans, events) is { } full)
                return full;

            // The hovers go: the events that had only a hover go with them.
            List<Written> clicks = [];
            int[] map = new int[events.Count];
            for (int i = 0; i < events.Count; i++)
            {
                Written click = events[i] with { Hover = null };
                if (click.IsEmpty)
                {
                    map[i] = -1;
                    continue;
                }

                int at = clicks.IndexOf(click);
                if (at < 0)
                {
                    clicks.Add(click);
                    at = clicks.Count - 1;
                }

                map[i] = at;
            }

            List<(int Length, string Code, int Event)> clickSpans = [];
            foreach ((int length, string code, int index) in spans)
                Add(clickSpans, length, code, index < 0 ? -1 : map[index]);
            if (Fits(clickSpans, clicks) is { } withClicks)
                return withClicks;

            // Then the events.
            List<(int Length, string Code, int Event)> plain = [];
            foreach ((int length, string code, _) in spans)
                Add(plain, length, code, -1);
            return Fits(plain, []);
        }

        private static Result? Fits(List<(int Length, string Code, int Event)> spans, List<Written> events)
        {
            if (spans.Count == 0 || spans.Count > MaxSpans)
                return null;
            if (spans.Count == 1 && spans[0].Code.Length == 0 && spans[0].Event < 0)
                return null;

            string st = Json(writer =>
            {
                writer.WriteStartArray();
                foreach ((int length, string code, int index) in spans)
                {
                    writer.WriteStartArray();
                    writer.WriteNumberValue(length);
                    writer.WriteStringValue(code);
                    if (index >= 0)
                        writer.WriteNumberValue(index);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            });
            string? ev = events.Count == 0
                ? null
                : Json(writer =>
                {
                    writer.WriteStartArray();
                    foreach (Written written in events)
                    {
                        writer.WriteStartObject();
                        if (written.Click is not null)
                        {
                            writer.WritePropertyName("click");
                            writer.WriteRawValue(written.Click, skipInputValidation: true);
                        }

                        if (written.Hover is not null)
                        {
                            writer.WritePropertyName("hover");
                            writer.WriteRawValue(written.Hover, skipInputValidation: true);
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                });

            int bytes = Encoding.UTF8.GetByteCount(st) + (ev is null ? 0 : Encoding.UTF8.GetByteCount(ev));
            return bytes <= MaxLooksBytes ? new Result(st, ev) : null;
        }

        /// <summary>An event as the console reports it: its click when it may be reported, its hover when it reads.</summary>
        private static Written Write(PartEvent ev)
        {
            string? click = ev.Click is { } c ? ClickJson(c.Action, c.Value) : null;
            string? hover = ev.Hover is { } h ? HoverJson(h) : null;
            return new Written(click, hover);
        }

        /// <summary>
        /// A click the console may report: <c>run_command</c> (a command: starts with <c>/</c>), <c>suggest_command</c>,
        /// <c>open_url</c> (<c>https</c> only, with a host and no whitespace) or <c>copy_to_clipboard</c>, its value 1–256
        /// UTF-16 code units of well-formed text without a control, bidirectional or line separator character. Anything
        /// else, and a longer value (never cut: a cut command would be another command), has none.
        /// </summary>
        internal static string? ClickJson(string action, string value)
        {
            if (action is not ("run_command" or "suggest_command" or "open_url" or "copy_to_clipboard"))
                return null;
            if (value.Length is < 1 or > MaxValueLength || !Clean(value)
                || !string.Equals(AgentBridgeItems.WellFormed(value), value, StringComparison.Ordinal))
                return null;
            switch (action)
            {
                case "run_command" when value.Length < 2 || value[0] != '/':
                    return null;
                case "open_url" when !IsHttpsUrl(value):
                    return null;
            }

            return Json(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("a", action);
                writer.WriteString("v", value);
                writer.WriteEndObject();
            });
        }

        /// <summary>
        /// An address a click may open (the contract's rule, as the machine, the Worker and the apps check it):
        /// <c>https://</c>, no white space or forbidden character, no user info before the host (<c>@</c>), a port of
        /// digits only, and a host of two or more plain ASCII labels: no IDN look-alike (Unicode or <c>xn--</c>
        /// punycode), no IP address (v4, v6 or a number), no <c>localhost</c>.
        /// </summary>
        private static bool IsHttpsUrl(string value)
        {
            if (!value.StartsWith("https://", StringComparison.Ordinal) || value.Any(char.IsWhiteSpace) || !Clean(value))
                return false;
            string rest = value["https://".Length..];
            int end = rest.IndexOfAny(['/', '?', '#']);
            string authority = end < 0 ? rest : rest[..end];
            if (authority.Contains('@') || authority.StartsWith('['))
                return false;
            int colon = authority.IndexOf(':');
            string host = colon < 0 ? authority : authority[..colon];
            if (colon >= 0)
            {
                string port = authority[(colon + 1)..];
                if (port.Length is < 1 or > 5 || !port.All(char.IsAsciiDigit))
                    return false;
            }

            string[] labels = host.Split('.');
            if (labels.Length < 2 || !labels.All(IsLabel))
                return false;
            if (labels.Any(label => label.StartsWith("xn--", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (labels[^1].All(char.IsAsciiDigit))
                return false;
            string lower = host.ToLowerInvariant();
            if (lower == "localhost" || lower.EndsWith(".localhost", StringComparison.Ordinal))
                return false;
            return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && uri.UserInfo.Length == 0
                && string.Equals(uri.Host, lower, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A host label: 1 to 63 ASCII letters, digits and hyphens, a hyphen at neither end.</summary>
        private static bool IsLabel(string label) =>
            label.Length is >= 1 and <= 63
            && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            && label[0] != '-'
            && label[^1] != '-';

        /// <summary>
        /// A character no click value carries and hover texts leave out (the contract's one list, the same in the machine,
        /// the Worker and the apps): controls (C0, DEL, C1), the soft hyphen, the combining grapheme joiner, the Arabic
        /// letter mark, the Hangul and Mongolian fillers, the zero-width characters and direction marks (U+200B-U+200F),
        /// the separators, embeddings and overrides (U+2028-U+202E), the invisible operators (U+2060-U+2064), the
        /// isolates, U+FEFF and the halfwidth Hangul filler. Tag characters are <see cref="IsTag"/>'s.
        /// </summary>
        internal static bool Forbidden(char c) =>
            c < ' '
            || c is >= '\u007F' and <= '\u009F'
            || c is '\u00AD' or '\u034F' or '\u061C' or '\u115F' or '\u1160' or '\u180E' or '\u3164' or '\uFEFF' or '\uFFA0'
            || c is >= '\u200B' and <= '\u200F'
            || c is >= '\u2028' and <= '\u202E'
            || c is >= '\u2060' and <= '\u2064'
            || c is >= '\u2066' and <= '\u2069';

        /// <summary>A tag character (U+E0000-U+E007F) at <paramref name="i"/>: U+DB40 and a low half up to U+DC7F.</summary>
        internal static bool IsTag(string s, int i) =>
            s[i] == '\uDB40' && i + 1 < s.Length && s[i + 1] is >= '\uDC00' and <= '\uDC7F';

        /// <summary>No forbidden character and no tag character.</summary>
        internal static bool Clean(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (Forbidden(s[i]) || IsTag(s, i))
                    return false;
            }

            return true;
        }

        /// <summary>A hover as the console reports it (<c>{text, st?}</c>, <c>{item}</c> or <c>{entity}</c>), or null.</summary>
        private static string? HoverJson(AgentBridgeText.HoverRef hover)
        {
            Dictionary<string, object>? node = hover.Node switch
            {
                Dictionary<string, object> nbt => nbt,
                JsonObject json => NbtOf(json) as Dictionary<string, object>,
                _ => null,
            };
            if (node is null || node.GetValueOrDefault("action") is not string action)
                return null;

            // 1.21.5+: the fields inline (show_text's "value"); before: "contents", or the old "value".
            object? payload = hover.Modern
                ? action == "show_text" ? node.GetValueOrDefault("value") : node
                : node.GetValueOrDefault("contents") ?? node.GetValueOrDefault("value");
            try
            {
                return action switch
                {
                    "show_text" => HoverText(payload),
                    "show_item" when payload is Dictionary<string, object> item => HoverItem(item),
                    "show_entity" when payload is Dictionary<string, object> entity => HoverEntity(entity, hover.Modern),
                    _ => null,
                };
            }
            catch (Exception e) when (e is InvalidCastException or ArgumentException or FormatException
                                          or InvalidOperationException or KeyNotFoundException or OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// A hover's text: walked on its own (its parts' clicks and hovers left out), line breaks as <c>\n</c> and at most
        /// <see cref="MaxHoverLines"/> lines, the characters of <see cref="Forbidden"/> left out, at most
        /// <see cref="MaxHoverText"/> code units (a cut text ends with "…"), with its spans when it has a look.
        /// </summary>
        private static string? HoverText(object? component)
        {
            if (component is null)
                return null;
            Walk walk = new();
            walk.Nbt(component, Look.None, null, 0);
            if (walk.Failed)
                return null;

            StringBuilder text = new();
            List<string> codes = [];
            int lines = 1;
            bool cut = false;
            foreach ((string part, Look look, _) in walk.Runs)
            {
                string code = look.Code();
                string shown = AgentBridgeItems.WellFormed(part);
                for (int i = 0; i < shown.Length && !cut; i++)
                {
                    char c = shown[i];
                    if (c == '\r')
                    {
                        if (i + 1 < shown.Length && shown[i + 1] == '\n')
                            i++;
                        c = '\n';
                    }
                    else if (c is '\u2028' or '\u2029' or '\u000B' or '\u000C' or '\u0085')
                    {
                        c = '\n';
                    }

                    if (c == '\n')
                    {
                        if (lines == MaxHoverLines)
                        {
                            cut = true;
                            break;
                        }

                        lines++;
                    }
                    else if (Forbidden(c) || IsTag(shown, i))
                    {
                        // A tag character is two code units: both stay out.
                        if (IsTag(shown, i))
                            i++;
                        continue;
                    }

                    if (text.Length > MaxHoverText)
                    {
                        cut = true;
                        break;
                    }

                    text.Append(c);
                    codes.Add(code);
                }

                if (cut)
                    break;
            }

            // No trailing line breaks.
            while (text.Length > 0 && text[^1] == '\n')
            {
                text.Length--;
                codes.RemoveAt(codes.Count - 1);
            }

            if (text.Length > MaxHoverText)
                cut = true;
            if (cut)
            {
                int keep = Math.Min(text.Length, MaxHoverText - 1);
                if (keep > 0 && char.IsHighSurrogate(text[keep - 1]))
                    keep--;
                text.Length = keep;
                codes.RemoveRange(keep, codes.Count - keep);
                text.Append('…');
                codes.Add(codes.Count > 0 ? codes[^1] : string.Empty);
            }

            string plain = text.ToString();
            if (string.IsNullOrWhiteSpace(plain.Replace('…', ' ')))
                return null;

            List<(int Length, string Code)>? spans = CharSpans(codes, MaxHoverSpans);
            return Json(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("text", plain);
                if (spans is not null)
                {
                    writer.WritePropertyName("st");
                    AgentBridgeText.WriteSpans(writer, spans);
                }

                writer.WriteEndObject();
            });
        }

        /// <summary>Spans from one code a character; null for none, one span of no look, or more than <paramref name="max"/>.</summary>
        private static List<(int Length, string Code)>? CharSpans(List<string> codes, int max)
        {
            List<(int Length, string Code)> spans = [];
            foreach (string code in codes)
            {
                if (spans.Count > 0 && spans[^1].Code == code)
                    spans[^1] = (spans[^1].Length + 1, code);
                else
                    spans.Add((1, code));
            }

            if (spans.Count == 0 || (spans.Count == 1 && spans[0].Code.Length == 0))
                return null;
            return spans.Count <= max ? spans : null;
        }

        /// <summary>
        /// A hover's item as the bridge writes an item (<see cref="AgentBridgeItems"/>, no slot), from the 1.20.5+ form
        /// (<c>id</c>, <c>count</c>, <c>components</c>): the custom and the item name, the lore, a head's skin and name,
        /// the enchantments, the damage and the potion, with their looks. The form before (<c>tag</c>, SNBT) gives the id
        /// and count only. Shrunk to <see cref="MaxHoverItemBytes"/> as afk-host shrinks items: the looks go, then lore
        /// lines from the end.
        /// </summary>
        private static string? HoverItem(Dictionary<string, object> item)
        {
            if (Id(item.GetValueOrDefault("id")) is not { } id)
                return null;
            int count = Number(item.GetValueOrDefault("count") ?? item.GetValueOrDefault("Count")) is { } parsed
                ? (int)Math.Clamp(parsed, 1, 9999)
                : 1;
            Dictionary<string, object>? components = item.GetValueOrDefault("components") as Dictionary<string, object>;

            (string Plain, List<(int Length, string Code)>? Spans)? Text(string key, Look look)
            {
                if (components?.GetValueOrDefault(key) is not { } component)
                    return null;
                string plain = AgentBridgeItems.Plain(Rendered(component));
                return plain.Length == 0 ? null : (plain, AgentBridgeText.Spans(Component(component), look, plain));
            }

            var name = Text("minecraft:custom_name", Look.CustomName);
            var itemName = Text("minecraft:item_name", Look.None);
            List<(string Plain, List<(int Length, string Code)>? Spans)> lore = [];
            if (components?.GetValueOrDefault("minecraft:lore") is object[] lines)
            {
                foreach (object line in lines.Take(AgentBridgeItems.MaxLoreLines))
                {
                    string plain = AgentBridgeItems.Plain(Rendered(line));
                    lore.Add((plain, AgentBridgeText.Spans(Component(line), Look.Lore, plain)));
                }
            }

            string? profileName = null, skin = null;
            switch (components?.GetValueOrDefault("minecraft:profile"))
            {
                case string onlyName:
                    profileName = AgentBridgeItems.Plain(onlyName, 16);
                    break;
                case Dictionary<string, object> profile:
                    if (profile.GetValueOrDefault("name") is string profileText)
                        profileName = AgentBridgeItems.Plain(profileText, 16);
                    if (profile.GetValueOrDefault("properties") is object[] properties)
                        skin = properties.OfType<Dictionary<string, object>>()
                            .Where(p => p.GetValueOrDefault("name") as string == "textures")
                            .Select(p => AgentBridgeText.SkinHash(p.GetValueOrDefault("value") as string))
                            .FirstOrDefault(hash => hash is not null);
                    break;
            }

            Dictionary<string, int>? Enchantments(string key)
            {
                if (components?.GetValueOrDefault(key) is not Dictionary<string, object> value)
                    return null;
                Dictionary<string, object> levels = value.GetValueOrDefault("levels") as Dictionary<string, object> ?? value;
                Dictionary<string, int> found = new(StringComparer.Ordinal);
                foreach ((string enchantment, object level) in levels)
                {
                    if (RegistryId().IsMatch(enchantment) && enchantment.Length <= 64 && Number(level) is { } l and >= 1 and <= 255)
                        found[enchantment] = (int)l;
                }

                return found.Count > 0 ? found : null;
            }

            Dictionary<string, int>? ench = Enchantments("minecraft:enchantments");
            Dictionary<string, int>? stored = Enchantments("minecraft:stored_enchantments");
            long? damage = Number(components?.GetValueOrDefault("minecraft:damage"));
            long? maxDamage = Number(components?.GetValueOrDefault("minecraft:max_damage"));
            string? potion = components?.GetValueOrDefault("minecraft:potion_contents") switch
            {
                string potionId => Id(potionId),
                Dictionary<string, object> contents => Id(contents.GetValueOrDefault("potion")),
                _ => null,
            };

            string Item(bool looks, int loreLines) => Json(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartObject("item");
                writer.WriteString("id", id);
                writer.WriteNumber("count", count);
                if (name is { } n)
                {
                    writer.WriteString("name", n.Plain);
                    if (looks && n.Spans is { } spans)
                    {
                        writer.WritePropertyName("nameStyle");
                        AgentBridgeText.WriteSpans(writer, spans);
                    }
                }

                if (itemName is { } m)
                {
                    writer.WriteString("itemName", m.Plain);
                    if (looks && m.Spans is { } spans)
                    {
                        writer.WritePropertyName("itemNameStyle");
                        AgentBridgeText.WriteSpans(writer, spans);
                    }
                }

                if (loreLines > 0)
                {
                    writer.WriteStartArray("lore");
                    foreach ((string plain, _) in lore.Take(loreLines))
                        writer.WriteStringValue(plain);
                    writer.WriteEndArray();
                    if (looks && lore.Take(loreLines).Any(line => line.Spans is not null))
                    {
                        writer.WriteStartArray("loreStyle");
                        foreach ((_, List<(int Length, string Code)>? spans) in lore.Take(loreLines))
                        {
                            if (spans is null)
                                writer.WriteNullValue();
                            else
                                AgentBridgeText.WriteSpans(writer, spans);
                        }

                        writer.WriteEndArray();
                    }
                }

                if (skin is not null)
                    writer.WriteString("skin", skin);
                foreach ((string property, Dictionary<string, int>? list) in new[] { ("ench", ench), ("stored", stored) })
                {
                    if (list is null)
                        continue;
                    writer.WriteStartObject(property);
                    foreach ((string enchantment, int level) in list)
                        writer.WriteNumber(enchantment, level);
                    writer.WriteEndObject();
                }

                if (damage is { } d and >= 0 and <= int.MaxValue)
                    writer.WriteNumber("damage", d);
                if (maxDamage is { } md and >= 1 and <= int.MaxValue)
                    writer.WriteNumber("maxDamage", md);
                if (potion is not null)
                    writer.WriteString("potion", potion);
                if (profileName is { Length: > 0 })
                {
                    writer.WriteStartObject("components");
                    writer.WriteStartObject("minecraft:profile");
                    writer.WriteString("name", profileName);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            });

            // The whole object is {"item":{...}}: written as the hover, it is the hover's "item" field.
            string written = Item(true, lore.Count);
            if (Encoding.UTF8.GetByteCount(written) > MaxHoverItemBytes)
                written = Item(false, lore.Count);
            for (int keep = lore.Count - 1; keep >= 0 && Encoding.UTF8.GetByteCount(written) > MaxHoverItemBytes; keep--)
                written = Item(false, keep);
            return Encoding.UTF8.GetByteCount(written) <= MaxHoverItemBytes ? written : null;
        }

        /// <summary>A hover's entity: its type's registry id and its name with the name's look (never its UUID).</summary>
        private static string? HoverEntity(Dictionary<string, object> entity, bool modern)
        {
            // 1.21.5+: the type is "id" (and the UUID "uuid"); before, the type is "type" and "id" is the UUID.
            if (Id(entity.GetValueOrDefault(modern ? "id" : "type")) is not { } type)
                return null;

            string? name = null;
            List<(int Length, string Code)>? spans = null;
            if (entity.GetValueOrDefault("name") is { } component)
            {
                string plain = ConsoleKinds.Plain(Rendered(component), MaxEntityName);
                if (plain.Trim().Length > 0)
                {
                    name = plain;
                    Walk walk = new();
                    walk.Nbt(Component(component), Look.None, null, 0);
                    spans = AgentBridgeText.Merged(
                        AgentBridgeText.SpanRuns(walk, plain, joined => ConsoleKinds.CutLine(joined, MaxEntityName)),
                        MaxHoverSpans);
                }
            }

            return Json(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartObject("entity");
                writer.WriteString("id", type);
                if (name is not null)
                {
                    writer.WriteString("name", name);
                    if (spans is not null)
                    {
                        writer.WritePropertyName("st");
                        AgentBridgeText.WriteSpans(writer, spans);
                    }
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            });
        }

        /// <summary>A registry id as the game writes it (a missing namespace is <c>minecraft</c>), or null.</summary>
        private static string? Id(object? value)
        {
            if (value is not string { Length: > 0 and <= 64 } id)
                return null;
            string full = id.Contains(':') ? id : "minecraft:" + id;
            return full.Length <= 64 && RegistryId().IsMatch(full) ? full : null;
        }

        private static long? Number(object? value) => value switch
        {
            byte b => b,
            short s => s,
            int i => i,
            long l => l,
            double d when double.IsFinite(d) && d == Math.Floor(d) && Math.Abs(d) < 1e15 => (long)d,
            float f when float.IsFinite(f) && f == MathF.Floor(f) && Math.Abs(f) < 1e15 => (long)f,
            _ => null,
        };

        /// <summary>A text component of an item's or entity's (an NBT compound, string or list) as ChatParser writes it.</summary>
        private static string Rendered(object component) => component switch
        {
            Dictionary<string, object> nbt => ChatParser.ParseText(nbt),
            string text => text,
            object[] parts => ChatParser.ParseText(new Dictionary<string, object> { ["text"] = string.Empty, ["extra"] = parts }),
            _ => string.Empty,
        };

        /// <summary>A text component as <see cref="AgentBridgeText.Spans"/> walks NBT: a compound (a string is its text).</summary>
        private static Dictionary<string, object> Component(object component) => component switch
        {
            Dictionary<string, object> nbt => nbt,
            object[] parts => new Dictionary<string, object> { ["text"] = string.Empty, ["extra"] = parts },
            _ => new Dictionary<string, object> { ["text"] = Convert.ToString(component, CultureInfo.InvariantCulture) ?? string.Empty },
        };

        /// <summary>
        /// A JSON hover as the NBT form reads it (objects as compounds, arrays as lists, numbers as integers when they are
        /// whole, booleans as booleans), so both forms are read one way.
        /// </summary>
        private static object? NbtOf(JsonNode? node, int depth = 0)
        {
            if (depth > 64)
                throw new InvalidOperationException("nested too deep");
            switch (node)
            {
                case JsonObject obj:
                    Dictionary<string, object> compound = new(StringComparer.Ordinal);
                    foreach ((string key, JsonNode? value) in obj)
                    {
                        if (NbtOf(value, depth + 1) is { } converted)
                            compound[key] = converted;
                    }

                    return compound;
                case JsonArray array:
                    return array.Select(item => NbtOf(item, depth + 1)).Where(item => item is not null).ToArray()!;
                case JsonValue value:
                    return value.GetValueKind() switch
                    {
                        JsonValueKind.String => value.GetValue<string>(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Number when value.TryGetValue(out int i) => i,
                        JsonValueKind.Number when value.TryGetValue(out long l) => l,
                        JsonValueKind.Number => value.GetValue<double>(),
                        _ => null,
                    };
                default:
                    return null;
            }
        }

        private static string Json(Action<Utf8JsonWriter> body)
        {
            ArrayBufferWriter<byte> buffer = new(128);
            using (Utf8JsonWriter writer = new(buffer, jsonOptions))
                body(writer);
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
    }
}
