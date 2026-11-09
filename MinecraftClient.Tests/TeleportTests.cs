using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.8: a server teleport is answered as the game answers it. Anticheats send their setbacks with negative ids
/// (Grim: <c>random.nextInt() | Integer.MIN_VALUE</c>) and accept one only by the client's position and look sent right
/// away, before the server's next ping; the client took a negative id for "no id", sent nothing, and the setback came again
/// every tick until the anticheat kicked the player (Grim BadPacketsN with AimDuplicateLook, from a production kick).
/// </summary>
public sealed class TeleportTests
{
    private const int GrimSetbackId = unchecked((int)0x8000_1234); // random.nextInt() | Integer.MIN_VALUE

    private static readonly int[] IdVersionsBefore263 =
    [
        Protocol18Handler.MC_1_9_Version,
        Protocol18Handler.MC_1_12_2_Version,
        Protocol18Handler.MC_1_16_5_Version,
        Protocol18Handler.MC_1_19_2_Version,
        Protocol18Handler.MC_1_20_4_Version,
        Protocol18Handler.MC_1_21_Version,
        Protocol18Handler.MC_1_21_2_Version,
        Protocol18Handler.MC_1_21_5_Version,
        Protocol18Handler.MC_1_21_11_Version,
        Protocol18Handler.MC_26_1_Version,
        Protocol18Handler.MC_26_2_Version,
    ];

    public static TheoryData<int> IdVersions()
    {
        TheoryData<int> data = new();
        foreach (int version in IdVersionsBefore263)
            data.Add(version);
        return data;
    }

    // Location's own equality compares the block (the integer parts) only.
    private static void AssertAt(Location location, double x, double y, double z)
    {
        Assert.Equal(x, location.X);
        Assert.Equal(y, location.Y);
        Assert.Equal(z, location.Z);
    }

    private static Protocol18Handler.ServerTeleport Setback(int id = GrimSetbackId) =>
        // Grim's setback: an absolute position, the look relative and unchanged (0, 0)
        new(10.5, 64.0, -3.25, 0F, 0F, Protocol18Handler.TeleportRelativeYaw | Protocol18Handler.TeleportRelativePitch,
            true, id);

    [Theory]
    [MemberData(nameof(IdVersions))]
    public void ANegativeIdIsAcceptedAndThePositionAndLookFollowAtOnce(int version)
    {
        DataTypes data = new(version);
        Location at = new(10.5, 64.0, -3.25);

        var replies = Protocol18Handler.TeleportReplies(version, data, Setback(), at, 123.5F, -12.25F, sendsMoves: true);

        Assert.Equal(2, replies.Count);
        Assert.Equal(PacketTypesOut.TeleportConfirm, replies[0].Type);
        Assert.Equal(DataTypes.GetVarInt(GrimSetbackId), replies[0].Data);
        Assert.Equal(PacketTypesOut.PlayerPositionAndRotation, replies[1].Type);
        Assert.Equal(data.ConcatBytes(data.GetDouble(10.5), data.GetDouble(64.0), data.GetDouble(-3.25),
            data.GetFloat(123.5F), data.GetFloat(-12.25F), new byte[] { 0 }), replies[1].Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void EveryIdIsAccepted(int id)
    {
        int version = Protocol18Handler.MC_26_1_Version;
        var replies = Protocol18Handler.TeleportReplies(version, new DataTypes(version), Setback(id),
            new Location(0, 70, 0), 0F, 0F, sendsMoves: true);

        Assert.Equal(DataTypes.GetVarInt(id), replies[0].Data);
    }

    [Fact]
    public void From263TheAcceptanceCarriesThePositionAndLookAndNoMoveFollows()
    {
        int version = Protocol18Handler.MC_26_3_Version;
        DataTypes data = new(version);

        var replies = Protocol18Handler.TeleportReplies(version, data, Setback(), new Location(10.5, 64.0, -3.25),
            123.5F, -12.25F, sendsMoves: true);

        var confirm = Assert.Single(replies);
        Assert.Equal(PacketTypesOut.TeleportConfirm, confirm.Type);
        Assert.Equal(data.ConcatBytes(DataTypes.GetVarInt(GrimSetbackId), data.GetDouble(10.5), data.GetDouble(64.0),
            data.GetDouble(-3.25), data.GetFloat(123.5F), data.GetFloat(-12.25F)), confirm.Data);
    }

    [Theory]
    [InlineData(Protocol18Handler.MC_1_12_2_Version)]
    [InlineData(Protocol18Handler.MC_26_1_Version)]
    public void AClientWithoutMovesOnlyAcceptsTheTeleport(int version)
    {
        var replies = Protocol18Handler.TeleportReplies(version, new DataTypes(version), Setback(),
            new Location(10.5, 64.0, -3.25), 0F, 0F, sendsMoves: false);

        var confirm = Assert.Single(replies);
        Assert.Equal(PacketTypesOut.TeleportConfirm, confirm.Type);
    }

    [Fact]
    public void In18TheTeleportHasNoIdAndOnlyTheMoveAnswersIt()
    {
        int version = Protocol18Handler.MC_1_8_Version;
        DataTypes data = new(version);
        Protocol18Handler.ServerTeleport teleport = new(1.5, 70.0, 2.5, 90F, 10F, 0, false, 0);

        var replies = Protocol18Handler.TeleportReplies(version, data, teleport, new Location(1.5, 70.0, 2.5), 90F, 10F,
            sendsMoves: true);

        var move = Assert.Single(replies);
        Assert.Equal(PacketTypesOut.PlayerPositionAndRotation, move.Type);
        Assert.Equal(data.ConcatBytes(data.GetDouble(1.5), data.GetDouble(70.0), data.GetDouble(2.5), data.GetFloat(90F),
            data.GetFloat(10F), new byte[] { 0 }), move.Data);
        Assert.Empty(Protocol18Handler.TeleportReplies(version, data, teleport, new Location(1.5, 70.0, 2.5), 90F, 10F,
            sendsMoves: false));
    }

    [Fact]
    public void BeforeOneEightNothingAnswersTheTeleport()
    {
        const int version = 5; // 1.7.6 - 1.7.10
        Protocol18Handler.ServerTeleport teleport = new(1.5, 71.62, 2.5, 90F, 10F, 0, false, 0);

        Assert.Empty(Protocol18Handler.TeleportReplies(version, new DataTypes(version), teleport,
            new Location(1.5, 71.62, 2.5), 90F, 10F, sendsMoves: true));
    }

    [Theory]
    [MemberData(nameof(IdVersions))]
    public void ARelativeLookKeepsThePlayersOwnLook(int version)
    {
        // Grim's setback: before, a version older than 26.3 took its 0/0 as the look and the view snapped to 0/0.
        var (location, yaw, pitch) = Protocol18Handler.ResolveServerTeleport(version, Setback(),
            new Location(11.0, 64.5, -3.0), 123.5F, -12.25F);

        AssertAt(location, 10.5, 64.0, -3.25);
        Assert.Equal(123.5F, yaw);
        Assert.Equal(-12.25F, pitch);
    }

    [Fact]
    public void RelativeValuesAddToThePlayersOwn()
    {
        int version = Protocol18Handler.MC_26_1_Version;
        Protocol18Handler.ServerTeleport teleport = new(1.0, -0.5, 2.0, 30F, -5F,
            Protocol18Handler.TeleportRelativeX | Protocol18Handler.TeleportRelativeY | Protocol18Handler.TeleportRelativeZ
            | Protocol18Handler.TeleportRelativeYaw | Protocol18Handler.TeleportRelativePitch, true, 7);

        var (location, yaw, pitch) = Protocol18Handler.ResolveServerTeleport(version, teleport,
            new Location(100.0, 65.0, -50.0), 90F, 20F);

        AssertAt(location, 101.0, 64.5, -48.0);
        Assert.Equal(120F, yaw);
        Assert.Equal(15F, pitch);
    }

    [Theory]
    // version, yaw, pitch (absolute), expected yaw, expected pitch: the game's own rounding of a teleport's look
    [InlineData(Protocol18Handler.MC_1_8_Version, 370F, 100F, 10F, 100F)]
    [InlineData(Protocol18Handler.MC_1_8_Version, 0F, 400F, 0F, 40F)]
    [InlineData(Protocol18Handler.MC_1_12_2_Version, 370F, 100F, 10F, 90F)]
    [InlineData(Protocol18Handler.MC_1_19_2_Version, -370F, -100F, -10F, -90F)]
    [InlineData(Protocol18Handler.MC_1_19_3_Version, 370F, 100F, 370F, 100F)]
    [InlineData(Protocol18Handler.MC_1_21_Version, 370F, 100F, 370F, 100F)]
    [InlineData(Protocol18Handler.MC_1_21_2_Version, 370F, 450F, 370F, 90F)]
    [InlineData(Protocol18Handler.MC_1_21_4_Version, 370F, 400F, 370F, 40F)]
    [InlineData(Protocol18Handler.MC_1_21_5_Version, 370F, 400F, 370F, 90F)]
    [InlineData(Protocol18Handler.MC_26_1_Version, 370F, -100F, 370F, -90F)]
    public void AnAbsoluteLookIsRoundedAsTheGameRoundsIt(int version, float yaw, float pitch, float expectedYaw,
        float expectedPitch)
    {
        Protocol18Handler.ServerTeleport teleport = new(0, 70, 0, yaw, pitch, 0, version >= Protocol18Handler.MC_1_9_Version, 3);

        var (_, resolvedYaw, resolvedPitch) = Protocol18Handler.ResolveServerTeleport(version, teleport, new Location(),
            45F, 45F);

        Assert.Equal(expectedYaw, resolvedYaw);
        Assert.Equal(expectedPitch, resolvedPitch);
    }

    [Fact]
    public void A261TeleportIsReadWithItsNegativeId()
    {
        int version = Protocol18Handler.MC_26_1_Version;
        DataTypes data = new(version);
        Queue<byte> packet = new(data.ConcatBytes(DataTypes.GetVarInt(GrimSetbackId), data.GetDouble(10.5),
            data.GetDouble(64.0), data.GetDouble(-3.25), data.GetDouble(0.1), data.GetDouble(0.2), data.GetDouble(0.3),
            data.GetFloat(0F), data.GetFloat(0F), DataTypes.GetInt(0x18)));

        Protocol18Handler.ServerTeleport teleport = Protocol18Handler.ReadServerTeleport(data, packet, version);

        Assert.Equal(new Protocol18Handler.ServerTeleport(10.5, 64.0, -3.25, 0F, 0F, 0x18, true, GrimSetbackId), teleport);
        Assert.Empty(packet);
    }

    [Fact]
    public void A118TeleportIsReadWithItsIdAndDismountFlag()
    {
        int version = Protocol18Handler.MC_1_18_2_Version;
        DataTypes data = new(version);
        Queue<byte> packet = new(data.ConcatBytes(data.GetDouble(1.0), data.GetDouble(2.0), data.GetDouble(3.0),
            data.GetFloat(4F), data.GetFloat(5F), new byte[] { 0x08 }, DataTypes.GetVarInt(-42), data.GetBool(false)));

        Protocol18Handler.ServerTeleport teleport = Protocol18Handler.ReadServerTeleport(data, packet, version);

        Assert.Equal(new Protocol18Handler.ServerTeleport(1.0, 2.0, 3.0, 4F, 5F, 0x08, true, -42), teleport);
        Assert.Empty(packet);
    }

    [Fact]
    public void An18TeleportHasNoId()
    {
        int version = Protocol18Handler.MC_1_8_Version;
        DataTypes data = new(version);
        Queue<byte> packet = new(data.ConcatBytes(data.GetDouble(1.0), data.GetDouble(2.0), data.GetDouble(3.0),
            data.GetFloat(4F), data.GetFloat(5F), new byte[] { 0x18 }));

        Protocol18Handler.ServerTeleport teleport = Protocol18Handler.ReadServerTeleport(data, packet, version);

        Assert.False(teleport.HasId);
        Assert.Equal(0x18, teleport.Relative);
        Assert.Empty(packet);
    }

    [Fact]
    public void ARotationGoesOnlyWhenItChanged()
    {
        // The look the client already sent is not sent again (Grim AimDuplicateLook flags an unchanged rotation).
        Assert.False(Protocol18Handler.RotationChanged(123.5F, -12.25F, 123.5F, -12.25F));
        Assert.True(Protocol18Handler.RotationChanged(123.75F, -12.25F, 123.5F, -12.25F));
        Assert.True(Protocol18Handler.RotationChanged(123.5F, -12F, 123.5F, -12.25F));
        Assert.False(Protocol18Handler.RotationChanged(null, null, 0F, 0F));
        Assert.False(Protocol18Handler.RotationChanged(10F, null, 0F, 0F));
    }

    [Theory]
    // yaw the client works out (0..360), the yaw the server knows, the yaw that goes
    [InlineData(1F, 359F, 361F)] // past north: on to 361, not a 358° snap back (Grim AimModulo360)
    [InlineData(359F, 1F, -1F)]
    [InlineData(90F, 450F, 450F)] // the same look keeps the server's own yaw: nothing changed
    [InlineData(100F, 450F, 460F)]
    [InlineData(10F, 20F, 10F)]
    [InlineData(200F, 10F, -160F)]
    [InlineData(190F, 10F, -170F)] // half a turn goes the negative way
    public void TheYawTurnsTheShortWayFromTheServersYaw(float yaw, float reference, float expected)
    {
        Assert.Equal(expected, Protocol18Handler.NearestYaw(yaw, reference));
    }
}

/// <summary>
/// Client 4.2.8: from 1.21.2 the movement keys go as Player Input (one byte, a bit a key), as the game sends them; Grim
/// predicts a 1.21.2+ client's move from them, and a walk without its forward key was a simulation flag every tick.
/// </summary>
public sealed class PlayerInputTests
{
    [Fact]
    public void EachKeyIsItsBit()
    {
        Assert.Equal(0x00, Protocol18Handler.PlayerInputKeys(false, false, false, false, false, false, false));
        Assert.Equal(0x01, Protocol18Handler.PlayerInputKeys(true, false, false, false, false, false, false));
        Assert.Equal(0x02, Protocol18Handler.PlayerInputKeys(false, true, false, false, false, false, false));
        Assert.Equal(0x04, Protocol18Handler.PlayerInputKeys(false, false, true, false, false, false, false));
        Assert.Equal(0x08, Protocol18Handler.PlayerInputKeys(false, false, false, true, false, false, false));
        Assert.Equal(0x10, Protocol18Handler.PlayerInputKeys(false, false, false, false, true, false, false));
        Assert.Equal(Protocol18Handler.PlayerInputSneak,
            Protocol18Handler.PlayerInputKeys(false, false, false, false, false, true, false));
        Assert.Equal(0x40, Protocol18Handler.PlayerInputKeys(false, false, false, false, false, false, true));
    }

    [Fact]
    public void AWalkThatJumpsPressesForwardAndJump()
    {
        Assert.Equal(0x11, Protocol18Handler.PlayerInputKeys(true, false, false, false, true, false, false));
        Assert.Equal(0x21, Protocol18Handler.PlayerInputKeys(true, false, false, false, false, true, false));
    }
}
