using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

/// <summary>
/// Side events (MCC_CONSOLE_SIDE; docs/agent-bridge.md, "Client 4.2.5: side events"): the classic console prints
/// exactly what it printed before, and a chat line's looks go right before it only when the printed line is their text.
/// </summary>
[Collection(nameof(ConsoleKindsOutput))]
public sealed class ConsoleSideTests : IDisposable
{
    private const string Nonce = "Nq3vX8LkPz2RwT5yHc7dJm9bFs4gA1eU";
    private const string Prefix = Nonce + "@@MCC@@";

    private readonly List<string> lines = new();
    private readonly Action<string> previousOutput = ConsoleKinds.Output;
    private readonly string previousPrefix = ConsoleKinds.LinePrefix;
    private readonly bool previousKinds = ConsoleKinds.Enabled;
    private readonly bool previousLooks = ConsoleSide.Looks;
    private readonly bool previousDialogs = ConsoleSide.Dialogs;
    private readonly bool previousBasic = ConsoleIO.BasicIO;
    private readonly bool previousNoColor = ConsoleIO.BasicIO_NoColor;
    private readonly bool previousOneLine = ConsoleIO.OneLinePerWrite;
    private readonly bool previousTimestamps = ConsoleIO.EnableTimestamps;
    private readonly bool previousVisible = ConsoleIO.ChatVisible;
    private readonly TextWriter previousOut = Console.Out;

    public ConsoleSideTests()
    {
        ConsoleKinds.Output = lines.Add;
        ConsoleKinds.LinePrefix = LauncherBridge.BuildLinePrefix(Nonce);
        ConsoleKinds.Enabled = false;
        ConsoleIO.BasicIO = true;
        ConsoleIO.BasicIO_NoColor = true;
        ConsoleIO.OneLinePerWrite = true;
        ConsoleIO.EnableTimestamps = false;
        ConsoleIO.ChatVisible = true;
    }

    public void Dispose()
    {
        ConsoleKinds.Output = previousOutput;
        ConsoleKinds.LinePrefix = previousPrefix;
        ConsoleKinds.Enabled = previousKinds;
        ConsoleSide.Looks = previousLooks;
        ConsoleSide.Dialogs = previousDialogs;
        ConsoleIO.BasicIO = previousBasic;
        ConsoleIO.BasicIO_NoColor = previousNoColor;
        ConsoleIO.OneLinePerWrite = previousOneLine;
        ConsoleIO.EnableTimestamps = previousTimestamps;
        ConsoleIO.ChatVisible = previousVisible;
        Console.SetOut(previousOut);
    }

    private static Dictionary<string, object> C(params (string Key, object Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value);

    /// <summary>A coloured system line with a hover, as Protocol18 and OnTextReceived hand it on.</summary>
    private static (ChatMessage Message, string Rendered) ColouredLine()
    {
        Dictionary<string, object> component = C(
            ("text", ""),
            ("extra", new object[]
            {
                C(("text", "[Duyuru] "), ("color", "gold")),
                C(("text", "Etkinlik başlıyor"), ("color", "green"), ("bold", (byte)1),
                    ("hover_event", C(("action", "show_text"), ("value", C(("text", "Spawn'a gel")))))),
            }));
        string rendered = ChatParser.ParseText(component);
        return (new ChatMessage(rendered, null, false, -1, Guid.Empty, true) { rawContent = component }, rendered);
    }

    /// <summary>What the console prints for <paramref name="print"/> (one call, as Log.Chat makes it).</summary>
    private static string Printed(Action print)
    {
        StringWriter writer = new();
        TextWriter before = Console.Out;
        Console.SetOut(writer);
        try
        {
            print();
        }
        finally
        {
            Console.SetOut(before);
        }

        return writer.ToString();
    }

    [Theory]
    [InlineData("looks", "1", Nonce, true, false)]
    [InlineData("dialogs", "1", Nonce, false, true)]
    [InlineData("looks,dialogs", "1", Nonce, true, true)]
    [InlineData(" looks , dialogs ,colours", "1", Nonce, true, true)]
    [InlineData("kinds", "1", Nonce, false, false)]
    [InlineData(null, "1", Nonce, false, false)]
    [InlineData("looks", null, Nonce, false, false)]
    [InlineData("looks", "", Nonce, false, false)]
    [InlineData("looks", "1", null, false, false)]
    [InlineData("looks", "1", "short", false, false)]
    public void OnlyWithTheBridgeAndAValidNonce(string? side, string? bridge, string? nonce, bool looks, bool dialogs)
    {
        Assert.Equal((looks, dialogs), ConsoleSide.Parse(side, bridge, nonce));
    }

    [Fact]
    public void TheClassicPrintStaysByteForByteAndTheLooksGoFirst()
    {
        (ChatMessage message, string rendered) = ColouredLine();

        ConsoleSide.Looks = false;
        string without = Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered))
                ConsoleIO.WriteChatLineIfVisible(rendered);
        });
        Assert.Empty(lines);

        ConsoleSide.Looks = true;
        string with = Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered))
                ConsoleIO.WriteChatLineIfVisible(rendered);
        });

        Assert.Equal(without, with);
        string line = Assert.Single(lines);
        Assert.StartsWith(Prefix, line);
        Assert.DoesNotContain('\n', line);
        JsonObject looks = JsonNode.Parse(line[Prefix.Length..])!.AsObject();
        Assert.Equal("looks", (string?)looks["e"]);
        Assert.Equal("[Duyuru] Etkinlik başlıyor", (string?)looks["text"]);
        Assert.Equal(without.TrimEnd('\r', '\n'), (string?)looks["text"]);
        Assert.NotNull(looks["st"]);
    }

    [Fact]
    public void TheClientSMarkIsPartOfTheLineItsLooksDescribe()
    {
        ConsoleSide.Looks = true;
        (ChatMessage message, string rendered) = ColouredLine();

        // The client's signature mark is printed with the line (a system line's, grey): the looks cover it, first.
        string printed = Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered, "§7▌§r"))
                ConsoleIO.WriteChatLineIfVisible("§7▌§r" + rendered);
        });
        string line = Assert.Single(lines);
        JsonObject looks = JsonNode.Parse(line[Prefix.Length..])!.AsObject();
        Assert.Equal("▌[Duyuru] Etkinlik başlıyor", (string?)looks["text"]);
        Assert.Equal(printed.TrimEnd('\r', '\n'), (string?)looks["text"]);
        Assert.StartsWith("[[1,\"7\"],", looks["st"]!.ToJsonString());
        lines.Clear();

        // A mark the line does not carry: no looks.
        Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered, "§7▌§r"))
                ConsoleIO.WriteChatLineIfVisible(rendered);
        });
        Assert.Empty(lines);
    }

    [Fact]
    public void ATimestampedOrUnprintedLineGoesWithoutLooks()
    {
        ConsoleSide.Looks = true;
        (ChatMessage message, string rendered) = ColouredLine();

        // A signature mark the looks were not told of changes the printed line: no looks.
        Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered))
                ConsoleIO.WriteChatLineIfVisible("§7▌§r" + rendered);
        });
        Assert.Empty(lines);

        // A timestamp too.
        ConsoleIO.EnableTimestamps = true;
        Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered))
                ConsoleIO.WriteChatLineIfVisible(rendered);
        });
        ConsoleIO.EnableTimestamps = false;
        Assert.Empty(lines);

        // Hidden chat prints nothing, and the waiting looks never ride on the next line.
        ConsoleIO.ChatVisible = false;
        Printed(() =>
        {
            using (ConsoleSide.Expect(message, rendered))
                ConsoleIO.WriteChatLineIfVisible(rendered);
        });
        ConsoleIO.ChatVisible = true;
        Printed(() => ConsoleIO.WriteChatLineIfVisible(rendered));
        Assert.Empty(lines);
    }

    [Fact]
    public void ConsoleKindsTurnTheSideEventsOff()
    {
        ConsoleSide.Looks = true;
        ConsoleKinds.Enabled = true;
        (ChatMessage message, string rendered) = ColouredLine();
        using (ConsoleSide.Expect(message, rendered))
            ConsoleSide.BeforePrint(ConsoleKinds.Plain(rendered, ConsoleKinds.MaxTextLength));
        Assert.Empty(lines);
    }

    [Fact]
    public void DialogsAreReportedWithKindsOrTheSideEvents()
    {
        ConsoleSide.Dialogs = false;
        ConsoleKinds.Enabled = false;
        Assert.False(ConsoleDialogs.Enabled);
        ConsoleSide.Dialogs = true;
        Assert.True(ConsoleDialogs.Enabled);
        ConsoleSide.Dialogs = false;
        ConsoleKinds.Enabled = true;
        Assert.True(ConsoleDialogs.Enabled);
    }
}
