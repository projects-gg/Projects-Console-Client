using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// How the game draws a text: an item's name and lore (docs/agent-bridge.md, "Client 4.2.3") and a console line
    /// ("Client 4.2.4: console line looks", <see cref="ConsoleLooks"/>). For items, the text component
    /// walked the way the game styles it, written as spans over the plain text the bridge writes anyway
    /// (<c>nameStyle</c>, <c>itemNameStyle</c>, <c>loreStyle</c>). A span is <c>[length, code]</c>: its length in UTF-16
    /// code units and its look, a colour (<c>0</c>–<c>f</c>, or <c>#rrggbb</c>; none: the field's own colour) and then
    /// the flags that are on, in the order <c>k l m n o</c> (obfuscated, bold, strikethrough, underlined, italic).
    /// Spans never carry text: the text is the plain field's alone, so whatever the host masks there stays masked.
    /// The plain text is still ChatParser's; when this walk does not come to exactly that text, no spans are written.
    /// </summary>
    internal static partial class AgentBridgeText
    {
        /// <summary>Most spans one text keeps; a text with more is written without them.</summary>
        public const int MaxSpans = 64;

        /// <summary>Deepest component nesting walked; a deeper text is written without spans.</summary>
        private const int MaxDepth = 64;

        /// <summary>A part's look; a null setting is "not set here" (it comes from the parent).</summary>
        internal readonly record struct Look(
            string? Color,
            bool? Obfuscated,
            bool? Bold,
            bool? Strikethrough,
            bool? Underlined,
            bool? Italic)
        {
            public static readonly Look None = new(null, null, null, null, null, null);

            /// <summary>
            /// Where a lore line starts (the game's <c>ItemLore</c> style: dark purple and italic; the dark purple is the
            /// field's own colour, so it is left unset).
            /// </summary>
            public static readonly Look Lore = None with { Italic = true };

            /// <summary>Where a custom name starts: italic (<c>ItemStack.getStyledHoverName</c>).</summary>
            public static readonly Look CustomName = None with { Italic = true };

            /// <summary>This part's settings over its parent's (the game's <c>Style.applyTo</c>).</summary>
            public Look Over(Look parent) => new(
                Color ?? parent.Color,
                Obfuscated ?? parent.Obfuscated,
                Bold ?? parent.Bold,
                Strikethrough ?? parent.Strikethrough,
                Underlined ?? parent.Underlined,
                Italic ?? parent.Italic);

            /// <summary>The span code: the colour, then the flags that are on.</summary>
            public string Code()
            {
                StringBuilder code = new(Color ?? string.Empty);
                if (Obfuscated == true)
                    code.Append('k');
                if (Bold == true)
                    code.Append('l');
                if (Strikethrough == true)
                    code.Append('m');
                if (Underlined == true)
                    code.Append('n');
                if (Italic == true)
                    code.Append('o');
                return code.ToString();
            }

            /// <summary>A colour code in a text (<c>§a</c>): that colour, every flag off.</summary>
            public static Look Colored(string color) => new(color, false, false, false, false, false);
        }

        // ChatParser's Color2tag names (the game's, and MC 1.6's).
        private static readonly Dictionary<string, string> ColorNames = new(StringComparer.Ordinal)
        {
            ["black"] = "0",
            ["dark_blue"] = "1",
            ["dark_green"] = "2",
            ["dark_aqua"] = "3",
            ["dark_cyan"] = "3",
            ["dark_red"] = "4",
            ["dark_purple"] = "5",
            ["dark_magenta"] = "5",
            ["gold"] = "6",
            ["dark_yellow"] = "6",
            ["gray"] = "7",
            ["dark_gray"] = "8",
            ["blue"] = "9",
            ["green"] = "a",
            ["aqua"] = "b",
            ["cyan"] = "b",
            ["red"] = "c",
            ["light_purple"] = "d",
            ["magenta"] = "d",
            ["yellow"] = "e",
            ["white"] = "f",
        };

        /// <summary>A component's colour as a span colour: one of the 16 names, or <c>#rrggbb</c>; null otherwise.</summary>
        internal static string? ColorCode(string? color)
        {
            if (string.IsNullOrEmpty(color))
                return null;
            string lower = color.ToLowerInvariant();
            if (ColorNames.TryGetValue(lower, out string? code))
                return code;
            return lower.Length == 7 && lower[0] == '#' && lower.Skip(1).All(IsHexDigit) ? lower : null;
        }

        private static bool IsHexDigit(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f';

        /// <summary>
        /// A piece of a composed text that is text already (a signed message's plain content, a name the client rendered
        /// itself): walked as a literal with formatting codes, never read as JSON.
        /// </summary>
        internal sealed record PlainText(string Text);

        /// <summary>
        /// A part's hover as the server set it (client 4.2.4, console looks): the hover compound or object of the
        /// component that has it, shared by reference with the parts that inherit it. <see cref="Modern"/>: the 1.21.5+
        /// <c>hover_event</c> (its fields inline) rather than <c>hoverEvent</c> (<c>contents</c>, or <c>value</c>).
        /// </summary>
        internal sealed class HoverRef(object node, bool modern)
        {
            public object Node { get; } = node;
            public bool Modern { get; } = modern;
        }

        /// <summary>
        /// A part's click and hover (client 4.2.4, console looks): its own, else its parent's, each on its own (the game's
        /// <c>Style.applyTo</c>). A click of an action the console never reports still hides the parent's.
        /// </summary>
        internal sealed record PartEvent(ChatClick? Click, HoverRef? Hover);

        /// <summary>A click that hides its parent's and is never reported (no action, or one that does not read).</summary>
        private static readonly ChatClick NoClick = new(string.Empty, string.Empty);

        /// <summary>Most runs one walk keeps; a text of more parts is written without a look.</summary>
        private const int MaxRuns = 16384;

        internal sealed class Walk(bool events = false)
        {
            public readonly List<(string Text, Look Look, PartEvent? Event)> Runs = [];

            /// <summary>
            /// The walk cannot stand for the text: nesting deeper than <see cref="MaxDepth"/>, more than
            /// <see cref="MaxRuns"/> parts, or a translation ChatParser cannot fill.
            /// </summary>
            public bool Failed;

            /// <summary>
            /// A literal with the game's formatting codes taken out and applied (<c>StringDecomposer</c>): a colour code
            /// sets the colour and turns every flag off, a flag code turns its flag on, <c>§r</c> goes back to the
            /// literal's own look; the part's click and hover stay (<c>Style.applyLegacyFormat</c>). Which characters a
            /// code takes is <c>ChatBot.GetVerbatim</c>'s rule (MCC's <c>§#rrggbb</c> included), so the text stays the
            /// bridge's plain text.
            /// </summary>
            public void Literal(string text, Look look, PartEvent? ev = null)
            {
                Look current = look;
                StringBuilder part = new();
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c != '§')
                    {
                        part.Append(c);
                        continue;
                    }

                    Look next;
                    if (i + 1 < text.Length && text[i + 1] == '#' && i + 7 < text.Length)
                    {
                        string? hex = ColorCode(text.Substring(i + 1, 7));
                        next = hex is null ? current : Look.Colored(hex);
                        i += 7;
                    }
                    else
                    {
                        char code = i + 1 < text.Length ? char.ToLowerInvariant(text[i + 1]) : '\0';
                        next = code switch
                        {
                            >= '0' and <= '9' or >= 'a' and <= 'f' => Look.Colored(code.ToString()),
                            'k' => current with { Obfuscated = true },
                            'l' => current with { Bold = true },
                            'm' => current with { Strikethrough = true },
                            'n' => current with { Underlined = true },
                            'o' => current with { Italic = true },
                            'r' => look,
                            _ => current,
                        };
                        i++;
                    }

                    if (next != current)
                    {
                        Flush(part, current, ev);
                        current = next;
                    }
                }

                Flush(part, current, ev);
            }

            private void Flush(StringBuilder part, Look look, PartEvent? ev)
            {
                if (part.Length == 0)
                    return;
                if (Runs.Count >= MaxRuns)
                {
                    Failed = true;
                }
                else
                {
                    Runs.Add((part.ToString(), look, ev));
                }

                part.Clear();
            }

            /// <summary>
            /// A translation: its rule cut where ChatParser puts the arguments, each argument walked in its own look
            /// over the component's (the game's <c>TranslatableContents</c>); the text stays ChatParser's.
            /// </summary>
            private void Translate(string key, int arguments, Look look, PartEvent? ev, Action<int> argument)
            {
                List<ChatParser.TranslationPiece>? pieces =
                    ChatParser.TranslationPieces(ChatParser.TranslationRule(key), arguments);
                if (pieces is null)
                {
                    Failed = true;
                    return;
                }

                foreach (ChatParser.TranslationPiece piece in pieces)
                {
                    if (piece.Text is not null)
                        Literal(piece.Text, look, ev);
                    else
                        argument(piece.Argument);
                }
            }

            /// <summary>
            /// A translation over pieces of a composed line (a chat type's decoration): each argument is an NBT tree,
            /// JSON text or <see cref="PlainText"/>, walked over <paramref name="look"/>.
            /// </summary>
            public void Translate(string key, IReadOnlyList<object?> arguments, Look look)
            {
                Translate(key, arguments.Count, look, null, i => Piece(arguments[i], look, null, 1));
            }

            /// <summary>One piece of a composed line: an NBT tree, JSON text (a string) or <see cref="PlainText"/>.</summary>
            public void Piece(object? raw, Look parent, PartEvent? parentEvent, int depth)
            {
                switch (raw)
                {
                    case string json:
                        Json(global::MinecraftClient.Json.ParseJson(json), parent, parentEvent, depth);
                        return;
                    default:
                        Nbt(raw, parent, parentEvent, depth);
                        return;
                }
            }

            /// <summary>An NBT text component (1.20.3+), as ChatParser.NbtToString reads it.</summary>
            public void Nbt(object? node, Look parent, PartEvent? parentEvent, int depth)
            {
                if (depth > MaxDepth)
                {
                    Failed = true;
                    return;
                }

                switch (node)
                {
                    case Dictionary<string, object> nbt:
                        if (nbt.Count == 1 && nbt.TryGetValue("", out object? root))
                        {
                            Literal(root?.ToString() ?? string.Empty, parent, parentEvent);
                            return;
                        }

                        Look look = OwnNbt(nbt).Over(parent);
                        PartEvent? ev = events ? NbtEvent(nbt, parentEvent) : null;
                        if (nbt.TryGetValue("translate", out object? translate) && translate is string key)
                        {
                            object[] withs = nbt.TryGetValue("with", out object? withValue) && withValue is object[] list
                                ? list
                                : [];
                            Translate(key, withs.Length, look, ev, i => Nbt(Part(withs[i]), look, ev, depth + 1));
                        }
                        else
                        {
                            Literal(nbt.TryGetValue("text", out object? text) ? text?.ToString() ?? string.Empty : string.Empty,
                                look, ev);
                        }

                        if (nbt.TryGetValue("extra", out object? extra) && extra is object[] extras)
                        {
                            foreach (object part in extras)
                                Nbt(Part(part), look, ev, depth + 1);
                        }

                        return;
                    case object[] parts:
                        // A list is its first part with the others after it, as that part's extras (as JSON's).
                        Look first = parent;
                        PartEvent? firstEvent = parentEvent;
                        if (parts.Length > 0 && parts[0] is Dictionary<string, object> head)
                        {
                            first = OwnNbt(head).Over(parent);
                            firstEvent = events ? NbtEvent(head, parentEvent) : null;
                        }

                        for (int i = 0; i < parts.Length; i++)
                            Nbt(Part(parts[i]), i == 0 ? parent : first, i == 0 ? parentEvent : firstEvent, depth + 1);
                        return;
                    case string literal:
                        Literal(literal, parent, parentEvent);
                        return;
                    case PlainText plain:
                        Literal(plain.Text, parent, parentEvent);
                        return;
                    case null:
                        return;
                    default:
                        Literal(Convert.ToString(node, CultureInfo.InvariantCulture) ?? string.Empty, parent, parentEvent);
                        return;
                }
            }

            /// <summary>An extra or an argument: a number or a string is a text of its own (ChatParser's rule).</summary>
            private static object Part(object part) => part switch
            {
                int number => new Dictionary<string, object> { ["text"] = number.ToString(CultureInfo.InvariantCulture) },
                string text => new Dictionary<string, object> { ["text"] = text },
                _ => part,
            };

            /// <summary>
            /// A 1.21.5+ click (<c>click_event</c>): its <c>run_command</c> may leave out the <c>/</c> the game adds
            /// (<c>Commands.trimOptionalPrefix</c>); with it, the command reads as every other.
            /// </summary>
            private static ChatClick? Modern(ChatClick? click) =>
                click is { Action: "run_command", Value: { Length: > 0 } command } found && command[0] != '/'
                    ? found with { Value = "/" + command }
                    : click;

            private static PartEvent? NbtEvent(Dictionary<string, object> nbt, PartEvent? parent)
            {
                ChatClick? click = parent?.Click;
                if (nbt.TryGetValue("click_event", out object? modern) && modern is Dictionary<string, object> modernClick)
                    click = Modern(ChatClicks.ReadNbtClick(modernClick)) ?? NoClick;
                else if (nbt.TryGetValue("clickEvent", out object? legacy) && legacy is Dictionary<string, object> legacyClick)
                    click = ChatClicks.ReadNbtClick(legacyClick) ?? NoClick;

                HoverRef? hover = parent?.Hover;
                if (nbt.TryGetValue("hover_event", out object? modernHover) && modernHover is Dictionary<string, object> m)
                    hover = new HoverRef(m, true);
                else if (nbt.TryGetValue("hoverEvent", out object? legacyHover) && legacyHover is Dictionary<string, object> l)
                    hover = new HoverRef(l, false);

                return Event(click, hover, parent);
            }

            private static PartEvent? JsonEvent(JsonObject obj, PartEvent? parent)
            {
                ChatClick? click = parent?.Click;
                if (obj["click_event"] is JsonObject modernClick)
                    click = Modern(ChatClicks.ReadJsonClick(modernClick)) ?? NoClick;
                else if (obj["clickEvent"] is JsonObject legacyClick)
                    click = ChatClicks.ReadJsonClick(legacyClick) ?? NoClick;

                HoverRef? hover = parent?.Hover;
                if (obj["hover_event"] is JsonObject modernHover)
                    hover = new HoverRef(modernHover, true);
                else if (obj["hoverEvent"] is JsonObject legacyHover)
                    hover = new HoverRef(legacyHover, false);

                return Event(click, hover, parent);
            }

            private static PartEvent? Event(ChatClick? click, HoverRef? hover, PartEvent? parent)
            {
                if (click is null && hover is null)
                    return null;
                if (parent is not null && parent.Click == click && ReferenceEquals(parent.Hover, hover))
                    return parent;
                return new PartEvent(click, hover);
            }

            /// <summary>A text component's own look, from its NBT compound (also a chat type decoration's style).</summary>
            internal static Look OwnNbt(Dictionary<string, object> nbt)
            {
                static bool? Flag(Dictionary<string, object> nbt, string key) =>
                    nbt.TryGetValue(key, out object? value)
                        ? value switch
                        {
                            byte b => b > 0,
                            bool b => b,
                            _ => string.Equals(value?.ToString(), "true", StringComparison.OrdinalIgnoreCase),
                        }
                        : null;

                return new Look(
                    nbt.TryGetValue("color", out object? color) ? ColorCode(color as string) : null,
                    Flag(nbt, "obfuscated"),
                    Flag(nbt, "bold"),
                    Flag(nbt, "strikethrough"),
                    Flag(nbt, "underline") ?? Flag(nbt, "underlined"),
                    Flag(nbt, "italic"));
            }

            /// <summary>A JSON text component (before 1.20.3; legacy text too), as ChatParser.JSONData2String reads it.</summary>
            public void Json(JsonNode? node, Look parent, PartEvent? parentEvent, int depth)
            {
                if (depth > MaxDepth)
                {
                    Failed = true;
                    return;
                }

                switch (node)
                {
                    case JsonObject obj:
                        Look look = OwnJson(obj).Over(parent);
                        PartEvent? ev = events ? JsonEvent(obj, parentEvent) : null;
                        if (obj.ContainsKey("text"))
                        {
                            Json(obj["text"], look, ev, depth + 1);
                        }
                        else if (obj.ContainsKey("translate"))
                        {
                            JsonArray? withs = (obj["with"] ?? obj["using"]) as JsonArray;
                            Translate(obj["translate"].GetStringValue(), withs?.Count ?? 0, look, ev,
                                i => Json(withs![i], look, ev, depth + 1));
                        }

                        if (obj["extra"] is JsonArray extras)
                        {
                            foreach (JsonNode? part in extras)
                                Json(part, look, ev, depth + 1);
                        }

                        return;
                    case JsonArray array:
                        // The game reads a list as its first part with the others after it, as that part's extras.
                        Look first = parent;
                        PartEvent? firstEvent = parentEvent;
                        if (array.Count > 0 && array[0] is JsonObject head)
                        {
                            first = OwnJson(head).Over(parent);
                            firstEvent = events ? JsonEvent(head, parentEvent) : null;
                        }

                        for (int i = 0; i < array.Count; i++)
                            Json(array[i], i == 0 ? parent : first, i == 0 ? parentEvent : firstEvent, depth + 1);
                        return;
                    default:
                        Literal(node.GetStringValue(), parent, parentEvent);
                        return;
                }
            }

            private static Look OwnJson(JsonObject obj)
            {
                static bool? Flag(JsonObject obj, string key) =>
                    obj.ContainsKey(key)
                        ? obj[key].GetStringValue() switch
                        {
                            "true" => (bool?)true,
                            "false" => false,
                            _ => null,
                        }
                        : null;

                return new Look(
                    obj.ContainsKey("color") ? ColorCode(obj["color"].GetStringValue()) : null,
                    Flag(obj, "obfuscated"),
                    Flag(obj, "bold"),
                    Flag(obj, "strikethrough"),
                    Flag(obj, "underline") ?? Flag(obj, "underlined"),
                    Flag(obj, "italic"));
            }
        }

        /// <summary>
        /// A walk's runs as spans over <paramref name="plain"/>, the bridge's plain text of the same text: each run as
        /// <c>Plain()</c> writes text (one line, well-formed), adjacent runs of one code and one event as one. Null when
        /// the runs, cut by <paramref name="cut"/>, do not come to exactly that text. A cut text's last spans cover what is
        /// left of it, an ellipsis included.
        /// </summary>
        internal static List<(int Length, string Code, PartEvent? Event)>? SpanRuns(Walk walk, string plain,
            Func<string, string> cut)
        {
            if (walk.Failed || plain.Length == 0)
                return null;

            StringBuilder joined = new();
            List<(int Length, string Code, PartEvent? Event)> raw = [];
            foreach ((string text, Look look, PartEvent? ev) in walk.Runs)
            {
                string shown = AgentBridgeItems.WellFormed(ConsoleIO.KeepOnOneLine(text));
                if (shown.Length == 0)
                    continue;
                joined.Append(shown);
                raw.Add((shown.Length, look.Code(), ev));
            }

            if (!string.Equals(cut(joined.ToString()), plain, StringComparison.Ordinal))
                return null;

            List<(int Length, string Code, PartEvent? Event)> spans = [];
            int left = plain.Length;
            foreach ((int length, string code, PartEvent? ev) in raw)
            {
                int take = Math.Min(length, left);
                if (spans.Count > 0 && spans[^1].Code == code && Equals(spans[^1].Event, ev))
                    spans[^1] = (spans[^1].Length + take, code, ev);
                else
                    spans.Add((take, code, ev));
                left -= take;
                if (left == 0)
                    break;
            }

            return spans;
        }

        /// <summary>
        /// The spans of <paramref name="component"/> (an NBT text component; or a JSON or legacy text, as the NBT of
        /// versions before 1.20.5 keeps it) starting from <paramref name="look"/>, over <paramref name="plain"/>: the
        /// bridge's plain text of the same component. Null when the walk does not come to exactly that text, when one look
        /// covers all of it and that look is the field's own, or past <see cref="MaxSpans"/> spans.
        /// </summary>
        public static List<(int Length, string Code)>? Spans(object? component, Look look, string plain)
        {
            Walk walk = new();
            switch (component)
            {
                case Dictionary<string, object> nbt:
                    walk.Nbt(nbt, look, null, 0);
                    break;
                case string text:
                    walk.Json(global::MinecraftClient.Json.ParseJson(text), look, null, 0);
                    break;
                default:
                    return null;
            }

            return Merged(SpanRuns(walk, plain, joined => AgentBridgeItems.Cut(joined, AgentBridgeItems.MaxTextLength)),
                MaxSpans);
        }

        /// <summary>
        /// Spans of one code each, adjacent ones of one code as one (events left out); null for none, for one span of
        /// the field's own look, or past <paramref name="max"/> spans.
        /// </summary>
        internal static List<(int Length, string Code)>? Merged(List<(int Length, string Code, PartEvent? Event)>? runs,
            int max)
        {
            if (runs is null)
                return null;
            List<(int Length, string Code)> spans = [];
            foreach ((int length, string code, _) in runs)
            {
                if (spans.Count > 0 && spans[^1].Code == code)
                    spans[^1] = (spans[^1].Length + length, code);
                else
                    spans.Add((length, code));
            }

            if (spans.Count == 0 || (spans.Count == 1 && spans[0].Code.Length == 0))
                return null;
            return spans.Count <= max ? spans : null;
        }

        /// <summary>Writes spans as <c>[[length, code], ...]</c>.</summary>
        public static void WriteSpans(Utf8JsonWriter writer, List<(int Length, string Code)> spans)
        {
            writer.WriteStartArray();
            foreach ((int length, string code) in spans)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(length);
                writer.WriteStringValue(code);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        /// <summary>Most characters of a <c>textures</c> property value read.</summary>
        private const int MaxTexturesValue = 16 * 1024;

        private static readonly string[] TexturePrefixes =
        [
            "https://textures.minecraft.net/texture/",
            "http://textures.minecraft.net/texture/",
        ];

        // A url after "SKIN" in a payload a lenient writer made (unquoted keys): the first texture address there.
        [GeneratedRegex(@"SKIN[^{]*\{[^}]*?(https?://textures\.minecraft\.net/texture/[0-9A-Fa-f]{16,64})")]
        private static partial Regex LenientSkinUrl();

        /// <summary>
        /// A head's skin from its profile's <c>textures</c> property (base64 JSON, <c>textures.SKIN.url</c>): the
        /// texture's hash, only for a texture on Mojang's own texture server (<c>http(s)://textures.minecraft.net/texture/</c>
        /// and 16–64 hex digits, written lower case); null for anything else. Signed or not, only the address counts.
        /// </summary>
        public static string? SkinHash(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxTexturesValue)
                return null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(Padded(value));
            }
            catch (FormatException)
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("textures", out JsonElement textures) && textures.ValueKind == JsonValueKind.Object
                    && textures.TryGetProperty("SKIN", out JsonElement skin) && skin.ValueKind == JsonValueKind.Object
                    && skin.TryGetProperty("url", out JsonElement url) && url.ValueKind == JsonValueKind.String)
                    return TextureHash(url.GetString());
                return null;
            }
            catch (JsonException)
            {
                Match lenient = LenientSkinUrl().Match(Encoding.UTF8.GetString(bytes));
                return lenient.Success ? TextureHash(lenient.Groups[1].Value) : null;
            }
        }

        /// <summary>The hash of a texture address on Mojang's texture server, lower case; null for any other address.</summary>
        public static string? TextureHash(string? url)
        {
            if (url is null)
                return null;
            foreach (string prefix in TexturePrefixes)
            {
                if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                string hash = url[prefix.Length..].ToLowerInvariant();
                return hash.Length is >= 16 and <= 64 && hash.All(IsHexDigit) ? hash : null;
            }

            return null;
        }

        // Base64 as head plugins write it: line breaks, the URL-safe alphabet, padding left out.
        private static string Padded(string value)
        {
            StringBuilder text = new(value.Length + 3);
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c) || c == '=')
                    continue;
                text.Append(c switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => c,
                });
            }

            while (text.Length % 4 != 0)
                text.Append('=');
            return text.ToString();
        }
    }
}
