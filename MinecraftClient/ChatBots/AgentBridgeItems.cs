using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Item stacks as the agent bridge reports them (docs/agent-bridge.md, "Items"). Everything a trade check needs
    /// comes from the item's real data: the registry id, the enchantment and damage components, the removed default
    /// components. Names and lore are reported too, but only as text a player could have typed.
    /// </summary>
    internal static partial class AgentBridgeItems
    {
        public const int MaxLoreLines = 32;
        public const int MaxTextLength = 256;
        public const int MaxNestedItems = 64;
        private const int MaxNbtNodes = 2000;
        private const int MaxNbtDepth = 16;

        // PascalCase enum names back to registry names: "DiscFragment5" -> "disc_fragment_5",
        // "CarrotOnAStick" -> "carrot_on_a_stick", "JackOLantern" -> "jack_o_lantern".
        [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[A-Za-z])(?=[0-9])")]
        private static partial Regex WordBoundary();

        /// <summary>"minecraft:elytra" for <see cref="ItemType.Elytra"/>.</summary>
        public static string RegistryName(ItemType type)
        {
            return "minecraft:" + WordBoundary().Replace(type.ToString(), "_").ToLowerInvariant();
        }

        /// <summary>Plain text: no formatting codes, line breaks as "⏎", cut to <paramref name="maxLength"/>.</summary>
        public static string Plain(string? text, int maxLength = MaxTextLength)
        {
            string plain = WellFormed(ConsoleIO.KeepOnOneLine(ChatBot.GetVerbatim(text)));
            if (plain.Length <= maxLength)
                return plain;
            // Never end on the first half of a surrogate pair.
            return char.IsHighSurrogate(plain[maxLength - 1]) ? plain[..(maxLength - 1)] : plain[..maxLength];
        }

        /// <summary>
        /// <paramref name="text"/> with every lone surrogate replaced by U+FFFD: JSON writers refuse ill-formed UTF-16,
        /// and a server may send it.
        /// </summary>
        public static string WellFormed(string text)
        {
            int i = 0;
            for (; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    i++;
                else if (char.IsSurrogate(text[i]))
                    break;
            }

            if (i == text.Length)
                return text;

            StringBuilder result = new(text.Length);
            result.Append(text, 0, i);
            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    result.Append(c).Append(text[++i]);
                }
                else
                {
                    result.Append(char.IsSurrogate(c) ? '�' : c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Write one item as a JSON object. <paramref name="slot"/> is omitted when null (nested items).
        /// </summary>
        public static void Write(Utf8JsonWriter writer, int? slot, Item item, int depth = 0)
        {
            bool unparsed = item.Type is ItemType.Unknown or ItemType.Null;

            writer.WriteStartObject();
            if (slot is not null)
                writer.WriteNumber("slot", slot.Value);
            writer.WriteString("id", RegistryName(item.Type));
            writer.WriteNumber("count", item.Count);

            if (item.Components is { } components)
                unparsed |= WriteComponents(writer, components, depth);
            else if (item.NBT is { Count: > 0 } nbt)
                unparsed |= WriteLegacyNbt(writer, item, nbt);

            if (item.RemovedComponents is { Count: > 0 } removed)
            {
                writer.WriteStartArray("removed");
                foreach (string name in removed)
                    writer.WriteStringValue(name);
                writer.WriteEndArray();
            }

            writer.WriteString("fp", Fingerprint(item));
            if (unparsed)
                writer.WriteBoolean("unparsed", true);
            writer.WriteEndObject();
        }

        /// <summary>Returns true when something could not be reported faithfully.</summary>
        private static bool WriteComponents(Utf8JsonWriter writer, List<StructuredComponent> components, int depth)
        {
            bool unparsed = false;

            StructuredComponent? Find(string name) => components.FirstOrDefault(c => c.ComponentName == name);

            if (Find("minecraft:custom_name") is CustomNameComponent customName)
                writer.WriteString("name", Plain(customName.CustomName));
            if (Find("minecraft:item_name") is ItemNameComponent itemName)
                writer.WriteString("itemName", Plain(itemName.ItemName));

            if (Find("minecraft:lore") is LoreNameComponent1206 lore && lore.Lines.Count > 0)
            {
                writer.WriteStartArray("lore");
                foreach (string line in lore.Lines.Take(MaxLoreLines))
                    writer.WriteStringValue(Plain(line));
                writer.WriteEndArray();
            }

            unparsed |= WriteEnchantments(writer, "ench", Find("minecraft:enchantments"));
            unparsed |= WriteEnchantments(writer, "stored", Find("minecraft:stored_enchantments"));

            if (Find("minecraft:damage") is DamageComponent damage)
                writer.WriteNumber("damage", damage.Damage);
            if (Find("minecraft:max_damage") is MaxDamageComponent maxDamage)
                writer.WriteNumber("maxDamage", maxDamage.MaxDamage);

            // The potion and the color an item is drawn with (agent bridge version 2).
            (bool hasPotion, int potionId, int? customColor) = Find("minecraft:potion_contents") switch
            {
                PotionContentsComponent potion => (potion.HasPotionId, potion.PotionId,
                    potion.HasCustomColor ? potion.CustomColor : (int?)null),
                PotionContentsComponent1212 potion => (potion.HasPotionId, potion.PotionId,
                    potion.HasCustomColor ? potion.CustomColor : (int?)null),
                _ => (false, 0, null),
            };
            if (hasPotion)
            {
                if (PotionRegistry.Name(potionId) is { } potionName)
                    writer.WriteString("potion", potionName);
                else
                    unparsed = true;
            }

            int? dyedColor = Find("minecraft:dyed_color") switch
            {
                DyeColorComponent dye => dye.Color,
                DyeColorComponent1215 dye => dye.Color,
                _ => null,
            };
            if ((dyedColor ?? customColor) is { } color)
                writer.WriteNumber("color", color & 0xFFFFFF);

            writer.WriteStartArray("cnames");
            foreach (StructuredComponent component in components)
                writer.WriteStringValue(component.ComponentName);
            writer.WriteEndArray();

            // Decoded values of the components a trade check may look at; the rest is only named in "cnames".
            writer.WriteStartObject("components");
            foreach (StructuredComponent component in components)
            {
                switch (component)
                {
                    case CustomDataComponent customData:
                        writer.WritePropertyName(component.ComponentName);
                        unparsed |= !WriteNbt(writer, customData.Nbt);
                        break;
                    case TypedEntityDataComponent typed:
                        writer.WriteStartObject(component.ComponentName);
                        writer.WriteNumber("type", typed.EntityTypeId);
                        writer.WritePropertyName("nbt");
                        unparsed |= !WriteNbt(writer, typed.Nbt);
                        writer.WriteEndObject();
                        break;
                    case ProfileComponent profile:
                        writer.WriteStartObject(component.ComponentName);
                        if (profile.HasName && profile.Name is not null)
                            writer.WriteString("name", Plain(profile.Name, 16));
                        writer.WriteEndObject();
                        break;
                    case RepairCostComponent repairCost:
                        writer.WriteNumber(component.ComponentName, repairCost.Cost);
                        break;
                    case ContainerComponent261 container:
                        unparsed |= WriteNested(writer, component.ComponentName, container.Items, depth);
                        break;
                    case ItemStackTemplateListComponent261 list:
                        unparsed |= WriteNested(writer, component.ComponentName, list.Items.Cast<Item?>(), depth);
                        break;
                    default:
                        if (component.ComponentName == "minecraft:unbreakable")
                            writer.WriteBoolean(component.ComponentName, true);
                        break;
                }
            }

            writer.WriteEndObject();
            return unparsed;
        }

        private static bool WriteEnchantments(Utf8JsonWriter writer, string property, StructuredComponent? component)
        {
            if (component is not EnchantmentsComponent enchantments || enchantments.Enchantments.Count == 0)
                return false;

            bool unknown = false;
            writer.WriteStartObject(property);
            foreach (Enchantment enchantment in enchantments.Enchantments)
            {
                string? name = EnchantmentMapping.GetEnchantmentRegistryName(enchantment.Type);
                if (name is null)
                {
                    unknown = true;
                    continue;
                }

                writer.WriteNumber(name, enchantment.Level);
            }

            writer.WriteEndObject();
            return unknown;
        }

        private static bool WriteNested(Utf8JsonWriter writer, string property, IEnumerable<Item?> items, int depth)
        {
            List<Item> present = items.Where(item => item is { IsEmpty: false }).Cast<Item>().ToList();
            writer.WriteStartObject(property);
            writer.WriteNumber("count", present.Count);
            if (depth < 1)
            {
                writer.WriteStartArray("items");
                foreach (Item nested in present.Take(MaxNestedItems))
                    Write(writer, null, nested, depth + 1);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
            return depth >= 1 && present.Count > 0;
        }

        private static bool WriteLegacyNbt(Utf8JsonWriter writer, Item item, Dictionary<string, object> nbt)
        {
            string? displayName = item.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
                writer.WriteString("name", Plain(displayName));

            if (item.Lores is { Length: > 0 } lores)
            {
                writer.WriteStartArray("lore");
                foreach (string line in lores.Take(MaxLoreLines))
                    writer.WriteStringValue(Plain(line));
                writer.WriteEndArray();
            }

            if (item.Damage != 0)
                writer.WriteNumber("damage", item.Damage);

            // Before 1.20.5 the potion is a registry id in the NBT already; colors are integers there too.
            if (nbt.TryGetValue("Potion", out object? potion) && potion is string { Length: > 0 } potionId)
                writer.WriteString("potion", Plain(potionId.Contains(':') ? potionId : "minecraft:" + potionId, 64));
            int? legacyColor = nbt.TryGetValue("display", out object? display)
                               && display is Dictionary<string, object> displayTag
                               && displayTag.TryGetValue("color", out object? dyed) && dyed is int dyedColor
                ? dyedColor
                : nbt.TryGetValue("CustomPotionColor", out object? custom) && custom is int customColor
                    ? customColor
                    : null;
            if (legacyColor is { } color)
                writer.WriteNumber("color", color & 0xFFFFFF);

            bool unparsed = false;
            unparsed |= WriteLegacyEnchantments(writer, "ench", nbt, "Enchantments");
            unparsed |= WriteLegacyEnchantments(writer, "stored", nbt, "StoredEnchantments");

            writer.WritePropertyName("nbt");
            unparsed |= !WriteNbt(writer, nbt);
            return unparsed;
        }

        private static bool WriteLegacyEnchantments(Utf8JsonWriter writer, string property, Dictionary<string, object> nbt,
            string key)
        {
            if (!nbt.TryGetValue(key, out object? value) || value is not object[] list || list.Length == 0)
                return false;

            bool unknown = false;
            writer.WriteStartObject(property);
            foreach (object entry in list)
            {
                if (entry is Dictionary<string, object> enchantment
                    && enchantment.TryGetValue("id", out object? id) && id is string name
                    && enchantment.TryGetValue("lvl", out object? level))
                {
                    writer.WriteNumber(name.Contains(':') ? name : "minecraft:" + name,
                        Convert.ToInt32(level, CultureInfo.InvariantCulture));
                }
                else
                {
                    unknown = true;
                }
            }

            writer.WriteEndObject();
            return unknown;
        }

        /// <summary>Largest raw text component a window event carries, in UTF-8 bytes of its JSON.</summary>
        public const int MaxRawComponentBytes = 32 * 1024;

        private static readonly JsonWriterOptions rawComponentOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// A text component as the server sent it, as one line of JSON: an NBT tree (1.20.4+) the way
        /// <see cref="WriteNbt"/> writes it (a plain string root as a JSON string), a JSON text re-written compactly,
        /// and text that is not JSON as a JSON string. Fonts, colors and every glyph are kept. Null when there is none,
        /// it is larger than <see cref="MaxRawComponentBytes"/> or the tree had to be cut.
        /// </summary>
        public static byte[]? RawComponent(object? component)
        {
            if (component is null)
                return null;

            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            bool complete = true;
            try
            {
                using Utf8JsonWriter writer = new(buffer, rawComponentOptions);
                switch (component)
                {
                    case Dictionary<string, object> nbt:
                        object? root = nbt.Count == 1 && nbt.TryGetValue("", out object? value) ? value : nbt;
                        complete = WriteNbt(writer, root);
                        break;
                    case string text:
                        try
                        {
                            using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
                            document.RootElement.WriteTo(writer);
                        }
                        catch (JsonException)
                        {
                            writer.Reset();
                            buffer.Clear();
                            writer.WriteStringValue(WellFormed(text));
                        }

                        break;
                    default:
                        return null;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            return complete && buffer.WrittenCount <= MaxRawComponentBytes ? buffer.WrittenSpan.ToArray() : null;
        }

        /// <summary>
        /// Write NBT as plain JSON: compounds as objects, lists and arrays as arrays, numbers as numbers.
        /// Returns false when the value was cut (too deep or too big); the cut parts are written as null.
        /// </summary>
        public static bool WriteNbt(Utf8JsonWriter writer, object? nbt)
        {
            int budget = MaxNbtNodes;
            return WriteNbtNode(writer, nbt, 0, ref budget);
        }

        private static bool WriteNbtNode(Utf8JsonWriter writer, object? node, int depth, ref int budget)
        {
            if (--budget < 0 || depth > MaxNbtDepth)
            {
                writer.WriteNullValue();
                return false;
            }

            bool complete = true;
            switch (node)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case Dictionary<string, object> compound:
                    writer.WriteStartObject();
                    foreach ((string key, object value) in compound.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(key);
                        complete &= WriteNbtNode(writer, value, depth + 1, ref budget);
                    }

                    writer.WriteEndObject();
                    break;
                case string text:
                    writer.WriteStringValue(WellFormed(text));
                    break;
                case bool flag:
                    writer.WriteBooleanValue(flag);
                    break;
                case byte or sbyte or short or ushort or int or uint or long:
                    writer.WriteNumberValue(Convert.ToInt64(node, CultureInfo.InvariantCulture));
                    break;
                case float or double:
                    double number = Convert.ToDouble(node, CultureInfo.InvariantCulture);
                    if (double.IsFinite(number))
                        writer.WriteNumberValue(number);
                    else
                        writer.WriteStringValue(number.ToString(CultureInfo.InvariantCulture));
                    break;
                case System.Collections.IEnumerable list:
                    writer.WriteStartArray();
                    foreach (object? element in list)
                        complete &= WriteNbtNode(writer, element, depth + 1, ref budget);
                    writer.WriteEndArray();
                    break;
                default:
                    writer.WriteStringValue(Convert.ToString(node, CultureInfo.InvariantCulture));
                    break;
            }

            return complete;
        }

        /// <summary>
        /// Short hash of everything that makes the stack what it is: id, count, every component's network bytes and
        /// the removed components. Two stacks with the same fingerprint are the same to the server.
        /// </summary>
        public static string Fingerprint(Item item)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            void Add(string text) => hash.AppendData(Encoding.UTF8.GetBytes(text + "\u0000"));

            Add(RegistryName(item.Type));
            Add(item.Count.ToString(CultureInfo.InvariantCulture));

            if (item.Components is { } components)
            {
                foreach (StructuredComponent component in components)
                {
                    Add(component.ComponentName);
                    try
                    {
                        hash.AppendData(component.Serialize().ToArray());
                    }
                    catch (Exception)
                    {
                        Add("?");
                    }
                }
            }
            else if (item.NBT is { Count: > 0 } nbt)
            {
                var buffer = new System.Buffers.ArrayBufferWriter<byte>();
                using (Utf8JsonWriter writer = new(buffer))
                    WriteNbt(writer, nbt);
                hash.AppendData(buffer.WrittenSpan);
            }

            if (item.RemovedComponents is { } removed)
            {
                foreach (string name in removed.Order(StringComparer.Ordinal))
                    Add("-" + name);
            }

            return Convert.ToHexStringLower(hash.GetHashAndReset())[..32];
        }
    }
}
