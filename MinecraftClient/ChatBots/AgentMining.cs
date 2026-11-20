using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Auto-mining in place (agent bridge version 4, docs/agent-bridge.md): breaks the block the account looks at, when it
    /// is one of the list (a cobblestone generator's block, ores), never moving from its spot, with an idle action now and
    /// then. Not the stock AutoDig: its packets are the game's, in the game's order and timing, as the Projects anticheat
    /// (Grim) checks them:
    /// <list type="bullet">
    /// <item>the target is the block the look hits within <see cref="Reach"/> from the eyes, and the face is the one the
    /// look hits (PositionBreakA);</item>
    /// <item>the start and the finish go before the tick's move (Post), each with a swing, and a swing goes every tick in
    /// between (NoSwingBreak);</item>
    /// <item>the finish comes no sooner than the game's break time plus a tick (FastBreak, from the tool, its enchantments,
    /// the effects, the water and the ground), and the next start no sooner than 6 ticks after a finish, plus 0-3 at
    /// random (the game's 5-tick destroy delay);</item>
    /// <item>an abort faces down and has no sequence (PositionBreakB, BadPacketsH);</item>
    /// <item>the look, a tool change and the idle actions happen only between blocks, a look some ticks before a start
    /// (RotationBreak).</item>
    /// </list>
    /// </summary>
    internal sealed class AgentMining
    {
        public const double Reach = 4.5;            // survival's block interaction range
        public const double EyeHeight = 1.62, SneakingEyeHeight = 1.27;
        public const int FinishMarginTicks = 1;     // one tick more than the game's (it finishes n - 1 ticks after the start)
        public const int DestroyDelayTicks = 6;     // a finish, the game's 5 ticks of delay, then a start
        public const int RandomDelayTicks = 3;
        public const int NoTargetTicks = 200;       // nothing of the list in sight for 10 s: no_target
        public const int RetryTicks = 600;          // no_target is checked again every 30 s
        public const int CheckTicks = 60;           // the other waiting causes every 3 s (and on a change)
        public const int TargetGraceTicks = 40;     // a generator refills: the target stays said for 2 s of air
        public const int ToolReportTicks = 200;     // the pickaxe's wear alone is reported at most every 10 s
        public const int UpdateWaitTicks = 200;     // after a finish the server's word on that block comes first (10 s at most)
        public const double MovedAcross = 0.5, MovedUpDown = 1.0;
        public const int StepKeyTicks = 3;          // three ticks of a key from rest: about 0.9 blocks with the coast

        private enum Phase { Ready, LookBack, Digging, Cooldown, IdleLook, IdleSneak, IdleJump, IdleStepBack, IdleStepForth }

        private readonly IMineHost host;
        private readonly Action<AutoState> emitState;
        private readonly Action<string, int> emitDug;
        private readonly Random random;

        private AutoSettings settings = new();
        private HashSet<Material> blocks = new();
        private string state = "off";
        private string? reason;
        private AutoState? said;
        private long saidToolTick = long.MinValue;

        private long tick;
        private Phase phase = Phase.Ready;
        private long nextAt;            // the phase's next step
        private long startAllowedAt;    // the next start: a finish's destroy delay, a look's settling
        private long nextIdleAt;
        private long noTargetSince = -1;
        private long retryAt;
        private (int X, int Y, int Z)? pendingAbort; // a dig a menu cut short: its abort goes once the menu is closed
        private bool working;           // at work (on): AntiAFK stays silent meanwhile
        private long handOverAt;        // the tick the hand-over from AntiAFK releases the sneak key (0: none due)
        private long holdUntil;         // no action before this tick (the hand-over)

        // Where it mines: the spot and the look.
        private bool anchored;
        private Location anchor;
        private float anchorYaw, anchorPitch;

        // The block being broken, its face, and when it may be finished.
        private (int X, int Y, int Z) digAt;
        private Direction digFace;
        private Material digBlock;
        private long finishAt;
        private int digSlot;
        private bool awaitingUpdate;
        private long finishedAt;

        // What is said as the target: the block it looks at, kept through a generator's refill.
        private AutoTarget? target;
        private long targetSeenAt;

        // Idle actions.
        private long idleUntil;
        private Location stepFrom;
        private int stillTicks;

        public AgentMining(IMineHost host, Action<AutoState> emitState, Action<string, int> emitDug, Random? random = null)
        {
            this.host = host;
            this.emitState = emitState;
            this.emitDug = emitDug;
            this.random = random ?? new Random();
        }

        public string State => state;
        public string? Reason => reason;
        public bool Active => state != "off";

        /// <summary>The mode is at work (on): the client's AntiAFK stays silent meanwhile.</summary>
        public bool Working => working;

        /// <summary>
        /// The mode goes to work (it turns on): AntiAFK stops with the next tick, the sneak key it may have left down goes up
        /// on that tick (never in the same tick as AntiAFK's own toggle), and no action goes for three ticks after it; the
        /// anchor's look comes back before a start.
        /// </summary>
        private void BeginWork()
        {
            if (working)
                return;
            working = true;
            handOverAt = tick + 1;
            holdUntil = tick + 4;
        }

        /// <summary>Start, or take new settings while it runs (the spot stays). Null when accepted, else the reason.</summary>
        public string? Start(AutoSettings newSettings)
        {
            if (state != "off")
            {
                Apply(newSettings);
                Say();
                return null;
            }

            if (!host.Terrain)
                return "terrain_disabled";
            if (!host.Inventory)
                return "inventory_disabled";

            Apply(newSettings);
            ResetCycle();
            anchored = false;
            noTargetSince = -1;
            ScheduleIdle();
            if (host.Protocol < Protocol18Handler.MC_1_9_Version)
            {
                Set("off", "version");
                return null;
            }

            Set("on", null);
            return null;
        }

        private void Apply(AutoSettings newSettings)
        {
            settings = newSettings;
            blocks = newSettings.Blocks.Select(AgentAutoItems.MaterialOf).Where(m => m is not null).Select(m => m!.Value)
                .ToHashSet();
            if (newSettings.AnyIdle && nextIdleAt == 0)
                ScheduleIdle();
        }

        public void Stop()
        {
            if (state == "off")
                return;
            AbortDig();
            EndIdle();
            ResetCycle();
            target = null;
            Set("off", null);
        }

        /// <summary>The player (<c>player</c>) or an AFK+ deal (<c>trade</c>) pauses it.</summary>
        public string? Pause(string why)
        {
            if (state == "off")
                return "mode_off";
            AbortDig();
            EndIdle();
            ResetCycle();
            Set("paused", why);
            return null;
        }

        /// <summary>Go on from any pause, anchored where it stands and looks now.</summary>
        public string? Resume()
        {
            if (state == "off")
                return "mode_off";
            anchored = false;
            noTargetSince = -1;
            if (state == "paused")
            {
                ResetCycle();
                Set("on", null);
            }

            return null;
        }

        /// <summary>A new look to mine toward (also while paused): a dig in progress stops; the next start waits for the look.</summary>
        public void Aim(float yaw, float pitch)
        {
            anchorYaw = yaw;
            anchorPitch = pitch;
            if (phase == Phase.Digging)
                AbortDig();
            if (phase is not (Phase.IdleJump or Phase.IdleStepBack or Phase.IdleStepForth or Phase.IdleSneak))
            {
                if (!host.ScreenOpen)
                    host.Look(yaw, pitch);
                phase = Phase.Ready;
                startAllowedAt = Math.Max(startAllowedAt, tick + 3);
            }

            noTargetSince = -1;
        }

        /// <summary>A new session: the spot is where it joined; a window's pause ends.</summary>
        public void Joined()
        {
            working = false; // the hand-over comes again before the first action
            pendingAbort = null; // a new session has no dig
            if (phase == Phase.Digging)
                phase = Phase.Ready;
            ResetCycle();
            EndIdle(send: false);
            anchored = false;
            noTargetSince = -1;
            if (state == "paused" && reason == "window")
                Set("on", null);
            else if (state == "on")
                BeginWork();
        }

        public void Left()
        {
            working = false;
            pendingAbort = null;
            if (phase == Phase.Digging)
                phase = Phase.Ready;
            ResetCycle();
            EndIdle(send: false);
        }

        /// <summary>A death or a respawn moved the account.</summary>
        public void Moved()
        {
            if (state is "on" or "waiting")
            {
                ResetCycle();
                EndIdle(send: false);
                Set("paused", "moved");
            }
        }

        /// <summary>The server changed a block: after a finish, the one it broke (a generator may refill it at once).</summary>
        public void OnBlockChange(Location location, Material block)
        {
            if (awaitingUpdate && (int)Math.Floor(location.X) == digAt.X && (int)Math.Floor(location.Y) == digAt.Y
                && (int)Math.Floor(location.Z) == digAt.Z)
                awaitingUpdate = false;
        }

        public void OnInventoryChanged()
        {
            if (state == "waiting" && reason is "full" or "no_tool" or "tool_low")
                nextAt = Math.Min(nextAt, tick + 1);
        }

        /// <summary>One client tick, before its move: everything this mode sends goes from here.</summary>
        public void Tick()
        {
            tick++;
            if (state == "off" || !host.PositionKnown)
                return;

            if (host.WindowOpen)
            {
                AbortDig(); // the game aborts once the menu is closed
                if (phase is Phase.IdleSneak or Phase.IdleStepBack or Phase.IdleStepForth)
                    EndIdle();
                if (state is "on" or "waiting")
                    Set("paused", "window");
                return;
            }

            if (pendingAbort is { } cut && !host.ScreenOpen)
            {
                pendingAbort = null;
                host.Dig(1, new Location(cut.X, cut.Y, cut.Z), Direction.Down);
                startAllowedAt = Math.Max(startAllowedAt, tick + random.Next(2, 5));
            }

            if (state == "paused" && reason == "window")
            {
                ResetCycle();
                Set("on", null);
            }

            if (state == "paused")
                return;

            if (!anchored)
            {
                if (!host.OnGround)
                    return;
                anchored = true;
                anchor = host.Position;
                anchorYaw = host.Yaw;
                anchorPitch = host.Pitch;
            }
            else if (!Idling && Moved(host.Position))
            {
                AbortDig();
                ResetCycle();
                Set("paused", "moved");
                return;
            }

            if (host.ScreenOpen && phase != Phase.Digging)
                return; // the client's own swap: nothing in the hand changes until its screen closes

            if (handOverAt != 0 && tick >= handOverAt)
            {
                handOverAt = 0;
                host.ReleaseSneak();
            }

            if (tick < holdUntil && phase != Phase.Digging)
                return;

            switch (phase)
            {
                case Phase.Ready:
                    if (tick >= nextAt)
                        Prepare();
                    break;

                case Phase.LookBack:
                    if (tick >= nextAt)
                        phase = Phase.Ready;
                    break;

                case Phase.Digging:
                    Dig();
                    break;

                case Phase.Cooldown:
                    // The next start waits for the server's word on the block it broke: until then the client's view is the
                    // block as it was, and the anticheat's is air (Grim AirLiquidBreak flags a start or finish on air; e2e: a
                    // server's hiccup at another player's quit delayed the word past one second).
                    if (tick >= startAllowedAt && (!awaitingUpdate || tick - finishedAt >= UpdateWaitTicks))
                    {
                        awaitingUpdate = false;
                        phase = Phase.Ready;
                        if (tick >= nextAt)
                            Prepare();
                    }

                    break;

                default:
                    IdleTick();
                    break;
            }

            ReportTool();
        }

        private bool Idling => phase is Phase.IdleJump or Phase.IdleStepBack or Phase.IdleStepForth;

        /// <summary>Between blocks: the target, the tool, the room, an idle action, the look; then a start.</summary>
        private void Prepare()
        {
            if (host.Items is not { } items || !host.OnGround || host.Walking)
                return;

            // In waiting no_target the look is checked again every 30 s only.
            if (state == "waiting" && reason == "no_target" && tick < retryAt)
            {
                nextAt = retryAt;
                return;
            }

            var hit = TargetBlock();
            if (hit is null)
            {
                if (noTargetSince < 0)
                    noTargetSince = tick;
                if (target is not null && tick - targetSeenAt >= TargetGraceTicks)
                {
                    target = null;
                    Say();
                }

                if (tick - noTargetSince >= NoTargetTicks || state == "waiting" && reason == "no_target")
                {
                    if (!(state == "waiting" && reason == "no_target"))
                        Set("waiting", "no_target");
                    retryAt = tick + RetryTicks;
                    nextAt = retryAt;
                }

                if (!IdleDue())
                    return;
            }
            else
            {
                noTargetSince = -1;
                targetSeenAt = tick;
                AutoTarget seen = new(AgentAutoItems.BlockId(hit.Value.Block), hit.Value.X, hit.Value.Y, hit.Value.Z);
                if (seen != target)
                {
                    target = seen;
                    Say();
                }
            }

            items.TryGetValue(36 + host.HeldSlot, out Item? held);
            Material forTool = hit?.Block ?? blocks.FirstOrDefault(Material.Cobblestone);
            if (!Usable(held))
            {
                if (settings.Switch && FindTool(items, forTool) is { } tool)
                {
                    if (!working)
                    {
                        // A pickaxe came while it waited: on again, and the switch after the hand-over.
                        if (state == "waiting")
                            Set("on", null);
                        else
                            BeginWork();
                        return;
                    }

                    bool changed = tool.Slot >= 36 ? host.Hold(tool.Slot - 36) : host.Swap(tool.Slot, host.HeldSlot);
                    nextAt = tick + (changed ? random.Next(3, 7) : CheckTicks);
                    startAllowedAt = Math.Max(startAllowedAt, nextAt);
                    return;
                }

                Wait(AnyPickaxe(items) ? "tool_low" : "no_tool");
                return;
            }

            if (hit is { } room && !settings.KeepWhenFull && !Room(items, room.Block, held!))
            {
                Wait("full");
                return;
            }

            if (state == "waiting")
            {
                Set("on", null); // the hand-over first
                return;
            }

            if (IdleDue())
            {
                StartIdle();
                return;
            }

            if (hit is not { } block)
                return;

            // The server's word on the block broken last comes first (an aim, a pause or a menu can cut the cooldown short).
            if (awaitingUpdate && tick - finishedAt < UpdateWaitTicks)
                return;
            awaitingUpdate = false;

            // A start goes at the anchor's look, sent some ticks before.
            if (Math.Abs(AgentFishing.Delta(host.Yaw, anchorYaw)) > 0.01 || Math.Abs(host.Pitch - anchorPitch) > 0.01)
            {
                host.Look(anchorYaw, anchorPitch);
                phase = Phase.LookBack;
                nextAt = tick + 3;
                return;
            }

            if (tick < startAllowedAt)
                return;

            StartDig(block, held!);
        }

        private void StartDig((int X, int Y, int Z, Direction Face, Material Block) block, Item tool)
        {
            int ticks = host.DigTicks(block.Block, tool);
            if (ticks < 0)
                return; // unbreakable here
            digAt = (block.X, block.Y, block.Z);
            digFace = block.Face;
            digBlock = block.Block;
            digSlot = host.HeldSlot;
            if (!host.Dig(0, new Location(block.X, block.Y, block.Z), block.Face))
                return;
            host.Swing();
            if (ticks == 0)
            {
                Finished(); // an instant break: the start alone breaks it
                return;
            }

            // The game adds the first tick's progress in the start's tick: it finishes n - 1 ticks later; a tick more here.
            finishAt = tick + ticks - 1 + FinishMarginTicks;
            phase = Phase.Digging;
        }

        private void Dig()
        {
            // The block, the hand and the look must stay what the dig started with; else it stops.
            bool same = host.BlockAt(digAt.X, digAt.Y, digAt.Z) == digBlock && host.HeldSlot == digSlot
                        && !host.ScreenOpen && Usable(HeldItem())
                        && TargetBlock() is { } hit && (hit.X, hit.Y, hit.Z) == digAt && hit.Face == digFace;
            if (!same)
            {
                AbortDig();
                phase = Phase.Ready;
                startAllowedAt = Math.Max(startAllowedAt, tick + random.Next(2, 5));
                return;
            }

            if (tick >= finishAt)
            {
                host.Dig(2, new Location(digAt.X, digAt.Y, digAt.Z), digFace);
                host.Swing();
                Finished();
                return;
            }

            host.Swing();
        }

        private void Finished()
        {
            emitDug(AgentAutoItems.BlockId(digBlock), 1);
            finishedAt = tick;
            awaitingUpdate = true;
            startAllowedAt = tick + DestroyDelayTicks + random.Next(0, RandomDelayTicks + 1);
            phase = Phase.Cooldown;
        }

        /// <summary>Stop a dig in progress: an abort facing down, without a sequence, as the game's.</summary>
        private void AbortDig()
        {
            if (phase != Phase.Digging)
                return;
            phase = Phase.Ready;
            if (host.ScreenOpen)
                pendingAbort = digAt; // the game sends it once the screen is closed
            else
                host.Dig(1, new Location(digAt.X, digAt.Y, digAt.Z), Direction.Down);
        }

        /// <summary>
        /// The block the look hits within reach from the eyes, when it is one of the list, with the face the look enters
        /// it by; null when the look hits nothing, or first something else.
        /// </summary>
        private (int X, int Y, int Z, Direction Face, Material Block)? TargetBlock()
        {
            Location at = host.Position;
            double eyes = host.Sneaking && host.Protocol >= Protocol18Handler.MC_1_14_Version ? SneakingEyeHeight : EyeHeight;
            var hit = LookRay(host.BlockAt, at.X, at.Y + eyes, at.Z, anchorYaw, anchorPitch, Reach);
            if (hit is not { } block || !blocks.Contains(block.Block))
                return null;
            return block;
        }

        /// <summary>
        /// The first block a look hits (every block but air and fluids counts as a whole one: a look stops at a torch as
        /// the game's does at its outline), with the face it enters by; null within <paramref name="reach"/> none.
        /// </summary>
        internal static (int X, int Y, int Z, Direction Face, Material Block)? LookRay(Func<int, int, int, Material> blockAt,
            double eyeX, double eyeY, double eyeZ, float yaw, float pitch, double reach)
        {
            // The game's look vector (Entity.calculateViewVector).
            double yawRad = -yaw * Math.PI / 180, pitchRad = pitch * Math.PI / 180;
            double dirX = Math.Sin(yawRad) * Math.Cos(pitchRad);
            double dirY = -Math.Sin(pitchRad);
            double dirZ = Math.Cos(yawRad) * Math.Cos(pitchRad);

            int x = (int)Math.Floor(eyeX), y = (int)Math.Floor(eyeY), z = (int)Math.Floor(eyeZ);
            if (!Passable(blockAt(x, y, z)))
                return null; // the eyes are in a block

            int stepX = Math.Sign(dirX), stepY = Math.Sign(dirY), stepZ = Math.Sign(dirZ);
            double tDeltaX = stepX == 0 ? double.MaxValue : Math.Abs(1 / dirX);
            double tDeltaY = stepY == 0 ? double.MaxValue : Math.Abs(1 / dirY);
            double tDeltaZ = stepZ == 0 ? double.MaxValue : Math.Abs(1 / dirZ);
            double tMaxX = stepX == 0 ? double.MaxValue : (stepX > 0 ? x + 1 - eyeX : eyeX - x) * tDeltaX;
            double tMaxY = stepY == 0 ? double.MaxValue : (stepY > 0 ? y + 1 - eyeY : eyeY - y) * tDeltaY;
            double tMaxZ = stepZ == 0 ? double.MaxValue : (stepZ > 0 ? z + 1 - eyeZ : eyeZ - z) * tDeltaZ;

            while (true)
            {
                Direction face;
                double t;
                if (tMaxX <= tMaxY && tMaxX <= tMaxZ)
                {
                    t = tMaxX;
                    x += stepX;
                    tMaxX += tDeltaX;
                    face = stepX > 0 ? Direction.West : Direction.East;
                }
                else if (tMaxY <= tMaxZ)
                {
                    t = tMaxY;
                    y += stepY;
                    tMaxY += tDeltaY;
                    face = stepY > 0 ? Direction.Down : Direction.Up;
                }
                else
                {
                    t = tMaxZ;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    face = stepZ > 0 ? Direction.North : Direction.South;
                }

                if (t > reach)
                    return null;
                Material block = blockAt(x, y, z);
                if (!Passable(block))
                    return (x, y, z, face, block);
            }
        }

        private static bool Passable(Material block) =>
            block is Material.Air or Material.CaveAir or Material.VoidAir or Material.Water or Material.Lava
                or Material.BubbleColumn;

        private Item? HeldItem()
        {
            if (host.Items is not { } items)
                return null;
            items.TryGetValue(36 + host.HeldSlot, out Item? held);
            return held;
        }

        /// <summary>A pickaxe it may use: above its keep (each block wears it by 1 at most, so it never breaks).</summary>
        private bool Usable(Item? item)
        {
            if (item is null || item.IsEmpty || !AgentAutoItems.IsPickaxe(item.Type))
                return false;
            return AgentAutoItems.Durability(item) is not { } durability || durability.Left > settings.Keep
                                                                          || durability.Left == durability.Max;
        }

        /// <summary>The fastest pickaxe it may use for this block: the hotbar first, then the main inventory.</summary>
        private (int Slot, Item Item)? FindTool(IReadOnlyDictionary<int, Item> items, Material block)
        {
            foreach (bool hotbar in new[] { true, false })
            {
                (int Slot, Item Item)? best = null;
                (int Ticks, int Left) bestScore = (int.MaxValue, 0);
                foreach (int slot in AgentFishing.HotbarThenMain())
                {
                    if (slot >= 36 != hotbar || slot == 36 + host.HeldSlot || !items.TryGetValue(slot, out Item? item)
                        || !Usable(item))
                        continue;
                    int ticks = host.DigTicks(block, item);
                    if (ticks < 0)
                        ticks = int.MaxValue - 1;
                    int left = AgentAutoItems.Durability(item)?.Left ?? 0;
                    if (ticks < bestScore.Ticks || ticks == bestScore.Ticks && left > bestScore.Left)
                    {
                        bestScore = (ticks, left);
                        best = (slot, item);
                    }
                }

                if (best is not null)
                    return best;
            }

            return null;
        }

        private static bool AnyPickaxe(IReadOnlyDictionary<int, Item> items)
        {
            foreach ((int slot, Item item) in items)
            {
                if (AgentAutoItems.IsStorage(slot) && AgentAutoItems.IsPickaxe(item.Type))
                    return true;
            }

            return false;
        }

        /// <summary>The inventory has room for what this block drops with this tool.</summary>
        private bool Room(IReadOnlyDictionary<int, Item> items, Material block, Item tool)
        {
            bool silk = MiningCalculator.GetEnchantmentLevel(tool, Enchantments.SilkTouch, host.Protocol) > 0;
            ItemType? drop = Drop(block, silk, host.Protocol);
            return drop is { } type ? AgentAutoItems.RoomFor(items, type) : !AgentAutoItems.NoEmptySlot(items);
        }

        /// <summary>What a block of the list drops (the game's loot tables; with Silk Touch the block itself).</summary>
        internal static ItemType? Drop(Material block, bool silkTouch, int protocol)
        {
            bool raw = protocol >= Protocol18Handler.MC_1_17_Version;
            ItemType? self = Enum.TryParse(block.ToString(), out ItemType item) ? item : null;
            if (silkTouch)
                return self;
            return block switch
            {
                Material.Stone => ItemType.Cobblestone,
                Material.Deepslate => ItemType.CobbledDeepslate,
                Material.CoalOre or Material.DeepslateCoalOre => ItemType.Coal,
                Material.CopperOre or Material.DeepslateCopperOre => ItemType.RawCopper,
                Material.IronOre or Material.DeepslateIronOre => raw ? ItemType.RawIron : self,
                Material.GoldOre or Material.DeepslateGoldOre => raw ? ItemType.RawGold : self,
                Material.RedstoneOre or Material.DeepslateRedstoneOre => ItemType.Redstone,
                Material.LapisOre or Material.DeepslateLapisOre => ItemType.LapisLazuli,
                Material.DiamondOre or Material.DeepslateDiamondOre => ItemType.Diamond,
                Material.EmeraldOre or Material.DeepslateEmeraldOre => ItemType.Emerald,
                Material.NetherGoldOre => ItemType.GoldNugget,
                Material.NetherQuartzOre => ItemType.Quartz,
                _ => self,
            };
        }

        // --- idle actions, between blocks only ------------------------------------------------------------------------

        private bool IdleDue() => settings.AnyIdle && nextIdleAt > 0 && tick >= nextIdleAt && phase == Phase.Ready;

        private void ScheduleIdle()
        {
            nextIdleAt = settings.AnyIdle
                ? tick + random.Next(settings.IdleMin * 20, settings.IdleMax * 20 + 1)
                : 0;
        }

        private void StartIdle()
        {
            List<Phase> choices = new();
            if (settings.IdleLook)
                choices.Add(Phase.IdleLook);
            if (settings.IdleSneak)
                choices.Add(Phase.IdleSneak);
            if (settings.IdleJump)
                choices.Add(Phase.IdleJump);
            if (settings.IdleStep && SafeStepBack())
                choices.Add(Phase.IdleStepBack);
            ScheduleIdle();
            if (choices.Count == 0)
                return; // the step alone, and no safe floor behind: not done

            phase = choices[random.Next(choices.Count)];
            switch (phase)
            {
                case Phase.IdleLook:
                    float yaw = (float)Math.Round(anchorYaw + (random.Next(2) * 2 - 1) * (5 + random.NextDouble() * 10), 1);
                    float pitch = (float)Math.Round(Math.Clamp(anchorPitch + (random.NextDouble() * 2 - 1) * 8, -90, 90), 1);
                    host.Look(yaw, pitch);
                    idleUntil = tick + random.Next(6, 15);
                    break;
                case Phase.IdleSneak:
                    host.Sneak(true);
                    idleUntil = tick + random.Next(8, 21);
                    break;
                case Phase.IdleJump:
                    host.Jump();
                    idleUntil = tick + 4; // airborne by then; it ends once it stands again
                    break;
                case Phase.IdleStepBack:
                    stepFrom = host.Position;
                    host.StepKey(1);
                    idleUntil = tick + StepKeyTicks;
                    stillTicks = 0;
                    break;
            }
        }

        private void IdleTick()
        {
            switch (phase)
            {
                case Phase.IdleLook:
                    if (tick >= idleUntil)
                    {
                        host.Look(anchorYaw, anchorPitch);
                        EndIdleTo(3);
                    }

                    break;

                case Phase.IdleSneak:
                    if (tick >= idleUntil)
                    {
                        host.Sneak(false);
                        EndIdleTo(5);
                    }

                    break;

                case Phase.IdleJump:
                    if (tick >= idleUntil && host.OnGround)
                        EndIdleTo(6);
                    break;

                case Phase.IdleStepBack:
                case Phase.IdleStepForth:
                    if (tick == idleUntil)
                        host.StepKey(0);
                    if (tick < idleUntil)
                        break;
                    // Wait until it stands still, then the way back (the same keys the other way: the same distance).
                    Location now = host.Position;
                    stillTicks = Math.Abs(now.X - stepFrom.X) < 1e-4 && Math.Abs(now.Z - stepFrom.Z) < 1e-4 ? stillTicks + 1 : 0;
                    stepFrom = now;
                    if (stillTicks < 3)
                        break;
                    if (phase == Phase.IdleStepBack)
                    {
                        phase = Phase.IdleStepForth;
                        host.StepKey(2);
                        idleUntil = tick + StepKeyTicks;
                        stillTicks = 0;
                    }
                    else
                    {
                        EndIdleTo(4);
                        if (Moved(host.Position, 0.1))
                        {
                            ResetCycle();
                            Set("paused", "moved");
                        }
                    }

                    break;
            }
        }

        private void EndIdleTo(int settleTicks)
        {
            phase = Phase.Ready;
            nextAt = tick + settleTicks;
            startAllowedAt = Math.Max(startAllowedAt, tick + settleTicks);
        }

        /// <summary>An idle action cut short (a pause, a menu): its key goes up and the look comes back.</summary>
        private void EndIdle(bool send = true)
        {
            // The keys go up in any case (a new session's server knows none, so nothing goes then).
            if (phase == Phase.IdleSneak)
                host.Sneak(false);
            if (phase is Phase.IdleStepBack or Phase.IdleStepForth)
                host.StepKey(0);
            if (send && phase == Phase.IdleLook && !host.ScreenOpen)
                host.Look(anchorYaw, anchorPitch);

            if (phase >= Phase.IdleLook)
                phase = Phase.Ready;
        }

        /// <summary>A step back is safe: the block behind is free at the feet and the head, and solid ground is under it.</summary>
        private bool SafeStepBack()
        {
            string facing = AgentMovement.FacingOf(anchorYaw);
            (int dx, int dz) = AgentMovement.Delta(facing);
            int x = (int)Math.Floor(anchor.X) - dx, z = (int)Math.Floor(anchor.Z) - dz, y = (int)Math.Floor(anchor.Y);
            return Passable(host.BlockAt(x, y, z)) && !IsLiquid(host.BlockAt(x, y, z))
                   && Passable(host.BlockAt(x, y + 1, z)) && !IsLiquid(host.BlockAt(x, y + 1, z))
                   && SolidFloor(host.BlockAt(x, y - 1, z)) && SolidFloor(host.BlockAt((int)Math.Floor(anchor.X), y - 1,
                       (int)Math.Floor(anchor.Z)));
        }

        private static bool IsLiquid(Material block) => block is Material.Water or Material.Lava or Material.BubbleColumn;

        private static bool SolidFloor(Material block) =>
            !Passable(block) && block.IsSolid() && block is not (Material.MagmaBlock or Material.Cactus or Material.Fire
                or Material.SoulFire or Material.PowderSnow or Material.Scaffolding);

        // --- state ----------------------------------------------------------------------------------------------------

        private bool Moved(Location now, double across = MovedAcross) =>
            Math.Sqrt((now.X - anchor.X) * (now.X - anchor.X) + (now.Z - anchor.Z) * (now.Z - anchor.Z)) > across
            || Math.Abs(now.Y - anchor.Y) > MovedUpDown;

        private void Wait(string why)
        {
            working = false;
            if (state != "waiting" || reason != why)
                Set("waiting", why);
            nextAt = tick + CheckTicks;
        }

        private void ResetCycle()
        {
            if (phase is Phase.Digging or Phase.Cooldown or Phase.LookBack)
                phase = Phase.Ready;
            nextAt = tick + random.Next(3, 8);
            startAllowedAt = Math.Max(startAllowedAt, nextAt);
        }

        private void Set(string newState, string? newReason)
        {
            state = newState;
            reason = newReason;
            if (newState == "on")
                BeginWork();
            else
                working = false;
            Say();
        }

        private void Say()
        {
            AutoState now = new(state, reason, null, Pickaxe(), state == "off" ? null : target);
            if (now == said)
                return;
            said = now;
            saidToolTick = tick;
            emitState(now);
        }

        /// <summary>The pickaxe's wear alone is said at most every 10 s; any other change at once.</summary>
        private void ReportTool()
        {
            if (said is null)
                return;
            AutoTool? tool = Pickaxe();
            if (tool == said.Tool)
                return;
            bool onlyWear = tool is not null && said.Tool is not null && tool.Item == said.Tool.Item && tool.Max == said.Tool.Max;
            if (onlyWear && tick - saidToolTick < ToolReportTicks)
                return;
            Say();
        }

        private AutoTool? Pickaxe()
        {
            Item? held = HeldItem();
            return held is not null && AgentAutoItems.IsPickaxe(held.Type) ? AgentAutoItems.Tool(held) : null;
        }
    }
}
