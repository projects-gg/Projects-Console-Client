using MinecraftClient.Protocol;

namespace MinecraftClient.Tests;

/// <summary>
/// The version table: a release's name gives its protocol without a version ping. Every 26.1 release (26.1.1 and
/// 26.1.2 too) speaks protocol 775.
/// </summary>
public sealed class ProtocolVersionTests
{
    [Theory]
    [InlineData("26.1", 775)]
    [InlineData("26.1.1", 775)]
    [InlineData("26.1.2", 775)]
    [InlineData("26.2", 776)]
    [InlineData("26.3", 777)]
    [InlineData("26.1.9", 0)]
    public void AReleaseNameGivesItsProtocol(string version, int protocol)
    {
        Assert.Equal(protocol, ProtocolHandler.MCVer2ProtocolVersion(version));
    }

    [Fact]
    public void Protocol775ReadsAs26Point1()
    {
        Assert.Equal("26.1", ProtocolHandler.ProtocolVersion2MCVer(775));
    }
}
