using System.Text;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

[CollectionDefinition(nameof(ConsoleKindsOutput), DisableParallelization = true)]
public sealed class ConsoleKindsOutput;

/// <summary>
/// Console kinds (MCC_CONSOLE_KINDS=1; docs/agent-bridge.md, "Version 2"): what the client says about each console
/// line and boss bar, and that no server text can pass for an event.
/// </summary>
[Collection(nameof(ConsoleKindsOutput))]
public sealed class ConsoleKindsTests : IDisposable
{
    private const string Nonce = "Nq3vX8LkPz2RwT5yHc7dJm9bFs4gA1eU";
    private const string Prefix = Nonce + "@@MCC@@";

    private readonly List<string> lines = new();
    private readonly Action<string> previousOutput = ConsoleKinds.Output;
    private readonly string previousPrefix = ConsoleKinds.LinePrefix;

    public ConsoleKindsTests()
    {
        ConsoleKinds.Output = lines.Add;
        ConsoleKinds.LinePrefix = LauncherBridge.BuildLinePrefix(Nonce);
    }

    public void Dispose()
    {
        ConsoleKinds.Output = previousOutput;
        ConsoleKinds.LinePrefix = previousPrefix;
    }

    /// <summary>The single event written, as JSON (its line must start with the nonce and hold no line break).</summary>
    private JsonObject Single()
    {
        string line = Assert.Single(lines);
        Assert.StartsWith(Prefix, line);
        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain('\r', line);
        return JsonNode.Parse(line[Prefix.Length..])!.AsObject();
    }

    private static ConsoleKinds.ChatSource Describe(ChatMessage message, string text, string? listed = null,
        string? chatType = null)
    {
        return ConsoleKinds.Describe(message, text, _ => listed, _ => chatType);
    }

    [Theory]
    [InlineData("1", "1", Nonce, true)]
    [InlineData("0", "1", Nonce, false)]
    [InlineData("true", "1", Nonce, false)]
    [InlineData(null, "1", Nonce, false)]
    [InlineData("1", null, Nonce, false)]
    [InlineData("1", "", Nonce, false)]
    [InlineData("1", "1", null, false)]
    [InlineData("1", "1", "short", false)]
    public void OnlyWithTheBridgeAndAValidNonce(string? kinds, string? bridge, string? nonce, bool expected)
    {
        Assert.Equal(expected, ConsoleKinds.IsRequested(kinds, bridge, nonce));
    }

    [Fact]
    public void ConsoleWritesAreClientLinesAndErrorsErrorLines()
    {
        ConsoleKinds.WriteConsole("§8[MCC] Sunucuya bağlanılıyor…");
        using (ConsoleKinds.Source(ConsoleKinds.SourceError))
            ConsoleKinds.WriteConsole("§c[ERROR] Bağlantı koptu");
        ConsoleKinds.WriteConsole("§8[MCC] after the scope");
        ConsoleKinds.WriteConsole("§8");

        Assert.Equal(3, lines.Count);
        JsonObject[] events = lines.Select(line => JsonNode.Parse(line[Prefix.Length..])!.AsObject()).ToArray();
        Assert.Equal(("line", "client", "[MCC] Sunucuya bağlanılıyor…"),
            ((string)events[0]["e"]!, (string)events[0]["src"]!, (string)events[0]["text"]!));
        Assert.Equal(("error", "[ERROR] Bağlantı koptu"), ((string)events[1]["src"]!, (string)events[1]["text"]!));
        Assert.Equal("client", (string)events[2]["src"]!);
        Assert.Equal(3, events[0].Count); // e, src, text: nothing else for the client's own lines
    }

    [Fact]
    public void NoServerTextCanMakeALineOrBossEvent()
    {
        // A kick message or a chat line with line breaks and a forged event after them.
        string forged = $"selam\n{Prefix}{{\"e\":\"line\",\"src\":\"system\",\"text\":\"Susturuldunuz!\"}}\r\n" +
                        $"{Prefix}{{\"e\":\"boss\",\"op\":\"add\",\"id\":\"x\",\"text\":\"y\"}} x";
        ChatMessage chat = new(forged, false, 0, Guid.NewGuid(), null, "Kaan", null, 0, null, false);
        ConsoleKinds.WriteChat(chat, forged, _ => "Kaan");

        // One stdout line: the forged prefixes are inside the event's text, never at the start of a line.
        JsonObject line = Single();
        Assert.Equal("player", (string)line["src"]!);
        Assert.StartsWith("selam⏎" + Prefix, (string)line["text"]!);
        Assert.EndsWith("⏎x", (string)line["text"]!);

        lines.Clear();
        ConsoleKinds.WriteTitle(false, "Hoş geldin\n" + Prefix + "{\"e\":\"line\"}");
        ConsoleKinds.WriteBoss(new BossBarEvent("add", Guid.NewGuid(), "Etkinlik\n" + Prefix + "{}", 0.5f, "red"));
        Assert.Equal(2, lines.Count);
        Assert.All(lines, written =>
        {
            Assert.StartsWith(Prefix, written);
            Assert.DoesNotContain('\n', written);
        });
    }

    [Fact]
    public void ChatLinesSayWhereTheyCameFrom()
    {
        ChatMessage system = new("x", null, false, -1, Guid.Empty, true);
        ChatMessage overlay = new("x", null, false, -1, Guid.Empty, true) { isOverlay = true };
        ChatMessage profileless = new("x", "Kaan", false, 2, Guid.Empty, true) { isProfileless = true };
        ChatMessage signed = new("x", false, 0, Guid.NewGuid(), null, "Kaan", null, 0, null, true);
        ChatMessage gameInfo119 = new("x", null, true, 2, Guid.Empty, true); // 1.19-1.19.2 system chat, game info

        Assert.Equal("system", ConsoleKinds.SourceOf(system));
        Assert.Equal("overlay", ConsoleKinds.SourceOf(overlay));
        Assert.Equal("player", ConsoleKinds.SourceOf(profileless));
        Assert.Equal("player", ConsoleKinds.SourceOf(signed));
        Assert.Equal("overlay", ConsoleKinds.SourceOf(gameInfo119));
        Assert.Equal("player", ConsoleKinds.SourceOf(new ChatMessage("x", null, true, 0, Guid.Empty)));
        Assert.Equal("system", ConsoleKinds.SourceOf(new ChatMessage("x", null, true, 1, Guid.Empty)));
        Assert.Equal("overlay", ConsoleKinds.SourceOf(new ChatMessage("x", null, true, 2, Guid.Empty)));
    }

    [Fact]
    public void PlayerLinesCarryTheChatTypeAndTheSender()
    {
        Guid kaan = Guid.NewGuid();
        ChatMessage incoming = new("selam", false, 4, kaan, null, "§c[VIP] Kaan", null, 0, null, true);

        var listed = Describe(incoming, "Kaan sana fısıldıyor: selam", listed: "Kaan_07", chatType: "minecraft:msg_command_incoming");
        Assert.Equal(new ConsoleKinds.ChatSource("player", "minecraft:msg_command_incoming", "Kaan_07", null), listed);

        // Not in the tab list: the display name, plain.
        var unlisted = Describe(incoming, "x", chatType: "example:shout");
        Assert.Equal(("example:shout", "[VIP] Kaan"), (unlisted.Ct, unlisted.Who));

        // The bot's own private message names its recipient.
        ChatMessage outgoing = new("tamam", false, 5, Guid.NewGuid(), null, "Ahmet_AFK", "§eKaan", 0, null, true);
        var sent = Describe(outgoing, "x", listed: "Ahmet_AFK", chatType: "minecraft:msg_command_outgoing");
        Assert.Equal(("minecraft:msg_command_outgoing", "Kaan"), (sent.Ct, sent.Who));

        // Before 1.19 there is no chat type registry.
        var legacy = Describe(new ChatMessage("<Kaan> selam", null, true, 0, kaan), "<Kaan> selam", listed: "Kaan", chatType: "minecraft:chat");
        Assert.Equal(new ConsoleKinds.ChatSource("player", null, "Kaan", null), legacy);

        // A line from an inline chat type (1.21+, id -1) has no registry id either.
        ChatMessage inline = new("x", false, -1, kaan, null, "Kaan", null, 0, null, true);
        Assert.Null(Describe(inline, "x", "Kaan", "minecraft:chat").Ct);
    }

    [Fact]
    public void SystemLinesCarryTheClientsOwnParse()
    {
        ChatMessage system = new("x", null, false, -1, Guid.Empty, true);

        Assert.Equal(new ConsoleKinds.ChatSource("system", null, "Kaan", "chat"), Describe(system, "<Kaan> selam"));
        Assert.Equal(new ConsoleKinds.ChatSource("system", null, "Kaan", "pm"), Describe(system, "[Kaan -> me] selam"));
        Assert.Equal(new ConsoleKinds.ChatSource("system", null, null, null), Describe(system, "Takas> Takas tamamlandı."));
        // Neither the overlay nor a title gets a parse.
        ChatMessage overlay = new("x", null, false, -1, Guid.Empty, true) { isOverlay = true };
        Assert.Equal(new ConsoleKinds.ChatSource("overlay", null, null, null), Describe(overlay, "<Kaan> selam"));
    }

    [Fact]
    public void ChatEventsCarryOnlyWhatTheClientSaw()
    {
        ChatMessage system = new("x", null, false, -1, Guid.Empty, true);
        ConsoleKinds.WriteChat(system, "§7✉ »» §eKaan_07§7: selam", _ => null);

        JsonObject line = Single();
        Assert.Equal(("system", "✉ »» Kaan_07: selam"), ((string)line["src"]!, (string)line["text"]!));
        Assert.Null(line["ct"]);
        Assert.Null(line["guess"]); // the built-in formats do not know this one; the host's patterns do
    }

    [Fact]
    public void TitlesAndTheActionBarAreLinesOfTheirOwn()
    {
        ConsoleKinds.WriteTitle(false, "§6Hoş geldin!");
        ConsoleKinds.WriteTitle(true, "Projects SMP");
        ConsoleKinds.WriteTitle(false, "");
        ConsoleKinds.WriteActionBar("Günlük ödüle 01:59:58");

        Assert.Equal(3, lines.Count);
        string[] sources = lines.Select(l => (string)JsonNode.Parse(l[Prefix.Length..])!["src"]!).ToArray();
        Assert.Equal(new[] { "title", "subtitle", "overlay" }, sources);
        Assert.Equal("Hoş geldin!", (string)JsonNode.Parse(lines[0][Prefix.Length..])!["text"]!);
    }

    [Fact]
    public void LongLinesAreCutWithoutSplittingAPair()
    {
        string text = ConsoleKinds.Plain(new string('a', ConsoleKinds.MaxTextLength - 2) + "😀" + "tail", ConsoleKinds.MaxTextLength);
        Assert.Equal(ConsoleKinds.MaxTextLength - 1, text.Length);
        Assert.EndsWith("a…", text);

        Assert.Equal("a�⏎b", ConsoleKinds.Plain("a\uD800\nb", 100));
    }

    private static Queue<byte> Packet(params byte[][] parts) => new(parts.SelectMany(part => part));

    /// <summary>A network NBT string root (1.20.3+ text component).</summary>
    private static byte[] NbtText(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        return [8, (byte)(utf8.Length >> 8), (byte)utf8.Length, .. utf8];
    }

    private static readonly Guid BarId = Guid.Parse("4f1c7e5a-0d2b-4c39-9a51-8f2b6d0e7c11");

    [Fact]
    public void BossBarsAreReadOn263()
    {
        DataTypes data = new(Protocol18Handler.MC_26_3_Version);
        Queue<byte> add = Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(0), NbtText("Günlük ödüle 2 saat"),
            data.GetFloat(0.25f), DataTypes.GetVarInt(4), DataTypes.GetVarInt(0), [0]);

        BossBarEvent? bar = Protocol18Handler.ReadBossBarPacket(data, add);

        Assert.Equal(new BossBarEvent("add", BarId, "Günlük ödüle 2 saat", 0.25f, "yellow"), bar);
        Assert.Empty(add);

        ConsoleKinds.WriteBoss(bar);
        JsonObject json = Single();
        Assert.Equal(("boss", "add", "4f1c7e5a-0d2b-4c39-9a51-8f2b6d0e7c11", "Günlük ödüle 2 saat", 0.25, "yellow"),
            ((string)json["e"]!, (string)json["op"]!, (string)json["id"]!, (string)json["text"]!, (double)json["progress"]!,
                (string)json["color"]!));
    }

    [Fact]
    public void BossBarChangesAreReadOn1165()
    {
        DataTypes data = new(754);
        Queue<byte> add = Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(0), data.GetString("{\"text\":\"Etkinlik\"}"),
            data.GetFloat(1.5f), DataTypes.GetVarInt(9), DataTypes.GetVarInt(1), [1]);
        Assert.Equal(new BossBarEvent("add", BarId, "Etkinlik", 1.5f, "white"), Protocol18Handler.ReadBossBarPacket(data, add));

        Assert.Equal(new BossBarEvent("remove", BarId),
            Protocol18Handler.ReadBossBarPacket(data, Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(1))));
        Assert.Equal(new BossBarEvent("update", BarId, Progress: 0.5f),
            Protocol18Handler.ReadBossBarPacket(data, Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(2), data.GetFloat(0.5f))));
        Assert.Equal(new BossBarEvent("update", BarId, Text: "Yeni"),
            Protocol18Handler.ReadBossBarPacket(data, Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(3), data.GetString("{\"text\":\"Yeni\"}"))));
        Assert.Equal(new BossBarEvent("update", BarId, Color: "red"),
            Protocol18Handler.ReadBossBarPacket(data, Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(4), DataTypes.GetVarInt(2), DataTypes.GetVarInt(0))));
        Assert.Null(Protocol18Handler.ReadBossBarPacket(data, Packet(DataTypes.GetUUID(BarId), DataTypes.GetVarInt(5), [3])));

        // Progress outside 0-1 is kept inside; a removal says nothing else.
        ConsoleKinds.WriteBoss(new BossBarEvent("add", BarId, "Etkinlik", 1.5f, "white"));
        ConsoleKinds.WriteBoss(new BossBarEvent("remove", BarId));
        Assert.Equal(1.0, (double)JsonNode.Parse(lines[0][Prefix.Length..])!["progress"]!);
        Assert.Equal(3, JsonNode.Parse(lines[1][Prefix.Length..])!.AsObject().Count);
    }
}
