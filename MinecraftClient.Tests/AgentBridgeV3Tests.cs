using System.Text;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Mapping;
using static MinecraftClient.ChatBots.MovePlanner;

namespace MinecraftClient.Tests;

/// <summary>
/// The agent bridge's version 3 (docs/agent-bridge.md, "Version 3"): movement operations, the <c>move</c> event and
/// <c>pos.facing</c>, only with <c>MCC_AGENT_MOVE=1</c>; the path search; the movement controller's own limits.
/// Versions 1 and 2 (AgentBridgeTests.cs, AgentBridgeV2Tests.cs) are unchanged.
/// </summary>
public sealed class AgentBridgeV3Tests
{
    private static AgentOperation Parse(string json, long arrival = 7)
    {
        Assert.True(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), arrival, movement: true,
            out AgentOperation? operation, out _, out _, out string? reason), reason);
        return operation!;
    }

    private static string Refuse(string json, bool movement = true)
    {
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 1, movement, out _, out _, out _,
            out string? reason));
        return reason!;
    }

    [Theory]
    [InlineData("""{"op":"step","dir":"forward","n":1}""")]
    [InlineData("""{"op":"jump"}""")]
    [InlineData("""{"op":"sneak","on":true}""")]
    [InlineData("""{"op":"goto","x":1,"y":64,"z":1}""")]
    [InlineData("""{"op":"approach","name":"Kaan_07"}""")]
    [InlineData("""{"op":"follow","name":"Kaan_07"}""")]
    [InlineData("""{"op":"stop"}""")]
    public void MovementIsUnknownWithoutTheVariable(string json)
    {
        Assert.Equal("unknown_op", Refuse(json, movement: false));
        Assert.False(AgentMovement.Enabled); // the test process never sets MCC_AGENT_MOVE
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 1, out _, out _, out _, out string? reason));
        Assert.Equal("unknown_op", reason);
    }

    [Fact]
    public void MovementOperationsAreParsed()
    {
        AgentOperation step = Parse("""{"op":"step","id":"m1","dir":"left","n":8}""");
        Assert.Equal(("step", "m1", "left", 8, true), (step.Op, step.Id, step.Dir, step.Count, step.Moves));

        AgentOperation sneak = Parse("""{"op":"sneak","on":true}""");
        Assert.True(sneak.On);

        AgentOperation walk = Parse("""{"op":"goto","x":-30000000,"y":-2048,"z":30000000}""");
        Assert.Equal((-30_000_000, -2048, 30_000_000), (walk.X, walk.Y, walk.Z));

        AgentOperation follow = Parse("""{"op":"follow","name":"Kaan_07"}""");
        Assert.Equal("Kaan_07", follow.Name);

        AgentOperation stop = Parse("""{"op":"stop","id":"s1"}""");
        Assert.False(stop.Moves);

        Assert.Equal("north", Parse("""{"op":"step","dir":"north","n":2}""").Dir);
        Assert.Equal("click", Parse("""{"op":"click","win":3,"ws":57,"slot":3,"mode":"pickup"}""").Op);
    }

    [Theory]
    [InlineData("""{"op":"step","dir":"up","n":1}""", "bad_dir")]
    [InlineData("""{"op":"step","dir":"forward","n":9}""", "bad_count")]
    [InlineData("""{"op":"step","dir":"forward","n":0}""", "bad_count")]
    [InlineData("""{"op":"step","dir":"forward","n":1.5}""", "bad_count")]
    [InlineData("""{"op":"step","dir":"forward"}""", "bad_request")]
    [InlineData("""{"op":"step","n":1}""", "bad_request")]
    [InlineData("""{"op":"sneak"}""", "bad_request")]
    [InlineData("""{"op":"sneak","on":"true"}""", "bad_request")]
    [InlineData("""{"op":"goto","x":1,"y":64}""", "bad_request")]
    [InlineData("""{"op":"goto","x":1.5,"y":64,"z":1}""", "bad_request")]
    [InlineData("""{"op":"goto","x":1,"y":2049,"z":1}""", "bad_request")]
    [InlineData("""{"op":"goto","x":30000001,"y":64,"z":1}""", "bad_request")]
    [InlineData("""{"op":"follow","name":"Ab"}""", "bad_name")]
    [InlineData("""{"op":"follow","name":"Kaan 07"}""", "bad_name")]
    [InlineData("""{"op":"approach","name":"Çağan"}""", "bad_name")]
    [InlineData("""{"op":"approach"}""", "bad_request")]
    public void MalformedMovementIsRefused(string json, string reason)
    {
        Assert.Equal(reason, Refuse(json));
    }

    [Fact]
    public void MovementAddsPositionAndMoveEventsButNeverToAll()
    {
        Assert.Equal(0, (int)(AgentBridge.EventGroups.All & AgentBridge.EventGroups.Movement));
        Assert.Equal(AgentBridge.EventGroups.All, AgentBridge.ReportedGroups(null, movement: false));
        Assert.Equal(AgentBridge.EventGroups.All | AgentBridge.EventGroups.Movement,
            AgentBridge.ReportedGroups(null, movement: true));
        Assert.Equal(AgentBridge.EventGroups.Position | AgentBridge.EventGroups.Movement,
            AgentBridge.ReportedGroups("", movement: true));
        Assert.Equal(
            AgentBridge.EventGroups.Inventory | AgentBridge.EventGroups.Windows | AgentBridge.EventGroups.Held
            | AgentBridge.EventGroups.Position | AgentBridge.EventGroups.Movement,
            AgentBridge.ReportedGroups("inv,win,held", movement: true));
        Assert.Equal(AgentBridge.EventGroups.None, AgentBridge.ParseEventGroups("move"));
        Assert.Equal(2, AgentBridge.ProtocolVersion);
        Assert.Equal(3, AgentBridge.MovementProtocolVersion);
    }

    [Fact]
    public void DirectionsAreWhatTheCloudVectorsSay()
    {
        JsonArray cases = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors",
            "move-directions-v1.json")))!["directions"]!.AsArray();
        Assert.True(cases.Count >= 29);
        foreach (JsonNode? item in cases)
        {
            string name = (string)item!["name"]!;
            double yaw = (double)item["yaw"]!;
            string dir = (string)item["dir"]!;
            int n = (int)item["n"]!;
            JsonNode expected = item["expected"]!;

            string facing = AgentMovement.FacingOf(yaw);
            string toward = AgentMovement.Resolve(facing, dir)!;
            (int dx, int dz) = AgentMovement.Delta(toward);
            Assert.True((string)expected["facing"]! == facing, $"{name}: facing {facing}");
            Assert.True((string)expected["toward"]! == toward, $"{name}: toward {toward}");
            Assert.True((int)expected["dx"]! == dx * n && (int)expected["dz"]! == dz * n, $"{name}: {dx * n}, {dz * n}");
        }
    }

    [Fact]
    public void FollowNamesAreValidNamesOfAnyCase()
    {
        HashSet<string> names = AgentMovement.ParseNames(" Kaan_07 , AfkBot_2,,bad name,ab,Çağan,Abcdefghijklmnopq ");
        Assert.Equal(2, names.Count);
        Assert.Contains("kaan_07", names);
        Assert.Contains("AFKBOT_2", names);
        Assert.Empty(AgentMovement.ParseNames(null));
        Assert.Empty(AgentMovement.ParseNames(""));
    }

    [Fact]
    public void TheBucketHoldsEightAndGivesFourASecond()
    {
        double now = 0;
        AgentMovement.TokenBucket bucket = new(8, 4, () => now);
        for (int i = 0; i < 8; i++)
            Assert.True(bucket.TryTake());
        Assert.False(bucket.TryTake());
        now = 0.25;
        Assert.True(bucket.TryTake());
        Assert.False(bucket.TryTake());
        now = 10;
        for (int i = 0; i < 8; i++)
            Assert.True(bucket.TryTake());
        Assert.False(bucket.TryTake());
    }

    [Fact]
    public void WithMovementEveryNewBlockIsAPosition()
    {
        // A one-block step north from the middle of a block ends a little short of the next middle: a new block.
        Assert.False(AgentBridge.SameBlock(0.5, -0.45));
        Assert.True(AgentBridge.SameBlock(0.5, 0.99));
        Assert.False(AgentBridge.SameBlock(-0.01, 0.01));
        Assert.True(AgentBridge.SameBlock(-60, -60));
    }

    [Fact]
    public void DistanceIsToTheTargetBlocksCentre()
    {
        Location here = new(100.5, 64, -39.5);
        Assert.Equal(256, AgentMovement.Distance(here, new Cell(356, 64, -40)), 6);
        Assert.Equal(250, AgentMovement.Distance(here, new Cell(300, 214, -40)), 6);
    }
}
