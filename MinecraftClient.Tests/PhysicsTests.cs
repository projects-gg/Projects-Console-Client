using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.11: the walk moves as the game's own does, so an anticheat that replays it (Grim) predicts the very same
/// move. Before, a walk stuck at a wall or edge drew clustered Simulation flags from small differences.
/// </summary>
public sealed class PhysicsTests
{
    [Fact]
    public void AWalkTurnsByTheGamesSineTableAndTheYawItSends()
    {
        // MCC-TP e2e, Grim debug: a step from rest, forward, looking at 158.05678° sent as -201.94322 (the short way from
        // the server's yaw). Grim predicted exactly this; the client, with the exact sine and its 0..360 look, moved
        // (-0.03662138558, 0, -0.09090035465), 6E-6 off.
        Vec3d move = PlayerPhysics.GetInputVector(new Vec3d(0, 0, 0.98f), 0.1f, -201.94322204589844f);

        Assert.Equal(-0.03661579549968996, move.X);
        Assert.Equal(0.0, move.Y);
        Assert.Equal(-0.0909026093726239, move.Z);
    }

    [Fact]
    public void TheSineTableIsTheGames()
    {
        const double sinScale = 10430.378350470453; // Mth: 65536 steps a turn
        Assert.Equal(0f, VanillaMath.Sin(0.0));
        Assert.Equal(1f, VanillaMath.Cos(0.0));

        // An angle takes the step below it; the same angle a turn lower is negative and truncates toward zero (Java's
        // (long) cast) before it wraps, so it takes the step above: the game's own table, quirks and all.
        double angle = 28773.4 / sinScale;
        Assert.Equal((float)Math.Sin(28773 / sinScale), VanillaMath.Sin(angle));
        Assert.Equal((float)Math.Sin(28774 / sinScale), VanillaMath.Sin(angle - 2 * Math.PI));
        Assert.Equal((float)Math.Sin((28773 + 16384) / sinScale), VanillaMath.Cos(angle));
    }

    [Theory]
    // movement, the axes in the order they resolve (0 = X, 1 = Y, 2 = Z)
    [InlineData(0.02, -0.0784, 0.05, new[] { 1, 2, 0 })] // a slow step: still Y first (falls only after it moved)
    [InlineData(0.05, -0.0784, 0.02, new[] { 1, 0, 2 })]
    [InlineData(0.2, 0.5, 0.1, new[] { 1, 0, 2 })] // a step-up try: up first, then sideways
    [InlineData(0.1, 0.0, -0.1, new[] { 1, 0, 2 })] // as far on X as on Z: X first
    [InlineData(0.15, -0.0784, 0.16, new[] { 1, 2, 0 })]
    public void TheAxesResolveYFirstThenTheLongerWay(double x, double y, double z, int[] expected)
    {
        Assert.Equal(expected, CollisionDetector.GetAxisStepOrder(new Vec3d(x, y, z)));
    }

    [Fact]
    public void AWalkStepsUpOntoAHalfBlock()
    {
        var floor = new Aabb(-5, -1, -5, 5, 0, 5);
        var halfBlock = new Aabb(1, 0, -5, 2, 0.5, 5);
        Aabb player = Aabb.OfSize(0.6, 0, 0, PhysicsConsts.PlayerWidth, PhysicsConsts.PlayerHeight);

        Vec3d move = CollisionDetector.Collide(Colliders(floor, halfBlock), player, new Vec3d(0.2, -0.0784, 0), true,
            PhysicsConsts.StepHeight);

        // Before, the step try went sideways first, hit the half block's side and stayed down at 0.1.
        Assert.Equal(0.2, move.X);
        Assert.Equal(0.5, move.Y);
        Assert.Equal(0.0, move.Z);
    }

    [Fact]
    public void AWalkDoesNotStepUpAFullBlock()
    {
        var floor = new Aabb(-5, -1, -5, 5, 0, 5);
        var wall = new Aabb(1, 0, -5, 2, 1, 5);
        Aabb player = Aabb.OfSize(0.6, 0, 0, PhysicsConsts.PlayerWidth, PhysicsConsts.PlayerHeight);

        Vec3d move = CollisionDetector.Collide(Colliders(floor, wall), player, new Vec3d(0.2, -0.0784, 0), true,
            PhysicsConsts.StepHeight);

        Assert.Equal(1.0 - player.MaxX, move.X);
        Assert.Equal(0.0, move.Y);
    }

    [Fact]
    public void TheStepHeightsAreEveryBottomAndTopWithinAStep()
    {
        var feet = new Aabb(0, 1, 0, 0.6, 2.8, 0.6);
        var colliders = new List<Aabb>
        {
            new(0, 0, 0, 1, 1, 1), // the ground: its top is where the move ended, its bottom below the feet
            new(1, 1, 0, 2, 1.5, 1), // a half block: 0.5
            new(1, 1.2, 0, 2, 1.4, 1), // a box off the ground: its bottom 0.2 and its top 0.4 (before, 0.2 was missed)
            new(2, 1, 0, 3, 2, 1), // a full block: too high
        };

        Assert.Equal(new[] { 0.2f, 0.4f, 0.5f },
            CollisionDetector.CollectCandidateStepUpHeights(feet, colliders, PhysicsConsts.StepHeight, 0f));
        Assert.Empty(CollisionDetector.CollectCandidateStepUpHeights(feet, [new Aabb(2, 1, 0, 3, 2, 1)],
            PhysicsConsts.StepHeight, 0f));
    }

    [Fact]
    public void AWallABoxIsAHairInsideStillStopsIt()
    {
        // A rounding can leave a box 1E-9 inside the face of the wall it stopped at; the game's 1E-7 allowance stops it
        // there again (before, the wall was not ahead of the box any more and the box walked into it).
        var wall = new Aabb(1, 0, 0, 2, 1, 1);
        var box = new Aabb(0.4 + 1e-9, 0, 0, 1 + 1e-9, 1, 1);

        Assert.InRange(box.CollideX(wall, 0.1), -1e-7, 0.0);
    }

    [Fact]
    public void ABoxAHairIntoTheSideOfAWallSlidesAlongIt()
    {
        // The next block of a wall the box slides along: the box reaches 1E-9 into the wall's plane, which the game does
        // not count (before, the box stopped at every block of the wall).
        var nextBlock = new Aabb(1, 0, 0, 2, 1, 1);
        var box = new Aabb(0.4 + 1e-9, 0, -0.7, 1 + 1e-9, 1, -0.1);

        Assert.Equal(0.3, box.CollideZ(nextBlock, 0.3));
    }

    [Fact]
    public void ThePlayersBoxIsTheGames()
    {
        var physics = new PlayerPhysics();
        Aabb box = physics.GetBoundingBox();

        Assert.Equal((double)0.3f, box.MaxX);
        Assert.Equal((double)-0.3f, box.MinX);
        Assert.Equal((double)1.8f, box.MaxY);
    }

    [Fact]
    public void TheSneakSlowdownFollowsTheKeyATickLate()
    {
        var physics = new PlayerPhysics();
        var walk = new MovementInput { Forward = true };
        var sneakWalk = new MovementInput { Forward = true, Sneak = true };

        physics.ApplyInput(walk);
        Assert.Equal(0.98f, physics.Zza);

        physics.ApplyInput(sneakWalk); // the key goes down: this tick still at full speed, the edge back-off holds already
        Assert.Equal(0.98f, physics.Zza);
        Assert.True(physics.Sneaking);

        physics.ApplyInput(sneakWalk);
        Assert.Equal(0.98f * 0.3f, physics.Zza);

        physics.ApplyInput(walk); // the key goes up (AntiAFK starting a walk): this tick still slowed
        Assert.Equal(0.98f * 0.3f, physics.Zza);
        Assert.False(physics.Sneaking);

        physics.ApplyInput(walk);
        Assert.Equal(0.98f, physics.Zza);
    }

    [Fact]
    public void ADiagonalReachesTheUnitSquareFrom1215()
    {
        // LocalPlayer.modifyInputSpeedForSquareMovement (1.21.5+): a normalized diagonal stretched to the square's corner,
        // so each axis gets what a single key gets while sneaking, and the full length while walking. Before, the client
        // normalized and scaled it to 0.98 (2% slow) and 0.294 (sneaking: 30% slow on each axis).
        Assert.Equal((0.70710671f, 0.70710671f), PlayerPhysics.ModifyInput(true, 1f, 1f, false, 0.3f));
        Assert.Equal((0.98f * 0.3f, 0.98f * 0.3f), PlayerPhysics.ModifyInput(true, 1f, 1f, true, 0.3f));
        Assert.Equal((0f, 0.98f), PlayerPhysics.ModifyInput(true, 0f, 1f, false, 0.3f));
        Assert.Equal((0f, 0f), PlayerPhysics.ModifyInput(true, 0f, 0f, true, 0.3f));

        // Before 1.21.5 the keys go as they are (the walk's input vector is normalized later, when longer than 1)
        Assert.Equal((0.98f, 0.98f), PlayerPhysics.ModifyInput(false, 1f, 1f, false, 0.3f));
        Assert.Equal((-0.98f * 0.3f, 0.98f * 0.3f), PlayerPhysics.ModifyInput(false, -1f, 1f, true, 0.3f));
    }

    [Fact]
    public void SwiftSneakSpeedsTheCrouchingWalk()
    {
        // The server's sneaking speed (Swift Sneak III: +0.45 on 0.3), as the game takes it from Update Attributes
        var physics = new PlayerPhysics();
        physics.SneakingSpeedAttribute.SetFromServer(0.3,
            [new PlayerAttribute.Modifier("minecraft:enchantment.swift_sneak/legs", 0.45000001788139343, 0)]);
        var sneakWalk = new MovementInput { Forward = true, Sneak = true };

        physics.ApplyInput(sneakWalk);
        physics.ApplyInput(sneakWalk);

        Assert.Equal(0.98f * 0.75f, physics.Zza);
    }

    [Fact]
    public void AnAttributeIsWorkedOutAsTheGameDoes()
    {
        var speed = new PlayerAttribute(0.1f, 0.0, 1024.0);
        speed.SetFromServer(0.1f, [
            new PlayerAttribute.Modifier("a", 0.02, 0),
            new PlayerAttribute.Modifier("b", 0.2, 1),
            new PlayerAttribute.Modifier("c", 0.3, 2),
        ]);
        double added = (double)0.1f + 0.02;
        Assert.Equal((added + added * 0.2) * 1.3, speed.Value);

        // Out of range values are clamped (RangedAttribute): the sneaking speed is at most 1
        var sneaking = new PlayerAttribute(0.3, 0.0, 1.0);
        sneaking.SetFromServer(0.3, [new PlayerAttribute.Modifier("x", 2.0, 0)]);
        Assert.Equal(1.0, sneaking.Value);

        // The server's modifiers replace the client's own, its sprint modifier too (handleUpdateAttributes)
        var physics = new PlayerPhysics();
        physics.SetSprinting(true);
        Assert.Equal((float)((double)0.1f * (1.0 + 0.3f)), physics.MovementSpeed);
        physics.MovementSpeedAttribute.SetFromServer(0.1f, []);
        Assert.Equal(0.1f, physics.MovementSpeed);
    }

    [Theory]
    [InlineData(AttributesKept.None, 0.3, 0.1f)]
    [InlineData(AttributesKept.Bases, 0.4, 0.1f)]
    [InlineData(AttributesKept.All, 0.85, 0.12f)]
    public void ANewPlayerKeepsTheAttributesAsTheGameDoes(AttributesKept kept, double sneaking, float movement)
    {
        // A join has the defaults; a respawn after death keeps the bases, its modifiers go with the dead player
        // (assignBaseValues: the Swift Sneak and Speed below); a change of dimension keeps them (assignAllValues)
        var physics = new PlayerPhysics();
        physics.SneakingSpeedAttribute.SetFromServer(0.4, [
            new PlayerAttribute.Modifier("minecraft:enchantment.swift_sneak/legs", 0.45, 0),
        ]);
        physics.MovementSpeedAttribute.SetFromServer(0.1f, [
            new PlayerAttribute.Modifier("minecraft:effect.speed", 0.2, 2),
        ]);
        physics.SetSprinting(true);

        physics.ResetPlayer(kept);

        Assert.Equal(sneaking, physics.SneakingSpeedAttribute.Value, 1e-12);
        Assert.Equal(movement, physics.MovementSpeed, 1e-6f);
        Assert.False(physics.Sprinting);
    }

    [Fact]
    public void SprintingStartsWithTheKeyAndStopsAsInTheGame()
    {
        var physics = new PlayerPhysics();
        var sprintWalk = new MovementInput { Forward = true, Sprint = true };
        var walk = new MovementInput { Forward = true };

        physics.ApplyInput(sprintWalk);
        Assert.True(physics.Sprinting);
        Assert.Equal((float)((double)0.1f * (1.0 + 0.3f)), physics.MovementSpeed);

        physics.ApplyInput(walk); // releasing the key alone does not stop it
        Assert.True(physics.Sprinting);

        physics.HorizontalCollision = true; // a wall hit at a small angle: it goes on
        physics.MinorHorizontalCollision = true;
        physics.ApplyInput(walk);
        Assert.True(physics.Sprinting);

        physics.MinorHorizontalCollision = false; // a wall hit straight on stops it
        physics.ApplyInput(walk);
        Assert.False(physics.Sprinting);
        Assert.Equal(0.1f, physics.MovementSpeed);

        physics.HorizontalCollision = false;
        physics.FoodLevel = 6; // hungry: no sprinting
        physics.ApplyInput(sprintWalk);
        Assert.False(physics.Sprinting);

        physics.FoodLevel = 20;
        physics.ApplyInput(new MovementInput { Sprint = true }); // no forward key
        Assert.False(physics.Sprinting);

        physics.ApplyInput(new MovementInput { Forward = true, Sneak = true }); // crouching from the next tick
        physics.ApplyInput(new MovementInput { Forward = true, Sprint = true, Sneak = true }); // crouching: no start
        Assert.False(physics.Sprinting);
    }

    [Theory]
    // yaw, the move after the wall, minor (within 8 degrees of where the keys push)
    [InlineData(0f, 0.0001, 0.2, true)] // nearly straight on: the wall in another axis barely changed the way
    [InlineData(0f, 0.2, 0.0, false)] // pushed along the wall at a right angle
    [InlineData(0f, 0.01, 0.2, true)] // about 3 degrees off
    [InlineData(0f, 0.05, 0.2, false)] // about 14 degrees off
    public void AMinorCollisionIsWithinEightDegrees(float yaw, double x, double z, bool minor)
    {
        Assert.Equal(minor, PlayerPhysics.IsHorizontalCollisionMinor(yaw, 0f, 0.98f, new Vec3d(x, 0, z)));
    }

    private static Func<Aabb, List<Aabb>> Colliders(params Aabb[] shapes) =>
        searchBox => shapes.Where(shape => shape.Intersects(searchBox)).ToList();
}

/// <summary>Client 4.2.11: the physics against a world of blocks (the global block palette is set, so one at a time).</summary>
[Collection(BlockPaletteCollection.Name)]
public sealed class PhysicsWorldTests : IDisposable
{
    private readonly BlockPalette previousPalette = Block.Palette;

    public PhysicsWorldTests()
    {
        Block.Palette = new Palette112();
    }

    public void Dispose()
    {
        Block.Palette = previousPalette;
    }

    [Fact]
    public void ASlowStepOffAnEdgeKeepsTheGroundThatTick()
    {
        // The game resolves the fall first, while the box still overlaps the block, then the step: the player is on the
        // ground this tick and falls the next. Before, a step slower than the fall went first and the client fell a
        // tick early (an anticheat's simulation and ground checks flag it).
        World world = WorldWithStone((0, 0, 0), (-1, 0, 0));
        var physics = StandingAt(1.29, 1.0, 0.5, new Vec3d(0.05, -0.0784, 0));

        physics.ApplyInput(new MovementInput());
        physics.Tick(world);

        Assert.Equal(1.0, physics.Position.Y);
        Assert.True(physics.OnGround);
        Assert.Equal(1.34, physics.Position.X, 12);
    }

    [Fact]
    public void SneakingStopsAtTheEdge()
    {
        World world = WorldWithStone((0, 0, 0));
        var physics = StandingAt(0.75, 1.0, 0.5, new Vec3d(0.7, -0.0784, 0));

        physics.ApplyInput(new MovementInput { Sneak = true });
        physics.Tick(world);

        // The move shrinks by 0.05 until the feet would no longer be over the block: 0.5, the box still 0.05 on it.
        Assert.Equal(1.25, physics.Position.X, 9);
        Assert.Equal(1.0, physics.Position.Y);
        Assert.True(physics.OnGround);
    }

    [Fact]
    public void WalkingGoesOverTheEdge()
    {
        World world = WorldWithStone((0, 0, 0));
        var physics = StandingAt(0.75, 1.0, 0.5, new Vec3d(0.7, -0.0784, 0));

        physics.ApplyInput(new MovementInput());
        physics.Tick(world);

        Assert.Equal(1.45, physics.Position.X, 9);
    }

    [Fact]
    public void TheSneakKeyCrouchesThePoseAtTheTicksEnd()
    {
        // Player.updatePlayerPose runs after the move: the box is 1.5 high from the tick after the key goes down
        World world = WorldWithStone((0, 0, 0));
        var physics = StandingAt(0.5, 1.0, 0.5, Vec3d.Zero);

        physics.ApplyInput(new MovementInput { Sneak = true });
        Assert.False(physics.CrouchPose);
        physics.Tick(world);
        Assert.True(physics.CrouchPose);
        Assert.Equal((double)1.5f, physics.GetBoundingBox().YSize);

        physics.ApplyInput(new MovementInput());
        physics.Tick(world);
        Assert.False(physics.CrouchPose);
    }

    [Fact]
    public void APlayerUnderALowCeilingCrouchesWithoutTheKey()
    {
        // 1.5 between the feet and the ceiling: the standing box does not fit, the crouching one does, so the game
        // crouches the player and slows its walk as crouching (LocalPlayer.crouching: it cannot stand). Before, the box
        // stayed 1.8 high and the walk at full speed.
        World world = WorldWithStone((0, 2, 0));
        var physics = new PlayerPhysics();
        physics.SetPosition(0.5, 0.5, 0.5);

        physics.ApplyInput(new MovementInput());
        physics.Tick(world);
        Assert.True(physics.CrouchPose);

        physics.UpdateEnvironment(world);
        physics.ApplyInput(new MovementInput { Forward = true });
        Assert.True(physics.Crouching);
        Assert.Equal(0.98f * 0.3f, physics.Zza);
    }

    private static PlayerPhysics StandingAt(double x, double y, double z, Vec3d motion)
    {
        var physics = new PlayerPhysics();
        physics.SetPosition(x, y, z);
        physics.OnGround = true;
        physics.DeltaMovement = motion;
        return physics;
    }

    /// <summary>A world of stone blocks (the 1.12 palette's stone: a full block) in loaded chunks.</summary>
    internal static World WorldWithStone(params (int X, int Y, int Z)[] blocks)
    {
        var world = new World();
        foreach (var (x, y, z) in blocks)
        {
            var location = new Location(x, y, z);
            if (world.GetChunkColumn(location) is null)
                world.StoreChunk(location.ChunkX, location.ChunkY, location.ChunkZ, 16, null, true);
            world.SetBlock(location, new Block(1, 0)); // stone in the 1.12 palette: a full block
        }
        return world;
    }
}
