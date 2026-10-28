using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MinecraftClient.Protocol.Message
{
    /// <summary>
    /// A click action carried by a chat component (run_command, suggest_command, open_url, copy_to_clipboard,
    /// change_page, show_dialog, custom) and its value: the command, URL, text, page or id.
    /// </summary>
    public readonly record struct ChatClick(string Action, string Value);

    /// <summary>
    /// Collects the click actions of a chat component tree. The console never shows them, so protocol handlers
    /// only collect them while a host asked for them (<see cref="Collecting"/>, set by the agent bridge).
    /// </summary>
    public static class ChatClicks
    {
        /// <summary>At most this many distinct clicks are kept per message; the rest are ignored.</summary>
        public const int MaxClicks = 16;

        /// <summary>Longer values are cut to this many characters.</summary>
        public const int MaxValueLength = 256;

        private const int MaxActionLength = 32;
        private const int MaxDepth = 32;

        // 1.21.5+ names the value after the action ("command", "url", "page", "id"); older formats use "value".
        private static readonly string[] ValueKeys = ["command", "url", "value", "page", "id"];

        /// <summary>Set while the agent bridge runs.</summary>
        public static bool Collecting { get; set; }

        /// <summary>A list to collect into, or null when nobody needs the clicks.</summary>
        public static List<ChatClick>? NewListIfCollecting() => Collecting ? new List<ChatClick>() : null;

        /// <summary>Collects from an NBT text component (1.20.3+ network format).</summary>
        public static void Collect(Dictionary<string, object>? component, List<ChatClick> clicks)
        {
            CollectNbt(component, clicks, 0);
        }

        /// <summary>Collects from a JSON text component (network format before 1.20.3).</summary>
        public static void Collect(JsonNode? component, List<ChatClick> clicks)
        {
            CollectJson(component, clicks, 0);
        }

        /// <summary>Collects from serialized JSON; anything that is not a JSON object or array is ignored.</summary>
        public static void CollectJson(string? json, List<ChatClick> clicks)
        {
            if (string.IsNullOrEmpty(json))
                return;

            string trimmed = json.TrimStart();
            if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
                return;

            try
            {
                CollectJson(JsonNode.Parse(trimmed), clicks, 0);
            }
            catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
            {
                // Not JSON after all (a plain text line has no clicks), or JSON a game would not read either
                // (duplicate keys throw only when the object is first read): no clicks, never a crash.
            }
        }

        private static void CollectNbt(object? node, List<ChatClick> clicks, int depth)
        {
            if (depth > MaxDepth || clicks.Count >= MaxClicks)
                return;

            switch (node)
            {
                case Dictionary<string, object> compound:
                    if (compound.TryGetValue("click_event", out object? modern) && modern is Dictionary<string, object> modernClick)
                        Add(clicks, ReadNbtClick(modernClick));
                    else if (compound.TryGetValue("clickEvent", out object? legacy) && legacy is Dictionary<string, object> legacyClick)
                        Add(clicks, ReadNbtClick(legacyClick));

                    if (compound.TryGetValue("with", out object? with))
                        CollectNbt(with, clicks, depth + 1);
                    if (compound.TryGetValue("extra", out object? extra))
                        CollectNbt(extra, clicks, depth + 1);
                    break;

                case object[] list:
                    foreach (object child in list)
                        CollectNbt(child, clicks, depth + 1);
                    break;
            }
        }

        private static void CollectJson(JsonNode? node, List<ChatClick> clicks, int depth)
        {
            if (depth > MaxDepth || clicks.Count >= MaxClicks)
                return;

            switch (node)
            {
                case JsonObject obj:
                    if (obj["click_event"] is JsonObject modernClick)
                        Add(clicks, ReadJsonClick(modernClick));
                    else if (obj["clickEvent"] is JsonObject legacyClick)
                        Add(clicks, ReadJsonClick(legacyClick));

                    CollectJson(obj["with"], clicks, depth + 1);
                    CollectJson(obj["extra"], clicks, depth + 1);
                    break;

                case JsonArray array:
                    foreach (JsonNode? child in array)
                        CollectJson(child, clicks, depth + 1);
                    break;
            }
        }

        /// <summary>A click compound's action and value (uncut), or null without an action.</summary>
        internal static ChatClick? ReadNbtClick(Dictionary<string, object> click)
        {
            if (!click.TryGetValue("action", out object? actionValue) || actionValue is not string action || action.Length == 0)
                return null;

            foreach (string key in ValueKeys)
            {
                if (click.TryGetValue(key, out object? value) && value is string or int or long or short or byte)
                    return new ChatClick(action, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return new ChatClick(action, string.Empty);
        }

        /// <summary>A click object's action and value (uncut), or null without an action.</summary>
        internal static ChatClick? ReadJsonClick(JsonObject click)
        {
            if (click["action"] is not JsonValue actionNode
                || actionNode.GetValueKind() != JsonValueKind.String
                || actionNode.GetValue<string>() is not { Length: > 0 } action)
                return null;

            foreach (string key in ValueKeys)
            {
                if (click[key] is JsonValue value)
                {
                    return value.GetValueKind() switch
                    {
                        JsonValueKind.String => new ChatClick(action, value.GetValue<string>()),
                        JsonValueKind.Number => new ChatClick(action, value.ToJsonString()),
                        _ => new ChatClick(action, string.Empty),
                    };
                }
            }

            return new ChatClick(action, string.Empty);
        }

        private static void Add(List<ChatClick> clicks, ChatClick? click)
        {
            if (click is not { } found || clicks.Count >= MaxClicks)
                return;

            ChatClick entry = new(Cut(found.Action, MaxActionLength), Cut(found.Value, MaxValueLength));
            if (!clicks.Contains(entry))
                clicks.Add(entry);
        }

        private static string Cut(string text, int length) => text.Length <= length ? text : text[..length];
    }
}
