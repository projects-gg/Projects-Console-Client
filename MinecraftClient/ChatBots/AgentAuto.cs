using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MinecraftClient.ChatBots
{
    /// <summary>The parts of the agent bridge a host asked for when it started the client (docs/agent-bridge.md).</summary>
    [Flags]
    internal enum AgentFeatures
    {
        None = 0,

        /// <summary>Version 3's movement (<c>MCC_AGENT_MOVE=1</c>).</summary>
        Move = 1,

        /// <summary>Version 4's auto-fishing (<c>MCC_AGENT_FISH=1</c>).</summary>
        Fish = 2,

        /// <summary>Version 4's auto-mining (<c>MCC_AGENT_MINE=1</c>).</summary>
        Mine = 4,
    }

    /// <summary>
    /// Version 4 of the agent bridge: auto-fishing and auto-mining for the cloud's accounts (projects-studio
    /// <c>services/cloud-afk/README.md</c>, "Auto-fishing and auto-mining"). Each mode starts only with its variable,
    /// which only the cloud machine sets, and only together with the bridge's descriptors: the desktop launcher and the
    /// phone never set them, and no configuration key or console command turns a mode on.
    /// </summary>
    internal static class AgentAuto
    {
        public const string FishEnvVar = "MCC_AGENT_FISH";
        public const string MineEnvVar = "MCC_AGENT_MINE";

        /// <summary>The version <c>hello</c> says to a host that asked for a mode.</summary>
        public const int ProtocolVersion = 4;

        /// <summary>The parts the host asked for in this process.</summary>
        public static AgentFeatures Features { get; } = FeaturesOf(Environment.GetEnvironmentVariable(AgentMovement.MoveEnvVar),
            Environment.GetEnvironmentVariable(FishEnvVar), Environment.GetEnvironmentVariable(MineEnvVar));

        public static bool Fishing => (Features & AgentFeatures.Fish) != 0;
        public static bool Mining => (Features & AgentFeatures.Mine) != 0;

        private static volatile bool modeWorking;

        /// <summary>
        /// A mode is at work (its state is on, or it makes one of its tries while waiting): the client's own AntiAFK stays
        /// silent meanwhile, the mode's actions keep the account active. While the mode is paused, waiting or stopped
        /// AntiAFK runs as the account set it (the machine keeps it configured), so a server does not kick the account.
        /// Set by the bridge on every tick; never true without a mode's variable.
        /// </summary>
        internal static bool ModeWorking
        {
            get => modeWorking;
            set => modeWorking = value;
        }

        /// <summary>The parts these variables name: each only with the value <c>1</c>.</summary>
        internal static AgentFeatures FeaturesOf(string? move, string? fish, string? mine) =>
            (move == "1" ? AgentFeatures.Move : AgentFeatures.None)
            | (fish == "1" ? AgentFeatures.Fish : AgentFeatures.None)
            | (mine == "1" ? AgentFeatures.Mine : AgentFeatures.None);

        /// <summary>What <c>hello.features</c> lists: the parts the host asked for, by name.</summary>
        internal static List<string> FeatureNames(AgentFeatures features)
        {
            List<string> names = new();
            if ((features & AgentFeatures.Move) != 0)
                names.Add("move");
            if ((features & AgentFeatures.Fish) != 0)
                names.Add("fish");
            if ((features & AgentFeatures.Mine) != 0)
                names.Add("mine");
            return names;
        }

        /// <summary>
        /// The only blocks mining breaks: the cloud's <c>fixed.miningBlocks</c> (projects-studio
        /// <c>services/cloud-afk/feat/settings-v1.json</c>; a test holds them equal), stone kinds and ores, in its order.
        /// The client keeps its own copy as a second line behind the machine: a start naming anything else is refused.
        /// </summary>
        public static readonly string[] MiningBlocks =
        [
            "minecraft:cobblestone",
            "minecraft:stone",
            "minecraft:cobbled_deepslate",
            "minecraft:deepslate",
            "minecraft:andesite",
            "minecraft:diorite",
            "minecraft:granite",
            "minecraft:tuff",
            "minecraft:basalt",
            "minecraft:smooth_basalt",
            "minecraft:blackstone",
            "minecraft:netherrack",
            "minecraft:end_stone",
            "minecraft:obsidian",
            "minecraft:coal_ore",
            "minecraft:deepslate_coal_ore",
            "minecraft:copper_ore",
            "minecraft:deepslate_copper_ore",
            "minecraft:iron_ore",
            "minecraft:deepslate_iron_ore",
            "minecraft:gold_ore",
            "minecraft:deepslate_gold_ore",
            "minecraft:redstone_ore",
            "minecraft:deepslate_redstone_ore",
            "minecraft:lapis_ore",
            "minecraft:deepslate_lapis_ore",
            "minecraft:diamond_ore",
            "minecraft:deepslate_diamond_ore",
            "minecraft:emerald_ore",
            "minecraft:deepslate_emerald_ore",
            "minecraft:nether_gold_ore",
            "minecraft:nether_quartz_ore",
            "minecraft:ancient_debris",
        ];

        private static readonly HashSet<string> miningBlockSet = new(MiningBlocks, StringComparer.Ordinal);

        public static bool IsMiningBlock(string id) => miningBlockSet.Contains(id);
    }

    /// <summary>A mode's settings, from its <c>start</c> (version 4); the defaults are the cloud's.</summary>
    internal sealed record AutoSettings
    {
        /// <summary>Take another rod or pickaxe from the inventory (the hotbar first) when the one in hand is worn.</summary>
        public bool Switch { get; init; } = true;

        /// <summary>The durability left at which a rod or pickaxe is put away (1-100).</summary>
        public int Keep { get; init; } = 5;

        /// <summary><c>full: keep</c>: go on with a full inventory (what does not fit falls); else wait for room.</summary>
        public bool KeepWhenFull { get; init; }

        /// <summary>Fishing: a small turn of the head and back between casts.</summary>
        public bool Look { get; init; }

        /// <summary>Mining: the blocks it may break, a subset of <see cref="AgentAuto.MiningBlocks"/> in its order.</summary>
        public IReadOnlyList<string> Blocks { get; init; } = AgentAuto.MiningBlocks;

        /// <summary>Mining's idle actions between blocks: a look, a sneak, a jump, a step back and return.</summary>
        public bool IdleLook { get; init; }

        public bool IdleSneak { get; init; }
        public bool IdleJump { get; init; }
        public bool IdleStep { get; init; }

        /// <summary>One idle action at a random time within this many seconds (20 ≤ min ≤ max ≤ 3600).</summary>
        public int IdleMin { get; init; } = 60;

        public int IdleMax { get; init; } = 180;

        public bool AnyIdle => IdleLook || IdleSneak || IdleJump || IdleStep;

        /// <summary>
        /// Read a <c>start</c>'s settings: <c>rod</c> (fishing) or <c>tool</c> (mining) <c>{switch, keep}</c>,
        /// <c>full</c> (<c>wait</c> or <c>keep</c>), fishing's <c>look</c>, mining's <c>blocks</c> and <c>idle</c>.
        /// Every part is optional and takes the cloud's default; a part that is there must be right.
        /// </summary>
        public static bool TryParse(JsonElement root, bool mining, out AutoSettings? settings, out string? reason)
        {
            settings = null;
            reason = "bad_request";

            bool switchTool = true;
            int keep = 5;
            if (root.TryGetProperty(mining ? "tool" : "rod", out JsonElement tool))
            {
                if (tool.ValueKind != JsonValueKind.Object)
                    return false;
                if (tool.TryGetProperty("switch", out JsonElement switchElement)
                    && !TryReadBool(switchElement, out switchTool))
                    return false;
                if (tool.TryGetProperty("keep", out JsonElement keepElement)
                    && !AgentOperation.TryReadInt(keepElement, 1, AgentOperation.MaxKeep, out keep))
                    return false;
            }

            bool keepWhenFull = false;
            if (root.TryGetProperty("full", out JsonElement full))
            {
                if (full.ValueKind != JsonValueKind.String || full.GetString() is not ("wait" or "keep"))
                    return false;
                keepWhenFull = full.GetString() == "keep";
            }

            if (!mining)
            {
                bool look = false;
                if (root.TryGetProperty("look", out JsonElement lookElement) && !TryReadBool(lookElement, out look))
                    return false;

                settings = new AutoSettings { Switch = switchTool, Keep = keep, KeepWhenFull = keepWhenFull, Look = look };
                reason = null;
                return true;
            }

            IReadOnlyList<string> blocks = AgentAuto.MiningBlocks;
            if (root.TryGetProperty("blocks", out JsonElement blocksElement) && blocksElement.ValueKind != JsonValueKind.Null)
            {
                if (blocksElement.ValueKind != JsonValueKind.Array)
                    return false;
                int count = blocksElement.GetArrayLength();
                if (count is < 1 || count > AgentAuto.MiningBlocks.Length)
                    return false;

                HashSet<string> named = new(StringComparer.Ordinal);
                foreach (JsonElement block in blocksElement.EnumerateArray())
                {
                    if (block.ValueKind != JsonValueKind.String)
                        return false;
                    string id = block.GetString()!;
                    if (!AgentAuto.IsMiningBlock(id))
                    {
                        reason = "bad_block";
                        return false;
                    }

                    if (!named.Add(id))
                        return false;
                }

                // In the list's order, whatever order the host wrote.
                blocks = Array.FindAll(AgentAuto.MiningBlocks, named.Contains);
            }

            bool idleLook = false, idleSneak = false, idleJump = false, idleStep = false;
            int min = 60, max = 180;
            if (root.TryGetProperty("idle", out JsonElement idle))
            {
                if (idle.ValueKind != JsonValueKind.Object)
                    return false;
                if (idle.TryGetProperty("look", out JsonElement e) && !TryReadBool(e, out idleLook)
                    || idle.TryGetProperty("sneak", out e) && !TryReadBool(e, out idleSneak)
                    || idle.TryGetProperty("jump", out e) && !TryReadBool(e, out idleJump)
                    || idle.TryGetProperty("step", out e) && !TryReadBool(e, out idleStep)
                    || idle.TryGetProperty("min", out e)
                    && !AgentOperation.TryReadInt(e, AgentOperation.MinIdleSeconds, AgentOperation.MaxIdleSeconds, out min)
                    || idle.TryGetProperty("max", out e)
                    && !AgentOperation.TryReadInt(e, AgentOperation.MinIdleSeconds, AgentOperation.MaxIdleSeconds, out max))
                    return false;
                if (min > max)
                    return false;
            }

            settings = new AutoSettings
            {
                Switch = switchTool,
                Keep = keep,
                KeepWhenFull = keepWhenFull,
                Blocks = blocks,
                IdleLook = idleLook,
                IdleSneak = idleSneak,
                IdleJump = idleJump,
                IdleStep = idleStep,
                IdleMin = min,
                IdleMax = max,
            };
            reason = null;
            return true;
        }

        private static bool TryReadBool(JsonElement element, out bool value)
        {
            value = element.ValueKind == JsonValueKind.True;
            return element.ValueKind is JsonValueKind.True or JsonValueKind.False;
        }
    }
}
