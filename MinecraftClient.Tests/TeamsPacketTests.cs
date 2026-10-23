using System.Text;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// The Teams packet (set player team) read the way each protocol version writes it. 1.7 sends no name tag
/// visibility or color and a short player count; reading it the 1.8 way ran past the packet ("Queue empty").
/// </summary>
public sealed class TeamsPacketTests
{
    private const int Mc1710 = 5;
    private const int Mc18 = Protocol18Handler.MC_1_8_Version;
    private const int Mc1122 = Protocol18Handler.MC_1_12_2_Version;
    private const int Mc1165 = 754;
    private const int Mc263 = Protocol18Handler.MC_26_3_Version;

    private static Queue<byte> Packet(params byte[][] parts) => new(parts.SelectMany(part => part));

    /// <summary>A network NBT string root (1.20.3+ text component).</summary>
    private static byte[] NbtText(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        return [8, (byte)(utf8.Length >> 8), (byte)utf8.Length, .. utf8];
    }

    [Fact]
    public void CreateOn17HasNoNameTagFieldsAndAShortPlayerCount()
    {
        DataTypes data = new(Mc1710);
        Queue<byte> packet = Packet(data.GetString("red"), [0], data.GetString("Red team"), data.GetString("[R] "),
            data.GetString(""), [1], data.GetShort(2), data.GetString("Steve"), data.GetString("Alex"));

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc1710);

        Assert.Empty(packet);
        Assert.Equal(("red", (byte)0, "Red team", "[R] ", "", (byte)1),
            (team.Name, team.Method, team.DisplayName, team.Prefix, team.Suffix, team.FriendlyFlags));
        Assert.Equal((string.Empty, string.Empty, -1), (team.NameTagVisibility, team.CollisionRule, team.Color));
        Assert.Equal(new[] { "Steve", "Alex" }, team.Players);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void PlayerChangesOn17UseAShortCount(byte method)
    {
        DataTypes data = new(Mc1710);
        Queue<byte> packet = Packet(data.GetString("red"), [method], data.GetShort(1), data.GetString("Steve"));

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc1710);

        Assert.Empty(packet);
        Assert.Equal(new[] { "Steve" }, team.Players);
    }

    [Fact]
    public void RemoveCarriesOnlyTheName()
    {
        DataTypes data = new(Mc1710);
        Queue<byte> packet = Packet(data.GetString("red"), [1]);

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc1710);

        Assert.Empty(packet);
        Assert.Equal(("red", (byte)1), (team.Name, team.Method));
        Assert.Empty(team.Players);
    }

    [Fact]
    public void UpdateOn18HasNameTagVisibilityAndColor()
    {
        DataTypes data = new(Mc18);
        Queue<byte> packet = Packet(data.GetString("blue"), [2], data.GetString("Blue"), data.GetString("[B] "),
            data.GetString(""), [3], data.GetString("hideForOtherTeams"), [9]);

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc18);

        Assert.Empty(packet);
        Assert.Equal(("Blue", (byte)3, "hideForOtherTeams", string.Empty, 9),
            (team.DisplayName, team.FriendlyFlags, team.NameTagVisibility, team.CollisionRule, team.Color));
        Assert.Empty(team.Players);
    }

    [Fact]
    public void CreateOn1122AddsTheCollisionRule()
    {
        DataTypes data = new(Mc1122);
        Queue<byte> packet = Packet(data.GetString("blue"), [0], data.GetString("Blue"), data.GetString(""),
            data.GetString(""), [0], data.GetString("always"), data.GetString("pushOwnTeam"), [12],
            DataTypes.GetVarInt(1), data.GetString("Karsi"));

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc1122);

        Assert.Empty(packet);
        Assert.Equal(("always", "pushOwnTeam", 12), (team.NameTagVisibility, team.CollisionRule, team.Color));
        Assert.Equal(new[] { "Karsi" }, team.Players);
    }

    [Fact]
    public void CreateOn1165UsesJsonTextAndVarIntColor()
    {
        DataTypes data = new(Mc1165);
        Queue<byte> packet = Packet(data.GetString("green"), [0], data.GetString("""{"text":"Green"}"""), [1],
            data.GetString("never"), data.GetString("never"), DataTypes.GetVarInt(10),
            data.GetString("""{"text":"[G] "}"""), data.GetString("""{"text":""}"""), DataTypes.GetVarInt(0));

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc1165);

        Assert.Empty(packet);
        Assert.Equal(("Green", "[G] ", "never", "never", 10),
            (team.DisplayName, team.Prefix, team.NameTagVisibility, team.CollisionRule, team.Color));
    }

    [Fact]
    public void CreateOn263UsesTheNewFieldOrder()
    {
        DataTypes data = new(Mc263);
        Queue<byte> packet = Packet(data.GetString("smp"), [0], NbtText("SMP"), NbtText("[SMP] "), NbtText(""),
            DataTypes.GetVarInt(2), DataTypes.GetVarInt(3), data.GetBool(true), DataTypes.GetVarInt(6), [1],
            DataTypes.GetVarInt(2), data.GetString("AfkBot"), data.GetString("Karsi"));

        var team = Protocol18Handler.ReadTeamsPacket(data, packet, Mc263);

        Assert.Empty(packet);
        Assert.Equal(("SMP", "[SMP] ", "hideForOtherTeams", "pushOwnTeam", 6, (byte)1),
            (team.DisplayName, team.Prefix, team.NameTagVisibility, team.CollisionRule, team.Color, team.FriendlyFlags));
        Assert.Equal(new[] { "AfkBot", "Karsi" }, team.Players);
    }
}
