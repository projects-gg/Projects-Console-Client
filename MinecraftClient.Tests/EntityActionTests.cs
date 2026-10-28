using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// Entity actions by protocol version. From 1.21.6 the Player Command packet has no shift key: sneaking is the sneak
/// key of the Player Input packet, and the other commands moved down by two. Before this, a "sneak" sent to a 1.21.6+
/// server was "leave bed" (0) and "stop sneaking" was "start sprinting" (1): AntiAFK's sneaking did not sneak.
/// </summary>
public sealed class EntityActionTests
{
    private static readonly int[] OldProtocols =
    [
        Protocol18Handler.MC_1_8_Version,
        Protocol18Handler.MC_1_12_2_Version,
        Protocol18Handler.MC_1_16_5_Version,
        Protocol18Handler.MC_1_20_4_Version,
        Protocol18Handler.MC_1_21_2_Version,
        Protocol18Handler.MC_1_21_4_Version,
        Protocol18Handler.MC_1_21_5_Version,
    ];

    private static readonly int[] NewProtocols =
    [
        Protocol18Handler.MC_1_21_6_Version,
        Protocol18Handler.MC_1_21_9_Version,
        Protocol18Handler.MC_1_21_11_Version,
        Protocol18Handler.MC_26_1_Version,
        Protocol18Handler.MC_26_2_Version,
        Protocol18Handler.MC_26_3_Version,
    ];

    [Fact]
    public void BeforeOneTwentyOneSixEveryActionIsItsOwnPlayerCommand()
    {
        foreach (int protocol in OldProtocols)
        {
            foreach (EntityActionType action in Enum.GetValues<EntityActionType>())
            {
                Assert.Equal((int)action, Protocol18Handler.PlayerCommandAction(protocol, (int)action, out bool sneak));
                Assert.False(sneak);
            }
        }
    }

    [Fact]
    public void FromOneTwentyOneSixSneakingIsThePlayerInputKey()
    {
        foreach (int protocol in NewProtocols)
        {
            Assert.Null(Protocol18Handler.PlayerCommandAction(protocol, (int)EntityActionType.StartSneaking,
                out bool pressed));
            Assert.True(pressed);
            Assert.Null(Protocol18Handler.PlayerCommandAction(protocol, (int)EntityActionType.StopSneaking,
                out bool released));
            Assert.False(released);
        }
    }

    [Theory]
    [InlineData(EntityActionType.LeaveBed, 0)]
    [InlineData(EntityActionType.StartSprinting, 1)]
    [InlineData(EntityActionType.StopSprinting, 2)]
    [InlineData(EntityActionType.StartJumpWithHorse, 3)]
    [InlineData(EntityActionType.StopJumpWithHorse, 4)]
    [InlineData(EntityActionType.OpenHorseInventory, 5)]
    [InlineData(EntityActionType.StartFlyingWithElytra, 6)]
    public void FromOneTwentyOneSixTheOtherCommandsMovedDownByTwo(EntityActionType action, int id)
    {
        foreach (int protocol in NewProtocols)
        {
            Assert.Equal(id, Protocol18Handler.PlayerCommandAction(protocol, (int)action, out bool sneak));
            Assert.False(sneak);
        }
    }

    [Fact]
    public void TheSneakKeyIsThePlayerInputsSixthBit()
    {
        // forward 0x01, backward 0x02, left 0x04, right 0x08, jump 0x10, sneak 0x20, sprint 0x40
        Assert.Equal(0x20, Protocol18Handler.PlayerInputSneak);
    }
}
