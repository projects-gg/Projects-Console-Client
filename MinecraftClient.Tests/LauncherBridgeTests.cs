using MinecraftClient.ChatBots;

namespace MinecraftClient.Tests;

public sealed class LauncherBridgeHardeningTests
{
    [Theory]
    [InlineData("a\nb", "a⏎b")]
    [InlineData("a\r\nb", "a⏎b")]
    [InlineData("a\rb", "a⏎b")]
    [InlineData("a\u2028b\u2029c\u0085d", "a⏎b⏎c⏎d")]
    [InlineData("a\u000Bb\u000Cc", "a⏎b⏎c")]
    [InlineData("tek satır", "tek satır")]
    public void LineBreaksBecomeOneVisibleMark(string input, string expected)
    {
        Assert.Equal(expected, ConsoleIO.KeepOnOneLine(input));
    }

    [Fact]
    public void BridgeLinesCarryAValidNonce()
    {
        Assert.Equal("abcdefghijklmnop_-12@@MCC@@", LauncherBridge.BuildLinePrefix("abcdefghijklmnop_-12"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tooshort")]
    [InlineData("has space in it 12345")]
    [InlineData("newline\nnewline12345")]
    public void InvalidNoncesAreIgnored(string? nonce)
    {
        Assert.Equal(LauncherBridge.Prefix, LauncherBridge.BuildLinePrefix(nonce));
    }
}
