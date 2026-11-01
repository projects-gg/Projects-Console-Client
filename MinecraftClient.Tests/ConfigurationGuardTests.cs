using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.7: no play packet goes while the server configures the client (a proxy's server switch after Start
/// Configuration, or the configuration before the first join): the server would read it as another packet and the proxy
/// drops the connection. The configuration's acknowledgement is the one play packet that goes then: it ends play.
/// </summary>
public sealed class ConfigurationGuardTests
{
    [Theory]
    [InlineData(PacketTypesOut.ChatCommand)]
    [InlineData(PacketTypesOut.ChatMessage)]
    [InlineData(PacketTypesOut.MessageAcknowledgment)]
    [InlineData(PacketTypesOut.TeleportConfirm)]
    [InlineData(PacketTypesOut.SteerVehicle)]
    [InlineData(PacketTypesOut.EntityAction)]
    [InlineData(PacketTypesOut.ClientStatus)]
    [InlineData(PacketTypesOut.PluginMessage)]
    public void NoPlayPacketGoesWhileTheServerConfiguresTheClient(PacketTypesOut packet)
    {
        Assert.False(Protocol18Handler.PlayPacketGoes(CurrentState.Configuration, packet));
        Assert.False(Protocol18Handler.PlayPacketGoes(CurrentState.Login, packet));
        Assert.True(Protocol18Handler.PlayPacketGoes(CurrentState.Play, packet));
    }

    [Fact]
    public void TheAcknowledgementIsThePlayPacketThatEndsPlay()
    {
        Assert.True(Protocol18Handler.PlayPacketGoes(CurrentState.Configuration, PacketTypesOut.AcknowledgeConfiguration));
        Assert.True(Protocol18Handler.PlayPacketGoes(CurrentState.Play, PacketTypesOut.AcknowledgeConfiguration));
    }
}
