using MinecraftClient.ChatBots;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.11: AntiAFK searches its walk off the client tick, so the tick never waits for it (it stopped the client
/// for up to 2 seconds). The search itself, against a world of blocks (the global block palette: one at a time).
/// </summary>
[Collection(BlockPaletteCollection.Name)]
public sealed class AntiAfkTests : IDisposable
{
    private readonly BlockPalette previousPalette = Block.Palette;

    public AntiAfkTests()
    {
        Block.Palette = new Palette112();
    }

    public void Dispose()
    {
        Block.Palette = previousPalette;
    }

    [Fact]
    public void TheWalkSearchFindsThePathToTheFirstReachableGoal()
    {
        var floor = new List<(int X, int Y, int Z)>();
        for (int x = -2; x <= 6; x++)
            for (int z = -2; z <= 2; z++)
                floor.Add((x, 0, z));
        World world = PhysicsWorldTests.WorldWithStone(floor.ToArray());
        var start = new Location(0.5, 1, 0.5);
        var goal = new Location(3.5, 1, 0.5);

        Queue<Location>? path = AntiAFK.FindWalk(world, start, [goal], TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.NotNull(path);
        Assert.Equal(goal, path.Last());
    }

    [Fact]
    public void ACancelledOrUnloadedSearchFindsNone()
    {
        World world = PhysicsWorldTests.WorldWithStone((0, 0, 0), (1, 0, 0), (2, 0, 0));
        var start = new Location(0.5, 1, 0.5);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Null(AntiAFK.FindWalk(world, start, [new Location(2.5, 1, 0.5)], TimeSpan.FromSeconds(5),
            cancelled.Token));

        // A goal in a chunk the client has not loaded ends the search (Movement.CheckChunkLoading)
        Assert.Null(AntiAFK.FindWalk(world, start, [new Location(40.5, 1, 0.5)], TimeSpan.FromSeconds(5),
            CancellationToken.None));
    }
}
