using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot moves (and optionally sends a command) every X seconds in order to stay non-afk.
    /// </summary>
    public class AntiAFK : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized] private const string BotName = "AntiAFK";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Delay$")]
            public Range Delay = new(60);

            [TomlInlineComment("$ChatBot.AntiAfk.Command$")]
            // Empty by default: a command the server does not know ("/ping" is not universal)
            // produces an error reply on every round. Movement alone is enough to stay non-AFK.
            public string Command = "";

            [TomlInlineComment("$ChatBot.AntiAfk.Use_Sneak$")]
            public bool Use_Sneak = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Use_Terrain_Handling$")]
            public bool Use_Terrain_Handling = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Walk_Range$")]
            public int Walk_Range = 5;

            [TomlInlineComment("$ChatBot.AntiAfk.Walk_Retries$")]
            public int Walk_Retries = 20;

            public void OnSettingUpdate()
            {
                if (Walk_Range <= 0)
                {
                    Walk_Range = 5;
                    LogToConsole(BotName, Translations.bot_antiafk_invalid_walk_range);
                }

                Delay.min = Math.Max(1.0, Delay.min);
                Delay.max = Math.Max(1.0, Delay.max);

                double maxDelaySeconds = int.MaxValue / (double)Settings.ClientTicksPerSecond;
                Delay.min = Math.Min(maxDelaySeconds, Delay.min);
                Delay.max = Math.Min(maxDelaySeconds, Delay.max);

                if (Delay.min > Delay.max)
                {
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);
                    LogToConsole(BotName, Translations.bot_antiafk_swapping);
                }

                Command ??= string.Empty;
            }

            public struct Range
            {
                public double min, max;

                public Range()
                {
                    min = 0;
                    max = 0;
                }

                public Range(int value)
                {
                    min = max = value;
                }

                public Range(int min, int max)
                {
                    this.min = min;
                    this.max = max;
                }
            }
        }

        // The path search runs off the client tick (client 4.2.11): the tick goes on while it searches and the walk starts
        // on the tick after it ends, as the game's ticks never wait for a search. Before, it ran on the tick and stopped
        // the client for up to 2 s (Walk_Retries tries of up to 0.5 s each), then ten ticks went at once. Each try stays
        // short and the whole search is capped, so an unreachable spot (right after a teleport or respawn) ends it soon.
        private static readonly TimeSpan PathfindingAttemptTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan PathfindingBudget = TimeSpan.FromSeconds(2);

        private int count, nextrun = Settings.DoubleToTick(5.0);
        private bool previousSneakState = false;
        private readonly Random random = new();

        private readonly Func<Func<Queue<Location>?>, Task<Queue<Location>?>> runSearch;
        private Task<Queue<Location>?>? walkSearch;
        private CancellationTokenSource? walkCancel;
        private Location walkStart;

        /// <summary>
        /// This bot moves (and optionally sends a command) every X seconds in order to stay non-afk.
        /// </summary>
        public AntiAFK() : this(null)
        {
        }

        /// <summary>The bot, with the way its path search runs (tests: on the caller's thread).</summary>
        internal AntiAFK(Func<Func<Queue<Location>?>, Task<Queue<Location>?>>? runSearch)
        {
            count = 0;
            this.runSearch = runSearch ?? (search => Task.Run(search));
        }

        public override void Initialize()
        {
            if (Config.Use_Terrain_Handling)
            {
                if (!GetTerrainEnabled())
                {
                    LogToConsole(Translations.bot_antiafk_not_using_terrain_handling);
                }
                else
                {
                    var movementLock = BotMovementLock.Instance;
                    if (movementLock is { IsLocked: true })
                        LogToConsole(
                            $"§§6§1§0{string.Format(Translations.bot_antiafk_may_not_move, movementLock.LockedBy)}");
                }
            }
        }

        public override void Update()
        {
            // A search started on an earlier turn: the walk (or the turn's other moves) once it ends
            if (walkSearch is not null)
            {
                if (!walkSearch.IsCompleted)
                    return;
                Queue<Location>? path = walkSearch.Status == TaskStatus.RanToCompletion ? walkSearch.Result : null;
                walkSearch = null;
                walkCancel?.Dispose();
                walkCancel = null;
                FinishTurn(StartWalk(path));
                return;
            }

            if (!Due(IsScreenOpen()))
                return;
            DoAntiAfkStuff();
            count = 0;
            nextrun = random.Next(Settings.DoubleToTick(Config.Delay.min), Settings.DoubleToTick(Config.Delay.max));
        }

        public override void OnUnload()
        {
            walkCancel?.Cancel();
        }

        /// <summary>
        /// One client tick of the bot's clock: true when its turn has come. While a screen is open (a window, the player's
        /// own inventory) the clock stops and the bot neither sneaks nor walks: the game's player stands with every key up
        /// then, and an anticheat flags a window click made while sneaking or walking (client 4.2.9: Grim MultiActionsC
        /// cancelled an AFK account's trade clicks while AntiAFK held its sneak key). It goes on when the screen closes.
        /// </summary>
        internal bool Due(bool screenOpen)
        {
            if (screenOpen)
                return false;
            return ++count >= nextrun;
        }

        private void DoAntiAfkStuff()
        {
            if (Config.Use_Terrain_Handling && GetTerrainEnabled() && BotMovementLock.Instance is { IsLocked: false })
            {
                // The goals are drawn here, the paths searched off the tick; the turn ends when the search does
                Location start = GetCurrentLocation();
                List<Location> goals = new(Math.Max(0, Config.Walk_Retries));
                for (int i = 0; i < Config.Walk_Retries; i++)
                    goals.Add(RandomGoal(start));

                World world = GetWorld();
                walkStart = start;
                walkCancel = new CancellationTokenSource(PathfindingBudget);
                CancellationToken token = walkCancel.Token;
                walkSearch = runSearch(() => FindWalk(world, start, goals, PathfindingAttemptTimeout, token));
                return;
            }

            FinishTurn(walked: false);
        }

        /// <summary>
        /// The search, off the tick: the path to the first goal one can be found to, trying them in turn with a short time
        /// for each; none once the time is up, or when the next goal's chunks are not loaded.
        /// </summary>
        internal static Queue<Location>? FindWalk(World world, Location start, IReadOnlyList<Location> goals,
            TimeSpan attemptTimeout, CancellationToken token)
        {
            foreach (Location goal in goals)
            {
                if (token.IsCancellationRequested || !Movement.CheckChunkLoading(world, start, goal))
                    return null;

                using CancellationTokenSource attempt = CancellationTokenSource.CreateLinkedTokenSource(token);
                attempt.CancelAfter(attemptTimeout);
                Queue<Location>? path = Movement.CalculatePath(world, start, goal, allowUnsafe: false, maxOffset: 0,
                    minOffset: 0, attempt.Token);
                if (path is not null)
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Walk the path found, unless the player was moved meanwhile (a teleport, a respawn: the path starts elsewhere)
        /// or another bot took the movement.
        /// </summary>
        private bool StartWalk(Queue<Location>? path)
        {
            if (path is null || !GetTerrainEnabled() || BotMovementLock.Instance is { IsLocked: true })
                return false;

            Location now = GetCurrentLocation();
            double dx = now.X - walkStart.X, dz = now.Z - walkStart.Z;
            if (dx * dx + dz * dz > 1.0 || Math.Abs(now.Y - walkStart.Y) > 1.0)
                return false;

            Handler.WalkWaypoints(path);
            return true;
        }

        /// <summary>The rest of a turn: after a walk only the sneak goes up; otherwise the command and the sneak toggle.</summary>
        private void FinishTurn(bool walked)
        {
            if (walked && Config.Use_Sneak)
            {
                // Solve the case when the bot was closed in 1x2, was sneaking, but then he was freed, this will make him not sneak anymore
                previousSneakState = false;
                Sneak(false);
                return;
            }

            // An empty command means "move only": sending it would either be dropped or answered with an
            // "unknown command" line by the server on every single run.
            if (!string.IsNullOrWhiteSpace(Config.Command))
                SendText(Config.Command);

            if (Config.Use_Sneak)
            {
                Sneak(previousSneakState);
                previousSneakState = !previousSneakState;
            }

            count = 0;
        }

        /// <summary>A random goal in range, never the spot the player stands on.</summary>
        private Location RandomGoal(Location start)
        {
            Location goal = GetRandomLocationWithinRangeXZ(start, Config.Walk_Range);
            while (start.X == goal.X && start.Y == goal.Y && start.Z == goal.Z)
            {
                LogToConsole("Same location!, generating new one");
                goal = GetRandomLocationWithinRangeXZ(start, Config.Walk_Range);
            }
            return goal;
        }

        private Location GetRandomLocationWithinRangeXZ(Location currentLocation, int range)
        {
            return new Location(currentLocation.X + random.Next(range * -1, range), currentLocation.Y,
                currentLocation.Z + random.Next(range * -1, range));
        }
    }
}
