using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Mapping;
using static MinecraftClient.ChatBots.MovePlanner;

namespace MinecraftClient.ChatBots
{
    /// <summary>What the movement controller needs from the client: <see cref="McClient"/> through the bridge, a fake in tests.</summary>
    internal interface IMoveHost
    {
        /// <summary>The client runs with terrain and entity handling (the host started it with movement).</summary>
        bool CanMove { get; }

        /// <summary>The server told the client where it stands.</summary>
        bool PositionKnown { get; }

        Location Position { get; }
        float Yaw { get; }

        /// <summary>A path is being walked (waypoints left, or one still being walked to).</summary>
        bool Walking { get; }

        string SelfName { get; }

        void Walk(IReadOnlyList<Location> waypoints);
        void Halt();
        void Look(float yaw);
        void Jump();
        void Sneak(bool on);

        /// <summary>A player of that name the client tracks (in sight), and where it stands.</summary>
        bool TryFindPlayer(string name, out Location location);

        /// <summary>The cells one move away a player can move into (the world's loaded terrain). Called off the game thread.</summary>
        IEnumerable<Cell> Neighbors(Cell cell);

        /// <summary>A player stands in that cell (on the ground, or in water). Called off the game thread.</summary>
        bool Stands(Cell cell);
    }

    /// <summary>What the bridge's <c>move</c> event says (docs/agent-bridge.md, version 3).</summary>
    internal sealed record MoveState(string Doing, string? Id, Cell? To, string? Name, long? UntilMs, string? Ended,
        bool Sneak);

    /// <summary>
    /// Movement by the host's operations (docs/agent-bridge.md, version 3): steps relative to where the account looks,
    /// a jump, sneaking, a walk to coordinates, a walk next to a player, following a player. One activity at a time;
    /// a new one replaces what runs; <c>stop</c> always stops it. Its limits are the second line behind the host's:
    /// 256 blocks and 2 minutes for a walk, 30 minutes for following, only the names the host gave at the start
    /// (<see cref="FollowEnvVar"/>) and only players in sight, 4 operations a second in bursts of 8, bounded searches.
    /// </summary>
    internal sealed class AgentMovement
    {
        /// <summary><c>1</c>: the host speaks version 3's movement (operations, <c>move</c> events, <c>pos.facing</c>).</summary>
        public const string MoveEnvVar = "MCC_AGENT_MOVE";

        /// <summary>The names <c>approach</c> and <c>follow</c> may name, comma-separated (the host's list at start).</summary>
        public const string FollowEnvVar = "MCC_AGENT_FOLLOW";

        public const int MaxSteps = 8;
        public const double MaxWalkBlocks = 256;
        public const long WalkMs = 120_000;
        public const long FollowMs = 1_800_000;
        public const double SightBlocks = 64;
        public const int PathNodes = 4000;
        public const int FollowPathNodes = 1500;
        public const int StepPathNodes = 128;
        public const int PlanMs = 1000;
        public const long ReplanMs = 1000;
        public const double ApproachStop = 2.5;
        public const double FollowKeep = 3.0;
        public const int BucketSize = 8;
        public const double PerSecond = 4;

        /// <summary>A walk's goal: within 1 block across and 2 up or down of the target block.</summary>
        private const int GoalAcross = 1;
        private const int GoalUpDown = 2;

        /// <summary>A walk to a player ends on a cell whose centre is this close to the player, across.</summary>
        private const double PlayerReach = 2.0;

        private const int StuckTicks = 40;
        private const int FruitlessPlans = 3;
        private const double TeleportBlocks = 8;
        private const int CoordinateMax = 30_000_000;

        public static bool Enabled { get; } = Environment.GetEnvironmentVariable(MoveEnvVar) == "1";

        /// <summary>The four directions in Minecraft's order of yaw (0, 90, 180, 270 degrees).</summary>
        internal static readonly string[] Cardinals = ["south", "west", "north", "east"];

        private readonly IMoveHost host;
        private readonly Action<MoveState> emit;
        private readonly Func<long> clock;
        private readonly Func<long> unixMs;
        private readonly Func<Func<Plan?>, Task<Plan?>> runPlan;
        private readonly HashSet<string> followNames;
        private readonly TokenBucket bucket;

        private string doing = "idle";
        private string? activityId;
        private bool sneaking;
        private long deadline;
        private long untilUnixMs;

        // Steps: the yaw and facing the buttons are relative to, the direction walked and the column the steps end on.
        private float homeYaw;
        private string homeFacing = "south";
        private string? stepToward;
        private Cell stepEnd;
        private bool stepShort;

        // Walks and following.
        private Cell goal;
        private string? target;
        private Location planTarget;
        private Task<Plan?>? planning;
        private CancellationTokenSource? planCancel;
        private long lastPlanAt = long.MinValue;
        private double bestDistance = double.MaxValue;
        private int fruitless;

        // Stuck and teleport detection.
        private Location? lastPosition;
        private Location stuckFrom;
        private int stuckTicks;

        public AgentMovement(IMoveHost host, Action<MoveState> emit, Func<long>? clock = null, Func<long>? unixMs = null,
            Func<Func<Plan?>, Task<Plan?>>? runPlan = null, string? followNames = null)
        {
            this.host = host;
            this.emit = emit;
            Stopwatch watch = Stopwatch.StartNew();
            this.clock = clock ?? (() => watch.ElapsedMilliseconds);
            this.unixMs = unixMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            this.runPlan = runPlan ?? (work => Task.Run(work));
            this.followNames = ParseNames(followNames ?? Environment.GetEnvironmentVariable(FollowEnvVar));
            bucket = new TokenBucket(BucketSize, PerSecond, () => this.clock() / 1000.0);
        }

        /// <summary>What the account does now: <c>idle</c>, <c>step</c>, <c>goto</c>, <c>approach</c>, <c>follow</c>.</summary>
        public string Doing => doing;

        public bool Sneaking => sneaking;

        /// <summary>
        /// What the direction buttons are relative to: where the account looks, and while it takes steps where it looked
        /// before them (walking turns it).
        /// </summary>
        public string Facing => doing == "step" ? homeFacing : FacingOf(host.Yaw);

        /// <summary>A new session: nothing runs, no sneaking. Says so, or that this client cannot move.</summary>
        public void Joined()
        {
            Reset();
            emit(new MoveState(host.CanMove ? "idle" : "off", null, null, null, null, null, false));
        }

        /// <summary>The session ended: nothing runs, nothing is said (the host sees the disconnection).</summary>
        public void Left() => Reset();

        /// <summary>The server moved the account (a respawn, another world): what ran stops, sneaking ends.</summary>
        public void Moved()
        {
            if (doing != "idle")
                End("reset");
            if (sneaking)
            {
                sneaking = false;
                host.Sneak(false);
                Say();
            }
        }

        /// <summary>Stop what runs; always works, also before joining and without movement.</summary>
        public void Stop()
        {
            if (doing != "idle")
                End(null);
        }

        /// <summary>Carry out one movement operation (not <c>stop</c>); null when it started, else the reason it did not.</summary>
        public string? Start(AgentOperation operation)
        {
            if (!host.CanMove)
                return "movement_disabled";
            if (!host.PositionKnown)
                return "not_ready";

            switch (operation.Op)
            {
                case AgentOperation.Jump:
                    if (!bucket.TryTake())
                        return "rate_limited";
                    host.Jump();
                    return null;

                case AgentOperation.Sneak:
                    if (!bucket.TryTake())
                        return "rate_limited";
                    if (sneaking != operation.On)
                    {
                        sneaking = operation.On;
                        host.Sneak(sneaking);
                        Say();
                    }

                    return null;

                case AgentOperation.Step:
                    return StartSteps(operation);

                case AgentOperation.Goto:
                    return StartWalk(operation);

                case AgentOperation.Approach:
                case AgentOperation.Follow:
                    return StartChase(operation);
            }

            return "unknown_op";
        }

        private string? StartSteps(AgentOperation operation)
        {
            // The token first: the line search below is the costly part.
            if (!bucket.TryTake())
                return "rate_limited";

            bool stepping = doing == "step";
            string facing = stepping ? homeFacing : FacingOf(host.Yaw);
            string toward = Resolve(facing, operation.Dir!)!;
            (int dx, int dz) = Delta(toward);
            Cell here = Cell.Of(host.Position);

            // The same direction again extends the walk (at most 8 blocks ahead of the account); anything else starts anew.
            int ahead = operation.Count;
            if (stepping && toward == stepToward)
            {
                int left = (stepEnd.X - here.X) * dx + (stepEnd.Z - here.Z) * dz;
                ahead = Math.Min(MaxSteps, Math.Max(0, left) + operation.Count);
            }

            Plan? plan = StepPlan(here, dx, dz, ahead);
            if (plan is null)
                return "blocked";

            if (!stepping)
            {
                homeYaw = host.Yaw;
                homeFacing = facing;
            }

            Cancel();
            stepToward = toward;
            stepEnd = new Cell(here.X + dx * ahead, plan.Path[^1].Y, here.Z + dz * ahead);
            stepShort = !plan.Reached;
            if (!stepping)
                Begin("step", operation.Id, null, null, null);
            else
                activityId = operation.Id;
            WalkPlan(plan);
            return null;
        }

        /// <summary>The cells along one straight line from <paramref name="here"/>, at most <paramref name="ahead"/> blocks.</summary>
        private Plan? StepPlan(Cell here, int dx, int dz, int ahead)
        {
            int Progress(Cell cell) => (cell.X - here.X) * dx + (cell.Z - here.Z) * dz;

            bool OnLine(Cell cell)
            {
                int lateral = dx != 0 ? cell.Z - here.Z : cell.X - here.X;
                int progress = Progress(cell);
                return lateral == 0 && progress >= 0 && progress <= ahead;
            }

            return Find(here, cell => Progress(cell) == ahead, cell => ahead - Progress(cell),
                cell => host.Neighbors(cell).Where(OnLine), host.Stands, StepPathNodes, 1, CancellationToken.None);
        }

        private string? StartWalk(AgentOperation operation)
        {
            if (Math.Abs(operation.X) > CoordinateMax || Math.Abs(operation.Z) > CoordinateMax)
                return "bad_request";
            Cell to = new(operation.X, operation.Y, operation.Z);
            if (Distance(host.Position, to) > MaxWalkBlocks)
                return "too_far";
            if (!bucket.TryTake())
                return "rate_limited";

            Cancel();
            goal = to;
            target = null;
            Begin("goto", operation.Id, to, null, WalkMs);
            PlanWalk();
            return null;
        }

        private string? StartChase(AgentOperation operation)
        {
            string name = operation.Name!;
            if (string.Equals(name, host.SelfName, StringComparison.OrdinalIgnoreCase) || !followNames.Contains(name))
                return "not_allowed";
            if (!host.TryFindPlayer(name, out Location where) || host.Position.Distance(where) > SightBlocks)
                return "not_visible";
            if (!bucket.TryTake())
                return "rate_limited";

            Cancel();
            target = name;
            bool follow = operation.Op == AgentOperation.Follow;
            Begin(follow ? "follow" : "approach", operation.Id, null, name, follow ? FollowMs : WalkMs);
            if (!Near(host.Position, where, follow))
                PlanTowards(where, follow ? FollowPathNodes : PathNodes);
            return null;
        }

        /// <summary>Called every client tick (50 ms) while in game.</summary>
        public void Tick()
        {
            if (doing == "idle")
                return;

            Location position = host.Position;
            if (lastPosition is { } before && before.Distance(position) > TeleportBlocks)
            {
                End("reset");
                return;
            }

            lastPosition = position;
            long now = clock();
            if (doing != "step" && now >= deadline)
            {
                End(doing == "follow" ? "limit" : "timeout");
                return;
            }

            switch (doing)
            {
                case "step":
                    if (Stuck(position))
                    {
                        stepShort = true;
                        host.Halt();
                    }

                    if (!host.Walking)
                        End(stepShort ? "blocked" : "arrived");
                    break;

                case "goto":
                    WalkTick(position);
                    break;

                default:
                    ChaseTick(position, now);
                    break;
            }
        }

        private void WalkTick(Location position)
        {
            if (TakePlan(out Plan? plan, out bool searching))
            {
                if (plan is null)
                {
                    End(bestDistance == double.MaxValue ? "no_path" : "blocked");
                    return;
                }

                double distance = plan.Path[^1].DistanceTo(goal);
                if (distance < bestDistance - 1 || plan.Reached && distance < bestDistance)
                {
                    bestDistance = distance;
                    fruitless = 0;
                }
                else if (++fruitless >= FruitlessPlans)
                {
                    End("blocked");
                    return;
                }

                WalkPlan(plan);
                return;
            }

            if (searching)
                return;
            if (IsWalkGoal(Cell.Of(position), goal))
            {
                End("arrived");
                return;
            }

            if (Stuck(position))
                host.Halt();
            if (!host.Walking)
                PlanWalk();
        }

        private void ChaseTick(Location position, long now)
        {
            bool follow = doing == "follow";
            if (!host.TryFindPlayer(target!, out Location where) || position.Distance(where) > SightBlocks)
            {
                End("out_of_sight");
                return;
            }

            if (!follow && Near(position, where, false))
            {
                End("arrived");
                return;
            }

            if (TakePlan(out Plan? plan, out bool searching))
            {
                if (plan is not null)
                    WalkPlan(plan);
                else if (!follow)
                {
                    End("no_path");
                    return;
                }
            }

            if (searching)
                return;

            if (follow && Near(position, where, true))
            {
                if (host.Walking)
                    host.Halt();
                return;
            }

            bool moved = planTarget.Distance(where) > 2;
            if ((!host.Walking || moved || Stuck(position)) && now - lastPlanAt >= ReplanMs)
            {
                host.Halt();
                PlanTowards(where, follow ? FollowPathNodes : PathNodes);
            }
        }

        /// <summary>Next to the player: an approach ends there, a follow stands still there.</summary>
        private static bool Near(Location position, Location where, bool follow) =>
            position.Distance(where) <= (follow ? FollowKeep : ApproachStop)
            || IsPlayerGoal(Cell.Of(position), Cell.Of(where), where);

        private static bool IsWalkGoal(Cell cell, Cell to) =>
            Math.Abs(cell.X - to.X) <= GoalAcross && Math.Abs(cell.Z - to.Z) <= GoalAcross
                                                  && Math.Abs(cell.Y - to.Y) <= GoalUpDown;

        private static bool IsPlayerGoal(Cell cell, Cell around, Location where)
        {
            double dx = cell.X + 0.5 - where.X, dz = cell.Z + 0.5 - where.Z;
            return cell != around && dx * dx + dz * dz <= PlayerReach * PlayerReach && Math.Abs(cell.Y - where.Y) <= 2;
        }

        /// <summary>A search for the walk to <see cref="goal"/>: the whole way if it reaches it, else the nearest part.</summary>
        private void PlanWalk()
        {
            Cell start = Cell.Of(host.Position);
            Cell to = goal;
            Search(token => Find(start, cell => IsWalkGoal(cell, to), cell => cell.DistanceTo(to), host.Neighbors,
                host.Stands, PathNodes, 1, token));
        }

        /// <summary>A search for a walk to next to a player.</summary>
        private void PlanTowards(Location where, int nodes)
        {
            Cell start = Cell.Of(host.Position);
            Cell around = Cell.Of(where);
            planTarget = where;
            Search(token => Find(start, cell => IsPlayerGoal(cell, around, where), cell => cell.DistanceTo(around),
                host.Neighbors, host.Stands, nodes, 1, token));
        }

        private void Search(Func<CancellationToken, Plan?> search)
        {
            planCancel?.Cancel();
            CancellationTokenSource cancel = new(PlanMs);
            planCancel = cancel;
            lastPlanAt = clock();
            CancellationToken token = cancel.Token;
            planning = runPlan(() => search(token));
        }

        /// <summary>
        /// A finished search, once: true with its plan (null: nothing found). <paramref name="searching"/>: one still runs.
        /// </summary>
        private bool TakePlan(out Plan? plan, out bool searching)
        {
            plan = null;
            searching = planning is { IsCompleted: false };
            if (planning is null || searching)
                return false;

            plan = planning.Status == TaskStatus.RanToCompletion ? planning.Result : null;
            planning = null;
            return true;
        }

        private void WalkPlan(Plan plan)
        {
            stuckTicks = 0;
            stuckFrom = host.Position;
            List<Location> waypoints = plan.Path.Skip(1).Select(cell => cell.Center).ToList();
            if (waypoints.Count > 0)
                host.Walk(waypoints);
        }

        /// <summary>Walking, but not 0.1 block further in 2 seconds.</summary>
        private bool Stuck(Location position)
        {
            if (!host.Walking)
            {
                stuckTicks = 0;
                return false;
            }

            double dx = position.X - stuckFrom.X, dz = position.Z - stuckFrom.Z;
            if (dx * dx + dz * dz > 0.01)
            {
                stuckFrom = position;
                stuckTicks = 0;
                return false;
            }

            return ++stuckTicks >= StuckTicks;
        }

        private void Begin(string what, string? id, Cell? to, string? name, long? forMs)
        {
            doing = what;
            activityId = id;
            lastPosition = host.Position;
            bestDistance = double.MaxValue;
            fruitless = 0;
            stuckTicks = 0;
            stuckFrom = host.Position;
            deadline = forMs is { } ms ? clock() + ms : long.MaxValue;
            untilUnixMs = forMs is { } span ? unixMs() + span : 0;
            emit(new MoveState(what, id, to, name, forMs is null ? null : untilUnixMs, null, sneaking));
        }

        private void End(string? ended)
        {
            bool steps = doing == "step";
            Cancel();
            host.Halt();
            if (steps && ended != "reset")
                host.Look(homeYaw);
            doing = "idle";
            activityId = null;
            target = null;
            stepToward = null;
            lastPosition = null;
            emit(new MoveState("idle", null, null, null, null, ended, sneaking));
        }

        /// <summary>The state again (sneaking changed).</summary>
        private void Say()
        {
            bool walk = doing == "goto", chase = doing is "approach" or "follow";
            emit(new MoveState(doing, activityId, walk ? goal : null, chase ? target : null,
                walk || chase ? untilUnixMs : null, null, sneaking));
        }

        private void Reset()
        {
            Cancel();
            if (sneaking)
                host.Sneak(false);
            doing = "idle";
            activityId = null;
            target = null;
            stepToward = null;
            sneaking = false;
            lastPosition = null;
        }

        private void Cancel()
        {
            planCancel?.Cancel();
            planCancel = null;
            planning = null;
        }

        /// <summary>Where an account looks, from its yaw in degrees (the game's rounding: F3's "Facing").</summary>
        internal static string FacingOf(double yaw) => Cardinals[(int)Math.Floor(yaw % 360.0 / 90.0 + 0.5) & 3];

        /// <summary>
        /// The direction of a step: <c>forward</c>, <c>back</c>, <c>left</c> or <c>right</c> of <paramref name="facing"/>,
        /// or a compass direction (<c>north</c>, ...) as it is; null for anything else.
        /// </summary>
        internal static string? Resolve(string facing, string dir)
        {
            if (Array.IndexOf(Cardinals, dir) >= 0)
                return dir;
            int turn = dir switch
            {
                "forward" => 0,
                "right" => 1,
                "back" => 2,
                "left" => 3,
                _ => -1,
            };
            int from = Array.IndexOf(Cardinals, facing);
            return turn < 0 || from < 0 ? null : Cardinals[(from + turn) % 4];
        }

        /// <summary>The block offset of one step toward a compass direction.</summary>
        internal static (int Dx, int Dz) Delta(string toward) => toward switch
        {
            "south" => (0, 1),
            "west" => (-1, 0),
            "north" => (0, -1),
            "east" => (1, 0),
            _ => (0, 0),
        };

        /// <summary>From where the account stands (its feet) to the target block's centre at its height, in blocks.</summary>
        internal static double Distance(Location position, Cell target)
        {
            double dx = target.X + 0.5 - position.X, dy = target.Y - position.Y, dz = target.Z + 0.5 - position.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>The valid player names of a comma-separated list, matched without regard to case.</summary>
        internal static HashSet<string> ParseNames(string? value)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            if (value is null)
                return names;
            foreach (string name in value.Split(',',
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (AgentOperation.IsValidPlayerName(name))
                    names.Add(name);
            }

            return names;
        }

        /// <summary>A token bucket: <c>size</c> tokens, <c>perSecond</c> back each second, counted continuously.</summary>
        internal sealed class TokenBucket(int size, double perSecond, Func<double> seconds)
        {
            private double tokens = size;
            private double? last;

            public bool TryTake()
            {
                double now = seconds();
                if (last is { } before)
                    tokens = Math.Min(size, tokens + (now - before) * perSecond);
                last = now;
                if (tokens < 1)
                    return false;
                tokens -= 1;
                return true;
            }
        }
    }
}
