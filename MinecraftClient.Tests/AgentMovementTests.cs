using MinecraftClient.ChatBots;
using MinecraftClient.Mapping;
using static MinecraftClient.ChatBots.MovePlanner;

namespace MinecraftClient.Tests;

/// <summary>
/// The movement controller of the agent bridge's version 3 against a flat test world (no server): steps relative to
/// the facing, walks, following, its time and distance limits, who it may follow, and the path search.
/// </summary>
public sealed class AgentMovementTests
{
    private const int Floor = 64;

    /// <summary>A flat floor at y 64 from -60 to 60, with walls the test puts up; a clock and the events said.</summary>
    private sealed class World : IMoveHost
    {
        public readonly HashSet<(int X, int Z)> Walls = new();
        public readonly Dictionary<string, Location> Players = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<MoveState> Events = new();
        public readonly List<IReadOnlyList<Location>> Walks = new();
        public readonly List<float> Looks = new();
        public readonly List<bool> Sneaks = new();
        public int Halts;
        public int Jumps;
        public long Now;

        public bool CanMove { get; set; } = true;
        public bool PositionKnown { get; set; } = true;
        public Location Position { get; set; } = new(0.5, Floor, 0.5);
        public float Yaw { get; set; } = 180; // north
        public bool Walking { get; set; }
        public string SelfName => "AfkBot_1";

        public void Walk(IReadOnlyList<Location> waypoints)
        {
            Walks.Add(waypoints);
            Walking = true;
        }

        public void Halt()
        {
            Halts++;
            Walking = false;
        }

        public void Look(float yaw) => Looks.Add(yaw);
        public void Jump() => Jumps++;
        public void Sneak(bool on) => Sneaks.Add(on);

        public bool TryFindPlayer(string name, out Location location) => Players.TryGetValue(name, out location);

        private bool Open(int x, int z) => Math.Abs(x) <= 60 && Math.Abs(z) <= 60 && !Walls.Contains((x, z));

        public IEnumerable<Cell> Neighbors(Cell cell)
        {
            foreach ((int dx, int dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                if (cell.Y == Floor && Open(cell.X + dx, cell.Z + dz))
                    yield return new Cell(cell.X + dx, Floor, cell.Z + dz);
            }
        }

        public bool Stands(Cell cell) => cell.Y == Floor && Open(cell.X, cell.Z);

        public AgentMovement Controller(string follow = "Kaan_07,AfkBot_2") =>
            new(this, Events.Add, () => Now, () => 1_000_000 + Now, work => Task.FromResult(work()), follow);

        public Cell LastWalkEnd => Cell.Of(Walks[^1][^1]);
    }

    private static AgentOperation Op(string op, string? dir = null, int n = 0, int x = 0, int y = Floor, int z = 0,
        string? name = null, bool on = false, string id = "m") =>
        new() { Arrival = 1, Op = op, Id = id, Dir = dir, Count = n, X = x, Y = y, Z = z, Name = name, On = on };

    [Fact]
    public void StepsGoWhereTheButtonsPointAndTurnBackAfter()
    {
        World world = new();
        AgentMovement movement = world.Controller();

        Assert.Null(movement.Start(Op("step", "forward", 1)));
        Assert.Equal(new Cell(0, Floor, -1), world.LastWalkEnd); // facing north: forward is -Z
        Assert.Equal("step", world.Events[^1].Doing);
        Assert.Equal("north", movement.Facing);

        world.Yaw = 90; // walking turned it; the buttons keep pointing as before
        Assert.Equal("north", movement.Facing);
        world.Walking = false;
        movement.Tick();
        Assert.Equal(("idle", "arrived"), (world.Events[^1].Doing, world.Events[^1].Ended));
        Assert.Equal([180f], world.Looks);
    }

    [Fact]
    public void TheSameDirectionAgainExtendsTheWalkAndOthersStayRelativeToTheFacing()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        Assert.Null(movement.Start(Op("step", "forward", 1)));
        world.Yaw = 180 - 90; // the walk turned it west-ish; still north for the buttons
        Assert.Null(movement.Start(Op("step", "forward", 1)));
        Assert.Equal(new Cell(0, Floor, -2), world.LastWalkEnd);
        Assert.Single(world.Events); // still the same steps

        Assert.Null(movement.Start(Op("step", "left", 2)));
        Assert.Equal(new Cell(-2, Floor, 0), world.LastWalkEnd); // left of north is west, from where it stands
        Assert.Null(movement.Start(Op("step", "forward", 8)));
        Assert.Null(movement.Start(Op("step", "forward", 8)));
        Assert.Equal(new Cell(0, Floor, -8), world.LastWalkEnd); // never more than 8 ahead
    }

    [Fact]
    public void AWallBlocksTheStepAndAWallFurtherOnCutsItShort()
    {
        World world = new();
        world.Walls.Add((0, -1));
        AgentMovement movement = world.Controller();
        Assert.Equal("blocked", movement.Start(Op("step", "forward", 1)));
        Assert.Empty(world.Walks);

        world.Walls.Clear();
        world.Walls.Add((0, -3));
        Assert.Null(movement.Start(Op("step", "forward", 5)));
        Assert.Equal(new Cell(0, Floor, -2), world.LastWalkEnd);
        world.Walking = false;
        movement.Tick();
        Assert.Equal("blocked", world.Events[^1].Ended);
    }

    [Fact]
    public void AWalkGoesAtMost256BlocksFindsItsWayAndArrives()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        Assert.Equal("too_far", movement.Start(Op("goto", x: 300, z: 0)));
        Assert.Empty(world.Events);

        for (int z = -5; z <= 5; z++)
            world.Walls.Add((5, z));
        Assert.Null(movement.Start(Op("goto", x: 10, z: 0, id: "g1")));
        MoveState started = world.Events[^1];
        Assert.Equal(("goto", "g1", new Cell(10, Floor, 0), 1_120_000L), (started.Doing, started.Id, started.To,
            started.UntilMs));

        movement.Tick(); // the search finished: it walks around the wall
        Cell end = world.LastWalkEnd;
        Assert.True(Math.Abs(end.X - 10) <= 1 && Math.Abs(end.Z) <= 1, $"{end}");
        Assert.DoesNotContain(world.Walks[^1], waypoint => Cell.Of(waypoint) is { X: 5 } c && Math.Abs(c.Z) <= 5);

        // It walks there (a jump of more than 8 blocks in one tick would be a teleport).
        foreach (Location step in new Location[] { new(4.5, Floor, 6.5), new(8.5, Floor, 6.5), new(10.5, Floor, 2.5) })
        {
            world.Position = step;
            movement.Tick();
            Assert.Equal("goto", movement.Doing);
        }

        world.Position = new Location(10.5, Floor, 0.5);
        world.Walking = false;
        movement.Tick();
        Assert.Equal(("idle", "arrived"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void AWalkThatTakesTwoMinutesStops()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        Assert.Null(movement.Start(Op("goto", x: 40, z: 0)));
        movement.Tick();
        world.Now += 119_999;
        movement.Tick();
        Assert.Equal("goto", movement.Doing);
        world.Now += 1;
        movement.Tick();
        Assert.Equal(("idle", "timeout"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void AWalkWithNoWayThereSaysSo()
    {
        World world = new();
        foreach ((int x, int z) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            world.Walls.Add((x, z));
        AgentMovement movement = world.Controller();
        Assert.Null(movement.Start(Op("goto", x: 20, z: 0)));
        movement.Tick();
        Assert.Equal(("idle", "no_path"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void OnlyTheNamesItWasGivenAndOnlyInSight()
    {
        World world = new();
        AgentMovement movement = world.Controller("Kaan_07,AfkBot_1");
        world.Players["Stranger_9"] = new Location(3.5, Floor, 0.5);
        Assert.Equal("not_allowed", movement.Start(Op("follow", name: "Stranger_9")));
        Assert.Equal("not_allowed", movement.Start(Op("follow", name: "afkbot_1"))); // itself, though listed
        Assert.Equal("not_visible", movement.Start(Op("follow", name: "Kaan_07")));
        world.Players["Kaan_07"] = new Location(80.5, Floor, 0.5);
        Assert.Equal("not_visible", movement.Start(Op("approach", name: "kaan_07")));
        world.Players["Kaan_07"] = new Location(10.5, Floor, 0.5);
        Assert.Null(movement.Start(Op("follow", name: "kaan_07")));
        Assert.Equal(("follow", "kaan_07", 1_000_000L + 1_800_000), (world.Events[^1].Doing, world.Events[^1].Name,
            world.Events[^1].UntilMs));
    }

    [Fact]
    public void FollowingKeepsCloseStopsOutOfSightAndAfterThirtyMinutes()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        world.Players["Kaan_07"] = new Location(10.5, Floor, 0.5);
        Assert.Null(movement.Start(Op("follow", name: "Kaan_07")));
        movement.Tick();
        Cell end = world.LastWalkEnd;
        Assert.True(Math.Abs(end.X + 0.5 - 10.5) <= 2 && end != new Cell(10, Floor, 0), $"{end}");

        world.Position = new Location(8.5, Floor, 0.5);
        world.Walking = false;
        movement.Tick();
        Assert.Equal("follow", movement.Doing); // close: it stands
        int walks = world.Walks.Count;
        movement.Tick();
        Assert.Equal(walks, world.Walks.Count);

        world.Now += 1_800_000;
        movement.Tick();
        Assert.Equal(("idle", "limit"), (world.Events[^1].Doing, world.Events[^1].Ended));

        Assert.Null(movement.Start(Op("follow", name: "Kaan_07")));
        world.Players.Clear();
        movement.Tick();
        Assert.Equal(("idle", "out_of_sight"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void ApproachingEndsNextToThePlayer()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        world.Players["AfkBot_2"] = new Location(12.5, Floor, 0.5);
        Assert.Null(movement.Start(Op("approach", name: "AfkBot_2")));
        movement.Tick();
        Assert.NotEmpty(world.Walks);
        world.Position = new Location(5.5, Floor, 0.5);
        movement.Tick();
        Assert.Equal("approach", movement.Doing);
        world.Position = new Location(10.5, Floor, 0.5);
        movement.Tick();
        Assert.Equal(("idle", "arrived"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void StopStopsWithoutAReasonAndAlwaysWorks()
    {
        World world = new() { CanMove = false };
        AgentMovement movement = world.Controller();
        movement.Stop(); // nothing runs, no movement: no error, nothing said
        Assert.Empty(world.Events);

        world.CanMove = true;
        world.Players["Kaan_07"] = new Location(10.5, Floor, 0.5);
        Assert.Null(movement.Start(Op("follow", name: "Kaan_07")));
        movement.Stop();
        Assert.Equal(("idle", (string?)null), (world.Events[^1].Doing, world.Events[^1].Ended));
        Assert.False(world.Walking);
    }

    [Fact]
    public void SneakingIsSaidJumpsArePressedAndFourASecondAfterEight()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        Assert.Null(movement.Start(Op("sneak", on: true)));
        Assert.Equal([true], world.Sneaks);
        Assert.True(world.Events[^1].Sneak);
        Assert.Null(movement.Start(Op("sneak", on: true))); // already: nothing new said
        Assert.Single(world.Events);
        for (int i = 0; i < 6; i++)
            Assert.Null(movement.Start(Op("jump")));
        Assert.Equal(6, world.Jumps);
        Assert.Equal("rate_limited", movement.Start(Op("jump")));
        world.Now += 250;
        Assert.Null(movement.Start(Op("jump")));
    }

    [Fact]
    public void ARespawnOrATeleportStopsWhatRuns()
    {
        World world = new();
        AgentMovement movement = world.Controller();
        Assert.Null(movement.Start(Op("sneak", on: true)));
        Assert.Null(movement.Start(Op("goto", x: 30, z: 0)));
        movement.Moved();
        Assert.Contains(world.Events, state => state.Ended == "reset");
        Assert.False(world.Events[^1].Sneak);
        Assert.Equal([true, false], world.Sneaks);

        Assert.Null(movement.Start(Op("goto", x: 30, z: 0)));
        movement.Tick();
        world.Position = new Location(40.5, Floor, 30.5);
        movement.Tick();
        Assert.Equal(("idle", "reset"), (world.Events[^1].Doing, world.Events[^1].Ended));
    }

    [Fact]
    public void WithoutMovementOrAPositionNothingStarts()
    {
        World world = new() { CanMove = false };
        AgentMovement movement = world.Controller();
        movement.Joined();
        Assert.Equal("off", world.Events[^1].Doing);
        Assert.Equal("movement_disabled", movement.Start(Op("jump")));

        world.CanMove = true;
        world.PositionKnown = false;
        Assert.Equal("not_ready", movement.Start(Op("step", "forward", 1)));
        world.PositionKnown = true;
        movement.Joined();
        Assert.Equal(("idle", false), (world.Events[^1].Doing, world.Events[^1].Sneak));
    }

    private static IEnumerable<Cell> Grid(Cell cell, HashSet<(int, int)> walls, int size = 60)
    {
        foreach ((int dx, int dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            (int x, int z) = (cell.X + dx, cell.Z + dz);
            if (Math.Abs(x) <= size && Math.Abs(z) <= size && !walls.Contains((x, z)))
                yield return new Cell(x, 0, z);
        }
    }

    [Fact]
    public void TheSearchGoesAroundAWall()
    {
        HashSet<(int, int)> walls = new();
        for (int z = -5; z <= 5; z++)
            walls.Add((3, z));
        Cell goal = new(6, 0, 0);
        Plan? plan = Find(new Cell(0, 0, 0), cell => cell == goal, cell => cell.DistanceTo(goal),
            cell => Grid(cell, walls), _ => true, 4000, 1, CancellationToken.None);
        Assert.NotNull(plan);
        Assert.True(plan.Reached);
        Assert.Equal(goal, plan.Path[^1]);
        Assert.DoesNotContain(plan.Path, cell => walls.Contains((cell.X, cell.Z)));
        Assert.Equal(new Cell(0, 0, 0), plan.Path[0]);
    }

    [Fact]
    public void TheSearchStopsAtItsLimitWithTheNearestPart()
    {
        Cell goal = new(55, 0, 0);
        Plan? plan = Find(new Cell(0, 0, 0), cell => cell == goal, cell => cell.DistanceTo(goal),
            cell => Grid(cell, []), _ => true, 20, 1, CancellationToken.None);
        Assert.NotNull(plan);
        Assert.False(plan.Reached);
        Assert.True(plan.Path[^1].DistanceTo(goal) <= goal.X - 1);
        Assert.True(plan.Expanded <= 21);
    }

    [Fact]
    public void NoWayOutOrACancelledSearchGivesNothing()
    {
        HashSet<(int, int)> box = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        Cell goal = new(10, 0, 0);
        Assert.Null(Find(new Cell(0, 0, 0), cell => cell == goal, cell => cell.DistanceTo(goal),
            cell => Grid(cell, box), _ => true, 4000, 1, CancellationToken.None));
        Assert.Null(Find(new Cell(0, 0, 0), cell => cell == goal, cell => cell.DistanceTo(goal),
            cell => Grid(cell, []), _ => true, 4000, 1, new CancellationToken(canceled: true)));

        Plan? there = Find(goal, cell => cell == goal, cell => cell.DistanceTo(goal), cell => Grid(cell, []),
            _ => true, 4000, 1, CancellationToken.None);
        Assert.Equal([goal], there!.Path);
    }
}
