using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// What a version 4 mode needs from the client: <see cref="McClient"/> through the bridge, a fake in tests. Every
    /// action is taken on the client's tick, from the bridge's Update(), which runs before the tick's move: the packets go
    /// in the game's order (docs/agent-bridge.md, "Version 4").
    /// </summary>
    internal interface IAutoHost
    {
        /// <summary>The joined server's protocol version.</summary>
        int Protocol { get; }

        bool Terrain { get; }
        bool Entities { get; }
        bool Inventory { get; }

        /// <summary>The server placed the player (with terrain: the physics stands it in a loaded chunk).</summary>
        bool PositionKnown { get; }

        Location Position { get; }
        float Yaw { get; }
        float Pitch { get; }

        /// <summary>A screen is open: a window the server opened, or the player's own inventory the client clicks.</summary>
        bool ScreenOpen { get; }

        /// <summary>A window the server opened is open (a menu, a trade).</summary>
        bool WindowOpen { get; }

        /// <summary>The selected hotbar slot, 0-8.</summary>
        int HeldSlot { get; }

        int PlayerEntityId { get; }

        /// <summary>The player inventory by window 0's slots (9-35 main, 36-44 hotbar), or null without inventory handling.</summary>
        IReadOnlyDictionary<int, Item>? Items { get; }

        /// <summary>Use the item in the main hand.</summary>
        bool Use();

        /// <summary>Select a hotbar slot.</summary>
        bool Hold(int slot);

        /// <summary>Swap a main inventory slot with a hotbar slot (one click with the number key).</summary>
        bool Swap(int slot, int hotbar);

        /// <summary>Look this way; the tick's move carries it.</summary>
        void Look(float yaw, float pitch);

        /// <summary>A sneak key another bot left down goes up (the hand-over from AntiAFK before the mode's work).</summary>
        void ReleaseSneak();
    }

    /// <summary>What mining needs besides: the terrain, its own dig and the idle actions.</summary>
    internal interface IMineHost : IAutoHost
    {
        /// <summary>The block at a position (air where the client knows none).</summary>
        Material BlockAt(int x, int y, int z);

        /// <summary>The player stands on the ground (the physics).</summary>
        bool OnGround { get; }

        /// <summary>The sneak key is down for the server (the eyes are at 1.27).</summary>
        bool Sneaking { get; }

        /// <summary>Ticks to break this block with this item in the main hand now (0 instant, -1 never).</summary>
        int DigTicks(Material block, Item? tool);

        /// <summary>A dig action: 0 start, 1 abort, 2 finish.</summary>
        bool Dig(int status, Location block, Direction face);

        bool Swing();
        void Jump();
        void Sneak(bool on);

        /// <summary>Hold the back key (1), the forward key (2) or neither (0), without turning.</summary>
        void StepKey(int key);

        /// <summary>A path is being walked (another part of the client moves the player).</summary>
        bool Walking { get; }
    }

    /// <summary>A rod or a pickaxe as the bridge reports it: the namespaced id, durability left and maximum.</summary>
    internal sealed record AutoTool(string Item, int Dur, int Max);

    /// <summary>The block mining looks at.</summary>
    internal sealed record AutoTarget(string Id, int X, int Y, int Z);

    /// <summary>What a mode does, as its <c>fish</c> or <c>mine</c> event says.</summary>
    internal sealed record AutoState(string State, string? Reason, string? Doing, AutoTool? Tool, AutoTarget? Target);

    /// <summary>The items, durability and loot rules both modes share.</summary>
    internal static class AgentAutoItems
    {
        public static string Id(Item item) => AgentBridgeItems.RegistryName(item.Type);

        public static string BlockId(Material material) => "minecraft:" + material.ToString().ToUnderscoreCase();

        /// <summary>The material a namespaced block id names, or null.</summary>
        public static Material? MaterialOf(string id)
        {
            if (!id.StartsWith("minecraft:", StringComparison.Ordinal))
                return null;
            string pascal = string.Concat(id["minecraft:".Length..].Split('_')
                .Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));
            return Enum.TryParse(pascal, out Material material) && BlockId(material) == id ? material : null;
        }

        /// <summary>The durability an item type has when the stack does not say (the game's defaults).</summary>
        public static int? DefaultMaxDamage(ItemType type) => type switch
        {
            ItemType.FishingRod => 64,
            ItemType.WoodenPickaxe => 59,
            ItemType.StonePickaxe => 131,
            ItemType.CopperPickaxe => 190,
            ItemType.IronPickaxe => 250,
            ItemType.GoldenPickaxe => 32,
            ItemType.DiamondPickaxe => 1561,
            ItemType.NetheritePickaxe => 2031,
            _ => null,
        };

        public static bool IsPickaxe(ItemType type) => type is ItemType.WoodenPickaxe or ItemType.StonePickaxe
            or ItemType.CopperPickaxe or ItemType.IronPickaxe or ItemType.GoldenPickaxe or ItemType.DiamondPickaxe
            or ItemType.NetheritePickaxe;

        /// <summary>
        /// The durability an item has left and its maximum: the stack's <c>max_damage</c> (1.20.5+) or the type's
        /// default, less its damage; an unbreakable item has all of it. Null for an item without durability.
        /// </summary>
        public static (int Left, int Max)? Durability(Item item)
        {
            int? max = null;
            bool unbreakable = false;
            if (item.Components is { } components)
            {
                foreach (var component in components)
                {
                    if (component is MaxDamageComponent maxDamage)
                        max = maxDamage.MaxDamage;
                    else if (component.ComponentName == "minecraft:unbreakable")
                        unbreakable = component is not UnbreakableComponent1206 legacy || legacy.Unbreakable;
                }
            }
            else if (item.NBT is { } nbt && nbt.TryGetValue("Unbreakable", out object? flag))
            {
                unbreakable = flag is byte b ? b != 0 : flag is bool on && on;
            }

            max ??= DefaultMaxDamage(item.Type);
            if (max is not { } most || most <= 0)
                return null;
            if (unbreakable)
                return (most, most);
            return (Math.Clamp(most - item.Damage, 0, most), most);
        }

        public static AutoTool? Tool(Item? item)
        {
            if (item is null || item.IsEmpty || Durability(item) is not { } durability)
                return null;
            return new AutoTool(Id(item), durability.Left, durability.Max);
        }

        /// <summary>The item carries an enchantment (a book's stored ones count).</summary>
        public static bool Enchanted(Item item)
        {
            if (item.Components is { } components)
            {
                return components.Any(component =>
                    component is EnchantmentsComponent { Enchantments.Count: > 0 }
                    || component.ComponentName == "minecraft:stored_enchantments");
            }

            return item.NBT is { } nbt
                   && (nbt.TryGetValue("Enchantments", out object? e) && e is object[] { Length: > 0 }
                       || nbt.TryGetValue("ench", out object? old) && old is object[] { Length: > 0 }
                       || nbt.TryGetValue("StoredEnchantments", out object? s) && s is object[] { Length: > 0 });
        }

        /// <summary>
        /// The game's fishing loot categories (contract "Categories"): fish, treasure (a bow, an enchanted book, an
        /// enchanted rod, a name tag, a nautilus shell, a saddle), junk (the rest).
        /// </summary>
        public static string Category(string id, bool enchanted) => id switch
        {
            "minecraft:cod" or "minecraft:salmon" or "minecraft:tropical_fish" or "minecraft:pufferfish" => "fish",
            "minecraft:bow" or "minecraft:enchanted_book" or "minecraft:name_tag" or "minecraft:nautilus_shell"
                or "minecraft:saddle" => "treasure",
            "minecraft:fishing_rod" when enchanted => "treasure",
            _ => "junk",
        };

        /// <summary>Everything the game's fishing loot tables give (fish, treasure, junk): a reel brings one of them.</summary>
        public static readonly HashSet<string> FishingLoot = new(StringComparer.Ordinal)
        {
            "minecraft:cod", "minecraft:salmon", "minecraft:tropical_fish", "minecraft:pufferfish",
            "minecraft:bow", "minecraft:enchanted_book", "minecraft:fishing_rod", "minecraft:name_tag",
            "minecraft:nautilus_shell", "minecraft:saddle",
            "minecraft:lily_pad", "minecraft:leather_boots", "minecraft:leather", "minecraft:bone", "minecraft:potion",
            "minecraft:string", "minecraft:bowl", "minecraft:stick", "minecraft:ink_sac", "minecraft:tripwire_hook",
            "minecraft:rotten_flesh", "minecraft:bamboo",
        };

        /// <summary>The storage slots (main inventory and hotbar) of window 0.</summary>
        public static bool IsStorage(int slot) => slot is >= 9 and <= 44;

        /// <summary>No empty storage slot is left.</summary>
        public static bool NoEmptySlot(IReadOnlyDictionary<int, Item> items)
        {
            for (int slot = 9; slot <= 44; slot++)
            {
                if (!items.TryGetValue(slot, out Item? item) || item.IsEmpty)
                    return false;
            }

            return true;
        }

        /// <summary>A stack of that id in storage still has room.</summary>
        public static bool RoomFor(IReadOnlyDictionary<int, Item> items, ItemType type)
        {
            for (int slot = 9; slot <= 44; slot++)
            {
                if (!items.TryGetValue(slot, out Item? item) || item.IsEmpty)
                    return true;
                if (item.Type == type && item.Count < item.Type.StackCount())
                    return true;
            }

            return false;
        }

        /// <summary>What each storage slot holds: its id, count and whether it is enchanted.</summary>
        public static Dictionary<int, (string Id, int Count, bool Enchanted)> Snapshot(IReadOnlyDictionary<int, Item> items)
        {
            Dictionary<int, (string, int, bool)> snapshot = new();
            foreach ((int slot, Item item) in items)
            {
                if ((IsStorage(slot) || slot == 45) && !item.IsEmpty)
                    snapshot[slot] = (Id(item), item.Count, Enchanted(item));
            }

            return snapshot;
        }

        /// <summary>
        /// What came into the inventory between two snapshots: per slot, more of the same item, or an item in a slot that
        /// was empty. Items that only moved or wore (a rod's damage) are not new.
        /// </summary>
        public static List<(string Id, int Count, bool Enchanted)> Added(
            Dictionary<int, (string Id, int Count, bool Enchanted)> before,
            Dictionary<int, (string Id, int Count, bool Enchanted)> after)
        {
            List<(string, int, bool)> added = new();
            foreach ((int slot, var now) in after)
            {
                if (!before.TryGetValue(slot, out var was))
                    added.Add(now);
                else if (was.Id == now.Id && now.Count > was.Count)
                    added.Add((now.Id, now.Count - was.Count, now.Enchanted));
            }

            // An item that only moved to an empty slot is not new: it left another.
            foreach ((int slot, var was) in before)
            {
                if (after.ContainsKey(slot))
                    continue;
                int index = added.FindIndex(a => a.Item1 == was.Id && a.Item2 == was.Count);
                if (index >= 0)
                    added.RemoveAt(index);
            }

            return added;
        }
    }
}
