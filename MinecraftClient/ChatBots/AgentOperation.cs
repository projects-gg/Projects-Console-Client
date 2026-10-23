using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// One operation from the agent bridge's input (docs/agent-bridge.md): a window click, an anvil name, a window
    /// close, a hotbar slot or a snapshot request. There is no text command: chat and commands keep going through
    /// stdin, built by the host from fixed templates.
    /// </summary>
    internal sealed record AgentOperation
    {
        public const string Click = "click";
        public const string Rename = "rename";
        public const string Close = "close";
        public const string Hold = "hold";
        public const string Snapshot = "snapshot";

        public const int MaxIdLength = 64;
        public const int MaxRenameLength = 12;

        /// <summary>Order of arrival within the process, from 1.</summary>
        public required long Arrival { get; init; }

        public required string Op { get; init; }

        /// <summary>Host's correlation id, echoed in the answer.</summary>
        public string? Id { get; init; }

        public int? Window { get; init; }

        /// <summary>"seq" of the win_open event the host acted on; when given it must be the open window's.</summary>
        public long? WindowSeq { get; init; }

        public int Slot { get; init; }
        public int Button { get; init; }
        public string? Mode { get; init; }
        public string? Text { get; init; }
        public string? What { get; init; }

        /// <summary>
        /// Parse and check the shape of one input line. On failure, <paramref name="op"/> and
        /// <paramref name="id"/> carry what could be read, for the error event.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<byte> line, long arrival, [NotNullWhen(true)] out AgentOperation? operation,
            out string? op, out string? id, out string? reason)
        {
            operation = null;
            op = null;
            id = null;
            reason = "bad_request";

            JsonDocument document;
            try
            {
                // A duplicated field would make an operation mean two things: refuse it.
                document = JsonDocument.Parse(line.ToArray(),
                    new JsonDocumentOptions { MaxDepth = 4, AllowDuplicateProperties = false });
            }
            catch (JsonException)
            {
                return false;
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return false;

                if (root.TryGetProperty("id", out JsonElement idElement))
                {
                    if (idElement.ValueKind != JsonValueKind.String || !IsValidId(idElement.GetString()))
                    {
                        reason = "bad_id";
                        return false;
                    }

                    id = idElement.GetString();
                }

                if (!root.TryGetProperty("op", out JsonElement opElement) || opElement.ValueKind != JsonValueKind.String)
                    return false;

                op = opElement.GetString();
                if (op is not (Click or Rename or Close or Hold or Snapshot))
                {
                    reason = "unknown_op";
                    op = op is { Length: <= 16 } ? op : "?";
                    return false;
                }

                int? window = null;
                if (root.TryGetProperty("win", out JsonElement windowElement))
                {
                    if (!TryReadInt(windowElement, 0, int.MaxValue, out int value))
                        return false;
                    window = value;
                }

                long? windowSeq = null;
                if (root.TryGetProperty("ws", out JsonElement windowSeqElement))
                {
                    if (windowSeqElement.ValueKind != JsonValueKind.Number || !windowSeqElement.TryGetInt64(out long value)
                                                                            || value <= 0)
                        return false;
                    windowSeq = value;
                }

                int slot = 0, button = 0;
                string? mode = null, text = null, what = null;
                switch (op)
                {
                    case Click:
                        if (window is null
                            || !root.TryGetProperty("slot", out JsonElement slotElement)
                            || !TryReadInt(slotElement, 0, short.MaxValue, out slot)
                            || !root.TryGetProperty("mode", out JsonElement modeElement)
                            || modeElement.ValueKind != JsonValueKind.String)
                            return false;

                        mode = modeElement.GetString();
                        if (root.TryGetProperty("button", out JsonElement buttonElement)
                            && !TryReadInt(buttonElement, 0, 255, out button))
                            return false;
                        break;

                    case Rename:
                        if (window is null
                            || !root.TryGetProperty("text", out JsonElement textElement)
                            || textElement.ValueKind != JsonValueKind.String)
                            return false;
                        text = textElement.GetString();
                        break;

                    case Close:
                        if (window is null)
                            return false;
                        break;

                    case Hold:
                        if (!root.TryGetProperty("slot", out JsonElement holdElement) || !TryReadInt(holdElement, 0, 8, out slot))
                            return false;
                        break;

                    case Snapshot:
                        if (!root.TryGetProperty("what", out JsonElement whatElement) || whatElement.ValueKind != JsonValueKind.String)
                            return false;
                        what = whatElement.GetString();
                        if (what is not ("inv" or "win"))
                            return false;
                        break;
                }

                operation = new AgentOperation
                {
                    Arrival = arrival,
                    Op = op,
                    Id = id,
                    Window = window,
                    WindowSeq = windowSeq,
                    Slot = slot,
                    Button = button,
                    Mode = mode,
                    Text = text,
                    What = what,
                };
                reason = null;
                return true;
            }
        }

        /// <summary>The only text an anvil may get from the bridge: a money amount in digits.</summary>
        public static bool IsValidRenameText(string? text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > MaxRenameLength)
                return false;

            foreach (char c in text)
            {
                if (c is < '0' or > '9')
                    return false;
            }

            return true;
        }

        private static bool IsValidId(string? id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength)
                return false;

            foreach (char c in id)
            {
                if (!(char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.' or ':'))
                    return false;
            }

            return true;
        }

        private static bool TryReadInt(JsonElement element, int min, int max, out int value)
        {
            value = 0;
            return element.ValueKind == JsonValueKind.Number
                   && element.TryGetInt32(out value)
                   && value >= min
                   && value <= max;
        }
    }
}
