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
            string plain = ConsoleIO.KeepOnOneLine(ChatBot.GetVerbatim(text));
            return plain.Length <= maxLength ? plain : plain[..maxLength];
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
                    writer.WriteStringValue(text);
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
