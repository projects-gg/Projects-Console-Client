using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MinecraftClient.Dialogs;
using Look = MinecraftClient.ChatBots.AgentBridgeText.Look;
using Walk = MinecraftClient.ChatBots.AgentBridgeText.Walk;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Dialog cards (docs/agent-bridge.md, "Client 4.2.4: dialog cards"). With the console kinds on, the open dialog is
    /// one event line, <c>{"e":"dialog","rev","dialog":{...}}</c>, and a closed one <c>{"e":"dialog","rev","clear":true}</c>,
    /// so that the host can draw it as a card. A dialog the card cannot show as the game does (past a cap, a value
    /// outside the console's rule list, an action or input it does not know) is reported closed: the console keeps its
    /// plain lines. The host presses a button with <c>\dialog act {json}</c> (<see cref="Act"/>): on the dialog it showed
    /// only, a command button only with the command the player confirmed, every value checked again here, and the answer
    /// is a <c>{"e":"dialogres"}</c> line.
    /// </summary>
    internal static partial class ConsoleDialogs
    {
        /// <summary>What the bridge's <c>connecting</c> event lists in <c>caps</c> while dialog cards are reported.</summary>
        public const string Capability = "dialog";

        public const int MaxTitle = 128;
        public const int MaxTitleSpans = 16;
        public const int MaxBodyParts = 8;
        public const int MaxBodyText = 512;
        public const int MaxBodyLines = 16;
        public const int MaxBodySpans = 64;
        public const int MaxLabel = 64;
        public const int MaxLabelSpans = 16;
        public const int MaxInputs = 8;
        public const int MaxOptions = 16;
        public const int MaxOptionId = 64;
        public const int MaxButtons = 12;

        /// <summary>Longest value (a command template, an address, an input's text) in UTF-16 code units: never cut.</summary>
        public const int MaxValue = ConsoleLooks.MaxValueLength;

        /// <summary>Most bytes of a dialog's JSON; a larger dialog is reported closed.</summary>
        public const int MaxDialogBytes = 8192;

        /// <summary>Most characters of a press (<c>\dialog act</c>'s JSON).</summary>
        public const int MaxActLength = 4096;

        [GeneratedRegex("^[A-Za-z0-9_]{1,32}$")]
        private static partial Regex InputKey();

        private static readonly JsonWriterOptions jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
        };

        /// <summary>
        /// Dialog cards are reported while the console kinds are, or the side events' <c>dialogs</c> (client 4.2.5; both need
        /// the bridge's nonce, by which the host tells events from text).
        /// </summary>
        public static bool Enabled => ConsoleKinds.Enabled || ConsoleSide.Dialogs;

        /// <summary>Reports <paramref name="dialogs"/>' dialogs as event lines while <see cref="Enabled"/>.</summary>
        public static void Attach(DialogManager dialogs)
        {
            if (!Enabled)
                return;
            dialogs.DialogShown += instance => Write(ShownEvent(instance));
            dialogs.DialogCleared += revision => Write(ClearedEvent(revision));
        }

        internal static void Write(string json) => ConsoleKinds.Output(ConsoleKinds.LinePrefix + json);

        /// <summary>The open dialog as the card shows it; reported closed when the card cannot show it.</summary>
        internal static string ShownEvent(DialogInstance instance)
        {
            string? dialog = Describe(instance);
            if (dialog is null)
                return ClearedEvent(instance.Revision);
            return Json(writer =>
            {
                writer.WriteString("e", "dialog");
                writer.WriteNumber("rev", instance.Revision);
                writer.WritePropertyName("dialog");
                writer.WriteRawValue(dialog, skipInputValidation: true);
            });
        }

        internal static string ClearedEvent(int revision) => Json(writer =>
        {
            writer.WriteString("e", "dialog");
            writer.WriteNumber("rev", revision);
            writer.WriteBoolean("clear", true);
        });

        /// <summary>The answer to a press: done, or why not (<c>line</c>, <c>stale</c>, <c>button</c>, <c>input</c>, <c>cmd</c>, <c>phase</c>, <c>cancel</c>, <c>failed</c>).</summary>
        internal static string ResultEvent(int revision, string? why) => Json(writer =>
        {
            writer.WriteString("e", "dialogres");
            writer.WriteNumber("rev", revision);
            writer.WriteBoolean("ok", why is null);
            if (why is not null)
                writer.WriteString("why", why);
        });

        /// <summary>
        /// The dialog object of the event, or null when the card cannot show the dialog as the game does: unresolved,
        /// more than <see cref="MaxInputs"/> inputs, <see cref="MaxButtons"/> buttons or <see cref="MaxBodyParts"/> body
        /// parts, an input or action it does not know, an input key outside <c>[A-Za-z0-9_]{1,32}</c> (or twice), a value
        /// outside the rule list (<see cref="ConsoleLooks.ClickJson"/>), or more than <see cref="MaxDialogBytes"/> bytes.
        /// Texts (title, body, labels) are cut, never values.
        /// </summary>
        internal static string? Describe(DialogInstance instance)
        {
            DialogDefinition dialog = instance.Definition;
            if (!dialog.IsResolved || dialog.Inputs.Count > MaxInputs || dialog.Actions.Count > MaxButtons
                || dialog.Body.Count > MaxBodyParts)
                return null;

            List<Action<Utf8JsonWriter>> inputs = [];
            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (DialogInput input in dialog.Inputs)
            {
                if (!keys.Add(input.Key) || DescribeInput(input) is not { } write)
                    return null;
                inputs.Add(write);
            }

            List<Action<Utf8JsonWriter>> buttons = [];
            foreach (DialogButton button in dialog.Actions)
            {
                if (DescribeButton(button) is not { } write)
                    return null;
                buttons.Add(write);
            }

            (string title, List<(int Length, string Code)>? titleSpans) = Styled(dialog.Title, MaxTitle, 1, MaxTitleSpans);
            List<(string Text, List<(int Length, string Code)>? Spans)> body = dialog.Body
                .Select(part => Styled(part.Text, MaxBodyText, MaxBodyLines, MaxBodySpans))
                .Where(part => part.Text.Length > 0)
                .ToList();

            string json = Json(writer =>
            {
                writer.WriteString("title", title);
                if (titleSpans is not null)
                {
                    writer.WritePropertyName("titleSt");
                    AgentBridgeText.WriteSpans(writer, titleSpans);
                }

                writer.WriteStartArray("body");
                foreach ((string text, List<(int Length, string Code)>? spans) in body)
                {
                    writer.WriteStartObject();
                    writer.WriteString("text", text);
                    if (spans is not null)
                    {
                        writer.WritePropertyName("st");
                        AgentBridgeText.WriteSpans(writer, spans);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("inputs");
                foreach (Action<Utf8JsonWriter> write in inputs)
                    write(writer);
                writer.WriteEndArray();
                writer.WriteStartArray("buttons");
                foreach (Action<Utf8JsonWriter> write in buttons)
                    write(writer);
                writer.WriteEndArray();
                writer.WriteBoolean("cancel", CanCancel(dialog));
                writer.WriteBoolean("play", instance.Phase == DialogPhase.Play);
                writer.WriteNumber("cols", Math.Clamp(dialog.Columns, 1, 4));
            });
            return Encoding.UTF8.GetByteCount(json) <= MaxDialogBytes ? json : null;
        }

        /// <summary>
        /// A card may close the dialog when the game lets the player close it and closing runs no command: a command
        /// only ever runs from a button the player confirmed.
        /// </summary>
        internal static bool CanCancel(DialogDefinition dialog) =>
            (dialog.CanCloseWithEscape || dialog.CancelAction is not null)
            && dialog.CancelAction?.Kind != DialogActionKind.RunCommand;

        private static Action<Utf8JsonWriter>? DescribeButton(DialogButton button)
        {
            DialogActionDefinition? action = button.Action;
            (string kind, string? value) = (action?.Kind ?? DialogActionKind.None) switch
            {
                DialogActionKind.None => ("close", (string?)null),
                DialogActionKind.RunCommand => ("run_command", CommandTemplate(action!.Value)),
                DialogActionKind.CustomClick => ("custom", (string?)null),
                DialogActionKind.ShowDialog => ("dialog", (string?)null),
                DialogActionKind.OpenUrl => ("open_url", action!.Value),
                DialogActionKind.SuggestCommand => ("suggest_command", action!.Value),
                DialogActionKind.CopyToClipboard => ("copy_to_clipboard", action!.Value),
                _ => ("", (string?)null),
            };
            if (kind.Length == 0 || (kind == "custom" && string.IsNullOrEmpty(action!.Id)))
                return null;
            // A value follows the console's click rules: never cut, the rule list's characters refused.
            if (kind is "run_command" or "open_url" or "suggest_command" or "copy_to_clipboard"
                && (value is null || ConsoleLooks.ClickJson(kind, value) is null))
                return null;

            (string label, List<(int Length, string Code)>? spans) = Styled(button.Label, MaxLabel, 1, MaxLabelSpans);
            return writer =>
            {
                writer.WriteStartObject();
                writer.WriteNumber("i", button.Index);
                writer.WriteString("label", label);
                if (spans is not null)
                {
                    writer.WritePropertyName("labelSt");
                    AgentBridgeText.WriteSpans(writer, spans);
                }

                writer.WriteString("a", kind);
                if (value is not null)
                    writer.WriteString("v", value);
                writer.WriteEndObject();
            };
        }

        /// <summary>A command button's template as the card shows it: starting with "/", as the client runs it.</summary>
        private static string? CommandTemplate(string? template) =>
            template is null ? null : template.StartsWith('/') ? template : "/" + template;

        private static Action<Utf8JsonWriter>? DescribeInput(DialogInput input)
        {
            if (!InputKey().IsMatch(input.Key))
                return null;
            (string label, List<(int Length, string Code)>? spans) = Styled(input.Label, MaxLabel, 1, MaxLabelSpans);

            void Head(Utf8JsonWriter writer, string kind)
            {
                writer.WriteStartObject();
                writer.WriteString("key", input.Key);
                writer.WriteString("kind", kind);
                writer.WriteString("label", label);
                if (spans is not null)
                {
                    writer.WritePropertyName("labelSt");
                    AgentBridgeText.WriteSpans(writer, spans);
                }

                if (!input.LabelVisible)
                    writer.WriteBoolean("hideLabel", true);
            }

            switch (input.Kind)
            {
                case DialogInputKind.Text:
                {
                    int max = Math.Clamp(input.MaxLength, 1, MaxValue);
                    if (input.InitialValue.Length > max || !InputValueFits(input, input.InitialValue))
                        return null;
                    return writer =>
                    {
                        Head(writer, "text");
                        writer.WriteNumber("max", max);
                        if (input.Multiline)
                            writer.WriteBoolean("multiline", true);
                        writer.WriteString("initial", input.InitialValue);
                        writer.WriteEndObject();
                    };
                }

                case DialogInputKind.Boolean:
                {
                    // What a command template gets for each state: values too.
                    if (!TemplateValue(input.OnTrue) || !TemplateValue(input.OnFalse))
                        return null;
                    bool initial = input.InitialValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                    return writer =>
                    {
                        Head(writer, "bool");
                        writer.WriteBoolean("initial", initial);
                        writer.WriteString("onTrue", input.OnTrue);
                        writer.WriteString("onFalse", input.OnFalse);
                        writer.WriteEndObject();
                    };
                }

                case DialogInputKind.SingleOption:
                {
                    IReadOnlyList<DialogOption> options = input.Options ?? [];
                    if (options.Count is 0 or > MaxOptions
                        || options.Any(option => option.Id.Length > MaxOptionId || !TemplateValue(option.Id) || option.Id.Length == 0)
                        || options.Select(option => option.Id).Distinct(StringComparer.Ordinal).Count() != options.Count
                        || !options.Any(option => option.Id == input.InitialValue))
                        return null;
                    List<(string Id, string Label, List<(int Length, string Code)>? Spans)> shown = options
                        .Select(option =>
                        {
                            (string text, List<(int Length, string Code)>? optionSpans) =
                                Styled(string.IsNullOrWhiteSpace(option.Display) ? option.Id : option.Display, MaxLabel, 1, MaxLabelSpans);
                            return (option.Id, text, optionSpans);
                        })
                        .ToList();
                    return writer =>
                    {
                        Head(writer, "option");
                        writer.WriteStartArray("options");
                        foreach ((string id, string text, List<(int Length, string Code)>? optionSpans) in shown)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("id", id);
                            writer.WriteString("label", text);
                            if (optionSpans is not null)
                            {
                                writer.WritePropertyName("labelSt");
                                AgentBridgeText.WriteSpans(writer, optionSpans);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        writer.WriteString("initial", input.InitialValue);
                        writer.WriteEndObject();
                    };
                }

                case DialogInputKind.NumberRange:
                {
                    float initial = input.InitialNumber ?? (input.Start + input.End) / 2F;
                    float min = Math.Min(input.Start, input.End), max = Math.Max(input.Start, input.End);
                    if (!float.IsFinite(input.Start) || !float.IsFinite(input.End) || !float.IsFinite(initial)
                        || initial < min || initial > max
                        || input.Step is { } step && (!float.IsFinite(step) || step <= 0))
                        return null;
                    return writer =>
                    {
                        Head(writer, "range");
                        writer.WriteNumber("start", input.Start);
                        writer.WriteNumber("end", input.End);
                        if (input.Step is { } stepValue)
                            writer.WriteNumber("step", stepValue);
                        writer.WriteNumber("initial", initial);
                        writer.WriteEndObject();
                    };
                }

                default:
                    return null;
            }
        }

        /// <summary>A value a command template may get: at most <see cref="MaxValue"/> code units of well-formed text outside the rule list (empty allowed).</summary>
        private static bool TemplateValue(string value) =>
            value.Length <= MaxValue && string.Equals(AgentBridgeItems.WellFormed(value), value, StringComparison.Ordinal)
                                     && ConsoleLooks.Clean(value);

        /// <summary>
        /// An input's value from a card: at most <see cref="MaxValue"/> code units of well-formed text outside the rule
        /// list, a line break only in a multiline text. The dialog's own check (<see cref="DialogManager.SetInput(string, string)"/>)
        /// comes after: the length, the option, the range.
        /// </summary>
        internal static bool InputValueFits(DialogInput input, string value)
        {
            if (value.Length > MaxValue || !string.Equals(AgentBridgeItems.WellFormed(value), value, StringComparison.Ordinal))
                return false;
            bool lineBreaks = input.Kind == DialogInputKind.Text && input.Multiline;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n' && lineBreaks)
                    continue;
                if (ConsoleLooks.Forbidden(value[i]) || ConsoleLooks.IsTag(value, i))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Text with formatting codes as the card shows it: no codes, the rule list's characters left out, line breaks as
        /// <c>\n</c> up to <paramref name="maxLines"/> lines (one line: a space), at most <paramref name="max"/> code
        /// units (a cut text ends with "…"), with its spans when it has a look (none past <paramref name="maxSpans"/>).
        /// </summary>
        internal static (string Text, List<(int Length, string Code)>? Spans) Styled(string? formatted, int max, int maxLines,
            int maxSpans)
        {
            if (string.IsNullOrEmpty(formatted))
                return (string.Empty, null);
            Walk walk = new();
            walk.Literal(formatted, Look.None);
            if (walk.Failed)
                return (string.Empty, null);

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
                    if (c == '\r' && i + 1 < shown.Length && shown[i + 1] == '\n')
                        continue; // the line break is the '\n' that follows
                    if (c is '\r' or '\n' or '\u000B' or '\u000C' or '\u0085' or '\u2028' or '\u2029')
                    {
                        if (maxLines == 1)
                        {
                            c = ' ';
                        }
                        else if (lines == maxLines)
                        {
                            cut = true;
                            break;
                        }
                        else
                        {
                            c = '\n';
                            lines++;
                        }
                    }
                    else if (ConsoleLooks.Forbidden(c) || ConsoleLooks.IsTag(shown, i))
                    {
                        // A tag character is two code units: both stay out.
                        if (ConsoleLooks.IsTag(shown, i))
                            i++;
                        continue;
                    }

                    if (text.Length > max)
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

            // No line breaks or spaces at the ends that only a line break made.
            while (text.Length > 0 && text[^1] == '\n')
            {
                text.Length--;
                codes.RemoveAt(codes.Count - 1);
            }

            if (text.Length > max)
                cut = true;
            if (cut)
            {
                int keep = Math.Min(text.Length, max - 1);
                if (keep > 0 && char.IsHighSurrogate(text[keep - 1]))
                    keep--;
                text.Length = keep;
                codes.RemoveRange(keep, codes.Count - keep);
                text.Append('…');
                codes.Add(codes.Count > 0 ? codes[^1] : string.Empty);
            }

            string plain = text.ToString();
            if (string.IsNullOrWhiteSpace(plain.Replace('…', ' ')))
                return (string.Empty, null);
            return (plain, Spans(codes, maxSpans));
        }

        /// <summary>Spans from one code a character; null for none, one span of no look, or more than <paramref name="max"/>.</summary>
        private static List<(int Length, string Code)>? Spans(List<string> codes, int max)
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
        /// A press from the host (<c>\dialog act {json}</c>): <c>{"rev", "i", "inputs"?, "cmd"?}</c> presses button
        /// <c>i</c> after setting the inputs, <c>{"rev", "cancel": true}</c> closes the dialog. Only on the dialog of
        /// <c>rev</c> as the card showed it; a command button only when <c>cmd</c> is exactly its command filled in from
        /// the inputs (what the player confirmed), and only in play. The line holds no <c>%</c> (the client expands
        /// <c>%name%</c> in its commands): the host writes it as <c>\u0025</c>. The answer is the event to report.
        /// </summary>
        internal static string Act(DialogManager dialogs, string json)
        {
            int revision = 0;
            string? why = Press(dialogs, json, ref revision);
            return ResultEvent(revision, why);
        }

        private static string? Press(DialogManager dialogs, string json, ref int revision)
        {
            if (json.Length > MaxActLength || json.Contains('%'))
                return "line";

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
            }
            catch (JsonException)
            {
                return "line";
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("rev", out JsonElement rev)
                    || rev.ValueKind != JsonValueKind.Number || !rev.TryGetInt32(out revision))
                    return "line";

                DialogInstance? current = dialogs.Current;
                if (current is null || current.Revision != revision || Describe(current) is null)
                    return "stale";

                if (root.TryGetProperty("cancel", out JsonElement cancel) && cancel.ValueKind == JsonValueKind.True)
                {
                    if (!CanCancel(current.Definition))
                        return "cancel";
                    return dialogs.Cancel(revision).Success ? null : "stale";
                }

                if (!root.TryGetProperty("i", out JsonElement i) || i.ValueKind != JsonValueKind.Number
                    || !i.TryGetInt32(out int index))
                    return "line";
                DialogButton? button = current.Definition.Actions.FirstOrDefault(action => action.Index == index);
                if (button is null)
                    return "button";

                if (root.TryGetProperty("inputs", out JsonElement inputs))
                {
                    if (inputs.ValueKind != JsonValueKind.Object)
                        return "line";
                    int count = 0;
                    foreach (JsonProperty property in inputs.EnumerateObject())
                    {
                        DialogInput? input = current.Definition.Inputs
                            .FirstOrDefault(candidate => candidate.Key.Equals(property.Name, StringComparison.Ordinal));
                        if (++count > MaxInputs || input is null || property.Value.ValueKind != JsonValueKind.String
                            || property.Value.GetString() is not { } value || !InputValueFits(input, value))
                            return "input";
                        if (!dialogs.SetInput(revision, input.Key, value).Success)
                            return dialogs.Current?.Revision == revision ? "input" : "stale";
                    }

                    current = dialogs.Current;
                    if (current is null || current.Revision != revision)
                        return "stale";
                }

                string? command = root.TryGetProperty("cmd", out JsonElement cmd) && cmd.ValueKind == JsonValueKind.String
                    ? cmd.GetString()
                    : null;
                if (button.Action is { Kind: DialogActionKind.RunCommand } run)
                {
                    if (current.Phase != DialogPhase.Play)
                        return "phase";
                    string filled = DialogManager.CommandOf(current, run);
                    if (ConsoleLooks.ClickJson("run_command", filled) is null
                        || !string.Equals(filled, command, StringComparison.Ordinal))
                        return "cmd";
                }

                return dialogs.Press(revision, index, command).Success ? null : "failed";
            }
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
    }
}
