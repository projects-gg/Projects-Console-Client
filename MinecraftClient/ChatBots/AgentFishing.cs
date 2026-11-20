using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Auto-fishing (agent bridge version 4, docs/agent-bridge.md): casts where the account stands and looks, reels in at
    /// a bite after a human's delay, counts what it caught and never breaks a rod. Its packets go in the game's order and
    /// timing: every action is taken on the client's tick before the tick's move; a cast and a reel are a use of the rod
    /// (with the swing the game sends before 26.3) at a look the server already has; a slot change or a swap comes some
    /// ticks before a use, never in its tick.
    /// <para>
    /// A bite is the bobber's own "biting" flag (1.16+, at <see cref="BitingIndex"/>), else (before 1.16) the bobber pulled
    /// down or its splash next to it. A cast fails when its bobber does not come to rest in water or nothing bites within
    /// <see cref="BiteTicks"/>; after <see cref="FailedCasts"/> failures in a row it waits (<c>no_water</c>) and tries
    /// again every <see cref="RetryTicks"/>.
    /// </para>
    /// </summary>
    internal sealed class AgentFishing
    {
        public const int BobberTicks = 40;          // a cast's bobber must appear within 2 s
        public const int SettleTicks = 100;         // ...and come to rest within 5 s
        public const int RestTicks = 40;            // no move for 2 s: it lies still (on the ground, out of water)
        public const int BiteTicks = 1200;          // a bite within 60 s (the contract's no_water)
        public const int FailedCasts = 5;
        public const int RetryTicks = 600;          // no_water is tried again every 30 s
        public const int CheckTicks = 60;           // a waiting cause is checked again every 3 s (and on a change)
        public const int CatchTicks = 80;           // what comes in within 4 s of a reel was caught
        public const int ToolReportTicks = 200;     // the rod's wear alone is reported at most every 10 s
        public const int ReelMinTicks = 3, ReelMaxTicks = 9;        // 150-450 ms after the bite
        public const int RecastMinTicks = 12, RecastMaxTicks = 32;  // 0.6-1.6 s after the reel
        public const double MovedAcross = 0.5, MovedUpDown = 1.0;   // beyond this from the anchor: moved
        public const int GroundReelCost = 2;        // a reel from the ground wears a rod by 2 (FishingHook.retrieve)

        private enum Phase { Ready, LookAway, LookBack, Casting, Waiting, Biting, Reeled }

        private readonly IAutoHost host;
        private readonly Action<AutoState> emitState;
        private readonly Action<string, int, string, bool> emitCatch;
        private readonly Random random;

        private AutoSettings settings = new();
        private string state = "off";
        private string? reason;
        private string? doing;
        private AutoState? said;
        private long saidToolTick = long.MinValue;

        private long tick;
        private Phase phase = Phase.Ready;
        private long nextAt;            // the phase's next step
        private long phaseStart;
        private int failed;             // casts in a row that failed
        private bool aimAgain;          // a new look: reel in and cast again toward it
        private bool hooked;            // the bobber caught a mob or an item: reel it in
        private bool reelOnTick;        // a pause by the player or a stop: reel in on the next tick
        private long retryAt;           // no_water: the next try
        private bool working;           // at work: on, or a try while waiting (AntiAFK stays silent meanwhile)
        private long handOverAt;        // the tick the hand-over from AntiAFK releases the sneak key (0: none due)
        private long holdUntil;         // no action before this tick (the hand-over)

        // Where it fishes: the spot and the look, set at the start, a resume and a join.
        private bool anchored;
        private Location anchor;
        private float anchorYaw, anchorPitch;

        // The bobber of the last cast.
        private int? bobber;
        private Location bobberAt;
        private long bobberSeenAt;      // its last move (any)
        private long bobberWentAt;      // its last move across (still flying)
        private bool inWater;

        // What a reel brought.
        private Dictionary<int, (string Id, int Count, bool Enchanted)>? catchBefore;
        private long catchUntil;

        public AgentFishing(IAutoHost host, Action<AutoState> emitState, Action<string, int, string, bool> emitCatch,
            Random? random = null)
        {
            this.host = host;
            this.emitState = emitState;
            this.emitCatch = emitCatch;
            this.random = random ?? new Random();
        }

        public string State => state;
        public string? Reason => reason;
        public bool Active => state != "off";

        /// <summary>
        /// The mode is at work: on, or making one of its tries while it waits. The client's AntiAFK stays silent meanwhile
        /// and runs while the mode is paused, waiting or stopped.
        /// </summary>
        public bool Working => working;

        /// <summary>
        /// The mode goes to work (it turns on, or a no_water try begins): AntiAFK stops with the next tick, the sneak key it
        /// may have left down goes up on that tick (never in the same tick as AntiAFK's own toggle), and no action goes for
        /// three ticks after it; the anchor's look comes back before a cast.
        /// </summary>
        private void BeginWork()
        {
            if (working)
                return;
            working = true;
            handOverAt = tick + 1;
            holdUntil = tick + 4;
        }

        /// <summary>The bobber's biting flag in the entity data (FishingHook.DATA_BITING), or null before 1.16.</summary>
        internal static int? BitingIndex(int protocol) => protocol switch
        {
            // The Entity's own entries come first: seven before 1.17, eight from 1.17 (ticks frozen); then hooked, biting.
            >= Protocol18Handler.MC_1_17_Version => 9,
            >= Protocol18Handler.MC_1_16_Version => 8,
            _ => null,
        };

        /// <summary>The bobber's hooked entity in the entity data (FishingHook.DATA_HOOKED_ENTITY): its id plus one, or 0.</summary>
        internal static int HookedIndex(int protocol) => protocol >= Protocol18Handler.MC_1_17_Version ? 8 : 7;

        /// <summary>Start, or take new settings while it runs (the spot stays). Null when accepted, else the reason.</summary>
        public string? Start(AutoSettings newSettings)
        {
            if (state != "off")
            {
                settings = newSettings;
                Say();
                return null;
            }

            if (!host.Entities)
                return "entities_disabled";
            if (!host.Inventory)
                return "inventory_disabled";

            settings = newSettings;
            ResetCycle();
            failed = 0;
            anchored = false;
            if (host.Protocol < Protocol18Handler.MC_1_9_Version)
            {
                Set("off", "version");
                return null;
            }

            Set("on", null);
            return null;
        }

        public void Stop()
        {
            if (state == "off")
                return;
            reelOnTick = bobber is not null; // a rod is not left out
            ResetCycle(keepBobber: reelOnTick);
            Set("off", null);
            working = reelOnTick;
        }

        /// <summary>The player (<c>player</c>) or an AFK+ deal (<c>trade</c>) pauses it.</summary>
        public string? Pause(string why)
        {
            if (state == "off")
                return "mode_off";
            reelOnTick = why == "player" && bobber is not null; // a player who stops it reels in
            ResetCycle(keepBobber: reelOnTick);
            Set("paused", why);
            working = reelOnTick; // until the reel is in
            return null;
        }

        /// <summary>Go on from any pause, anchored where it stands and looks now.</summary>
        public string? Resume()
        {
            if (state == "off")
                return "mode_off";
            anchored = false;
            failed = 0;
            if (state == "paused")
            {
                ResetCycle();
                Set("on", null);
            }

            return null;
        }

        /// <summary>A new look to fish toward (also while paused): a bobber out is reeled in and cast again toward it.</summary>
        public void Aim(float yaw, float pitch)
        {
            anchorYaw = yaw;
            anchorPitch = pitch;
            if (!host.ScreenOpen)
                host.Look(yaw, pitch);
            aimAgain = bobber is not null && phase is Phase.Casting or Phase.Waiting;
        }

        /// <summary>A new session: the spot is where it joined; a window's pause ends (no window is left).</summary>
        public void Joined()
        {
            ResetCycle();
            working = false; // the hand-over comes again before the first action
            anchored = false;
            if (state == "paused" && reason == "window")
                Set("on", null);
            else if (state == "on")
                BeginWork();
        }

        public void Left()
        {
            ResetCycle();
            working = false;
        }

        /// <summary>A death or a respawn moved the account.</summary>
        public void Moved()
        {
            if (state is "on" or "waiting")
            {
                ResetCycle();
                Set("paused", "moved");
            }
        }

        public void OnEntitySpawn(int id, EntityType type, int owner, Location location)
        {
            if (type != EntityType.FishingBobber || owner != host.PlayerEntityId || phase != Phase.Casting)
                return;
            bobber = id;
            bobberAt = location;
            bobberSeenAt = bobberWentAt = tick;
            inWater = false;
        }

        public void OnEntityMove(int id, Location location)
        {
            if (id != bobber)
                return;
            if (Math.Abs(location.X - bobberAt.X) > 0.05 || Math.Abs(location.Z - bobberAt.Z) > 0.05)
                bobberWentAt = tick;
            bobberSeenAt = tick;
            bobberAt = location;
        }

        public void OnEntityDespawn(int id)
        {
            if (id != bobber)
                return;
            bobber = null;
            if (phase is Phase.Casting or Phase.Waiting or Phase.Biting)
            {
                // Gone without a reel (the server took it away): cast again in a moment.
                phase = Phase.Ready;
                nextAt = tick + random.Next(RecastMinTicks, RecastMaxTicks + 1);
            }
        }

        public void OnEntityMetadata(int id, Dictionary<int, object?> metadata)
        {
            if (id != bobber)
                return;
            if (BitingIndex(host.Protocol) is { } index && metadata.TryGetValue(index, out object? value)
                                                        && value is true)
            {
                inWater = true; // only a bobber in water gets a bite
                Bite();
            }

            if (metadata.TryGetValue(HookedIndex(host.Protocol), out object? caught) && caught is int entity && entity != 0)
                hooked = true;
        }

        /// <summary>Before 1.16, a bite pulls the bobber down.</summary>
        public void OnEntityVelocity(int id, double vx, double vy, double vz)
        {
            if (id == bobber && BitingIndex(host.Protocol) is null && phase == Phase.Waiting && vy <= -0.2)
                Bite();
        }

        /// <summary>Before 1.16, a bite splashes next to the bobber.</summary>
        public void OnSound(string? name, Location? location)
        {
            if (bobber is null || BitingIndex(host.Protocol) is not null || phase != Phase.Waiting || location is not { } at
                || name is null || !name.EndsWith("fishing_bobber.splash", StringComparison.Ordinal))
                return;
            if (at.Distance(bobberAt) <= 1.5)
                Bite();
        }

        /// <summary>The inventory changed: a waiting cause may be gone, a catch may have come in.</summary>
        public void OnInventoryChanged()
        {
            if (state == "waiting" && reason is "full" or "no_rod" or "rod_low")
                nextAt = Math.Min(nextAt, tick + 1);
            CollectCatch();
        }

        /// <summary>One client tick, before its move: everything this mode sends goes from here.</summary>
        public void Tick()
        {
            tick++;
            if (reelOnTick && host.PositionKnown && !host.ScreenOpen)
            {
                reelOnTick = false;
                host.Use();
                bobber = null;
                working = state == "on";
                return;
            }

            if (state == "off" || !host.PositionKnown)
                return;

            // A menu the server opened pauses it; it goes on by itself when the menu closes.
            if (host.WindowOpen)
            {
                if (state is "on" or "waiting")
                    Set("paused", "window");
                return;
            }

            if (state == "paused" && reason == "window")
            {
                Set("on", null);
                if (phase == Phase.Ready)
                    nextAt = Math.Max(nextAt, tick + random.Next(RecastMinTicks, RecastMaxTicks + 1));
            }

            CollectCatch();
            if (state == "paused")
                return;

            if (!anchored)
            {
                anchored = true;
                anchor = host.Position;
                anchorYaw = host.Yaw;
                anchorPitch = host.Pitch;
            }
            else if (Moved(host.Position))
            {
                ResetCycle();
                Set("paused", "moved");
                return;
            }

            if (host.ScreenOpen)
                return; // the client's own swap: nothing in the hand changes until its screen closes

            if (handOverAt != 0 && tick >= handOverAt)
            {
                handOverAt = 0;
                host.ReleaseSneak();
            }

            if (tick < holdUntil)
                return;

            switch (phase)
            {
                case Phase.Ready:
                    if (tick >= nextAt)
                        Prepare();
                    break;

                case Phase.LookAway:
                    if (tick >= nextAt)
                    {
                        host.Look(anchorYaw, anchorPitch);
                        phase = Phase.LookBack;
                        nextAt = tick + random.Next(2, 5);
                    }

                    break;

                case Phase.LookBack:
                    if (tick >= nextAt)
                        Cast();
                    break;

                case Phase.Casting:
                    WatchCast();
                    break;

                case Phase.Waiting:
                    if (hooked)
                        FreeHook();
                    else if (aimAgain)
                        Reel(failedCast: false);
                    else if (tick - phaseStart >= BiteTicks)
                        Reel(failedCast: true);
                    break;

                case Phase.Biting:
                    if (tick >= nextAt)
                        Reel(failedCast: false, caught: true);
                    break;

                case Phase.Reeled:
                    if (tick >= nextAt)
                        phase = Phase.Ready;
                    break;
            }

            ReportTool();
        }

        /// <summary>Before a cast: the rod, the room, the look.</summary>
        private void Prepare()
        {
            if (host.Items is not { } items)
                return;

            items.TryGetValue(36 + host.HeldSlot, out Item? held);
            if (!Usable(held))
            {
                if (settings.Switch && FindRod(items) is { } rod)
                {
                    if (!working)
                    {
                        BeginWork(); // a switch is work: the hand-over first
                        nextAt = holdUntil;
                        return;
                    }

                    bool changed = rod.Slot >= 36 ? host.Hold(rod.Slot - 36) : host.Swap(rod.Slot, host.HeldSlot);
                    nextAt = tick + (changed ? random.Next(3, 7) : CheckTicks); // the cast goes a few ticks after the change
                    return;
                }

                Wait(AnyRod(items) ? "rod_low" : "no_rod");
                return;
            }

            if (!settings.KeepWhenFull && AgentAutoItems.NoEmptySlot(items))
            {
                Wait("full");
                return;
            }

            if (failed >= FailedCasts)
            {
                if (!(state == "waiting" && reason == "no_water"))
                {
                    Wait("no_water", RetryTicks);
                    return;
                }

                if (tick < retryAt)
                {
                    working = false; // between tries it waits, and AntiAFK keeps the account active
                    nextAt = retryAt;
                    return;
                }

                // One try every 30 s: a bobber in water says "on" again, a failure keeps it waiting.
                retryAt = tick + RetryTicks;
                failed = FailedCasts - 1;
                BeginWork();
                nextAt = holdUntil;
                return;
            }

            if (state == "waiting" && reason != "no_water")
            {
                Set("on", null); // the hand-over first
                nextAt = holdUntil;
                return;
            }

            // A cast goes at the anchor's look, which the server already has.
            if (Math.Abs(Delta(host.Yaw, anchorYaw)) > 0.01 || Math.Abs(host.Pitch - anchorPitch) > 0.01)
            {
                host.Look(anchorYaw, anchorPitch);
                phase = Phase.LookBack;
                nextAt = tick + 2;
                return;
            }

            if (settings.Look && random.NextDouble() < 0.6)
            {
                float yaw = (float)Math.Round(anchorYaw + (random.NextDouble() * 2 - 1) * 12, 1);
                float pitch = (float)Math.Round(Math.Clamp(anchorPitch + (random.NextDouble() * 2 - 1) * 6, -90, 90), 1);
                host.Look(yaw, pitch);
                phase = Phase.LookAway;
                nextAt = tick + random.Next(4, 11);
                return;
            }

            Cast();
        }

        private void Cast()
        {
            if (!host.Use())
            {
                phase = Phase.Ready;
                nextAt = tick + CheckTicks;
                return;
            }

            phase = Phase.Casting;
            phaseStart = tick;
            bobber = null;
            inWater = false;
            hooked = false;
            aimAgain = false;
            doing = "cast";
            Say();
        }

        private void WatchCast()
        {
            if (bobber is null)
            {
                if (tick - phaseStart >= BobberTicks)
                    Failed(); // no bobber came: the server took no cast
                return;
            }

            if (hooked)
            {
                FreeHook();
                return;
            }

            if (aimAgain)
            {
                Reel(failedCast: false);
                return;
            }

            inWater |= InWater();
            if (inWater && tick - bobberWentAt >= 10)
            {
                phase = Phase.Waiting;
                phaseStart = tick;
                if (state == "waiting")
                {
                    state = "on"; // a no_water try came to rest in water
                    reason = null;
                }

                doing = "wait";
                Say();
                return;
            }

            // At rest out of water (on the ground), or never at rest: a failed cast.
            if (tick - bobberSeenAt >= RestTicks && !inWater || tick - phaseStart >= SettleTicks)
                Reel(failedCast: true);
        }

        /// <summary>
        /// The bobber is in water: the terrain says so, else (without terrain) it stopped going across but still moves, as
        /// the server bobs it in water (on the ground it lies still and no move comes).
        /// </summary>
        private bool InWater()
        {
            if (host.Terrain && host is IMineHost world)
            {
                int x = (int)Math.Floor(bobberAt.X), z = (int)Math.Floor(bobberAt.Z);
                return IsWater(world.BlockAt(x, (int)Math.Floor(bobberAt.Y), z))
                       || IsWater(world.BlockAt(x, (int)Math.Floor(bobberAt.Y - 0.25), z));
            }

            return tick - phaseStart >= 20 && tick - bobberWentAt >= 10 && tick - bobberSeenAt <= 15
                   && bobberSeenAt > bobberWentAt;
        }

        private static bool IsWater(Material material) => material is Material.Water or Material.BubbleColumn;

        private void Bite()
        {
            if (phase is not (Phase.Waiting or Phase.Casting) || bobber is null)
                return;
            phase = Phase.Biting;
            nextAt = tick + random.Next(ReelMinTicks, ReelMaxTicks + 1);
        }

        private void Reel(bool failedCast, bool caught = false)
        {
            if (host.Items is { } items)
            {
                catchBefore = AgentAutoItems.Snapshot(items);
                catchUntil = tick + CatchTicks;
            }

            host.Use();
            bobber = null;
            aimAgain = false;
            hooked = false;
            phase = Phase.Reeled;
            nextAt = tick + random.Next(RecastMinTicks, RecastMaxTicks + 1);
            if (caught)
                failed = 0;
            else if (failedCast)
                failed++;
            doing = "reel";
            Say();
        }

        /// <summary>
        /// The bobber caught a mob or an item. A reel would wear the rod by up to 5: with less left, the rod is put away a
        /// moment instead (the hook goes when no rod is held), so a rod never breaks.
        /// </summary>
        private void FreeHook()
        {
            AutoTool? rod = HeldRod();
            if (rod is null || rod.Dur > 5 || rod.Dur == rod.Max)
            {
                Reel(failedCast: true);
                return;
            }

            host.Hold(host.HeldSlot == 8 ? 7 : host.HeldSlot + 1);
            bobber = null;
            hooked = false;
            failed++;
            phase = Phase.Ready;
            nextAt = tick + random.Next(RecastMinTicks, RecastMaxTicks + 1);
        }

        private void Failed()
        {
            failed++;
            phase = Phase.Ready;
            nextAt = tick + random.Next(RecastMinTicks, RecastMaxTicks + 1);
        }

        /// <summary>
        /// What a reel caught: the first item of the game's fishing loot that came into the inventory within 4 s of it (a
        /// reel brings one stack). Anything else that comes in meanwhile (a server's gift, a pick-up) is not a catch.
        /// </summary>
        private void CollectCatch()
        {
            if (catchBefore is null || host.Items is not { } items)
                return;
            Dictionary<int, (string Id, int Count, bool Enchanted)> now = AgentAutoItems.Snapshot(items);
            foreach ((string id, int count, bool enchanted) in AgentAutoItems.Added(catchBefore, now))
            {
                if (!AgentAutoItems.FishingLoot.Contains(id))
                    continue;
                emitCatch(id, count, AgentAutoItems.Category(id, enchanted), enchanted);
                catchBefore = null;
                return;
            }

            catchBefore = tick < catchUntil ? now : null;
        }

        private void Wait(string why, int ticks = CheckTicks)
        {
            working = false;
            if (state != "waiting" || reason != why)
                Set("waiting", why);
            if (why == "no_water")
                retryAt = tick + ticks;
            phase = Phase.Ready;
            nextAt = tick + ticks;
        }

        /// <summary>A rod it may use: never one a reel from the ground could break, nor one at its keep.</summary>
        private bool Usable(Item? item)
        {
            if (item is null || item.IsEmpty || item.Type != ItemType.FishingRod)
                return false;
            if (AgentAutoItems.Durability(item) is not { } durability)
                return true;
            return durability.Left > settings.Keep && durability.Left > GroundReelCost;
        }

        private (int Slot, Item Item)? FindRod(IReadOnlyDictionary<int, Item> items)
        {
            (int Slot, Item Item)? best = null;
            long bestScore = long.MinValue;
            int held = 36 + host.HeldSlot;
            foreach (int slot in HotbarThenMain())
            {
                if (slot == held || !items.TryGetValue(slot, out Item? item) || !Usable(item))
                    continue;
                // The hotbar first; then the best enchanted, then the least worn.
                long score = (slot >= 36 ? 1L << 40 : 0)
                             + (MiningCalculator.GetEnchantmentLevel(item, Enchantments.Lure, host.Protocol)
                                + MiningCalculator.GetEnchantmentLevel(item, Enchantments.LuckOfTheSea, host.Protocol)) * 100_000L
                             + (AgentAutoItems.Durability(item)?.Left ?? 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = (slot, item);
                }
            }

            return best;
        }

        private static bool AnyRod(IReadOnlyDictionary<int, Item> items)
        {
            foreach ((int slot, Item item) in items)
            {
                if (AgentAutoItems.IsStorage(slot) && item.Type == ItemType.FishingRod)
                    return true;
            }

            return false;
        }

        /// <summary>The hotbar's slots, then the main inventory's (window 0).</summary>
        internal static IEnumerable<int> HotbarThenMain()
        {
            for (int slot = 36; slot <= 44; slot++)
                yield return slot;
            for (int slot = 9; slot <= 35; slot++)
                yield return slot;
        }

        private bool Moved(Location now) =>
            Math.Sqrt((now.X - anchor.X) * (now.X - anchor.X) + (now.Z - anchor.Z) * (now.Z - anchor.Z)) > MovedAcross
            || Math.Abs(now.Y - anchor.Y) > MovedUpDown;

        /// <summary>The signed difference between two yaws, the short way (-180 to 180).</summary>
        internal static double Delta(double yaw, double target)
        {
            double delta = (target - yaw) % 360;
            if (delta > 180)
                delta -= 360;
            else if (delta < -180)
                delta += 360;
            return delta;
        }

        private void ResetCycle(bool keepBobber = false)
        {
            phase = Phase.Ready;
            if (!keepBobber)
            {
                bobber = null;
                reelOnTick = false;
            }

            inWater = false;
            aimAgain = false;
            hooked = false;
            nextAt = tick + random.Next(RecastMinTicks, RecastMaxTicks + 1);
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
            AutoState now = new(state, reason, state == "on" ? doing : null, HeldRod(), null);
            if (now == said)
                return;
            said = now;
            saidToolTick = tick;
            emitState(now);
        }

        /// <summary>The rod's wear alone is said at most every 10 s; any other change at once.</summary>
        private void ReportTool()
        {
            if (said is null)
                return;
            AutoTool? rod = HeldRod();
            if (rod == said.Tool)
                return;
            bool onlyWear = rod is not null && said.Tool is not null && rod.Item == said.Tool.Item && rod.Max == said.Tool.Max;
            if (onlyWear && tick - saidToolTick < ToolReportTicks)
                return;
            Say();
        }

        private AutoTool? HeldRod()
        {
            if (host.Items is not { } items || !items.TryGetValue(36 + host.HeldSlot, out Item? held)
                                           || held.Type != ItemType.FishingRod)
                return null;
            return AgentAutoItems.Tool(held);
        }
    }
}
