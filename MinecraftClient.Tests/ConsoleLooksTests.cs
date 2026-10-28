using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.4: the looks of console lines (docs/agent-bridge.md, "Client 4.2.4: console line looks"): spans over the
/// plain text, the clicks the console may report and the hovers, within their caps, and nothing for text the walk does
/// not come to.
/// </summary>
[Collection(nameof(ConsoleKindsOutput))]
public sealed class ConsoleLooksTests : IDisposable
{
    private const string Nonce = "Nq3vX8LkPz2RwT5yHc7dJm9bFs4gA1eU";
    private const string Prefix = Nonce + "@@MCC@@";

    private static readonly JsonSerializerOptions Relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly List<string> lines = new();
    private readonly Action<string> previousOutput = ConsoleKinds.Output;
    private readonly string previousPrefix = ConsoleKinds.LinePrefix;

    public ConsoleLooksTests()
    {
        ConsoleKinds.Output = lines.Add;
        ConsoleKinds.LinePrefix = LauncherBridge.BuildLinePrefix(Nonce);
    }

    public void Dispose()
    {
        ConsoleKinds.Output = previousOutput;
        ConsoleKinds.LinePrefix = previousPrefix;
    }

    private static Dictionary<string, object> C(params (string Key, object Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value);

    private static Dictionary<string, object> T(string text, string? color = null) =>
        color is null ? C(("text", text)) : C(("text", text), ("color", color));

    /// <summary>A 1.20.3+ system line from its NBT component, as Protocol18 and OnTextReceived hand it on.</summary>
    private JsonObject SystemLine(object component)
    {
        string rendered = component is Dictionary<string, object> nbt
            ? ChatParser.ParseText(nbt)
            : ChatParser.ParseText((string)component);
        ChatMessage message = new(rendered, null, false, -1, Guid.Empty, true) { rawContent = component };
        ConsoleKinds.WriteChat(message, rendered, _ => null);
        return Single();
    }

    private JsonObject Single()
    {
        string line = Assert.Single(lines);
        lines.Clear();
        Assert.StartsWith(Prefix, line);
        Assert.DoesNotContain('\n', line);
        return JsonNode.Parse(line[Prefix.Length..])!.AsObject();
    }

    private static string? Spans(JsonObject line) => line["st"]?.ToJsonString(Relaxed);

    private static int Covered(JsonObject line) => line["st"]!.AsArray().Sum(span => (int)span![0]!);

    [Theory]
    [InlineData("%s says %s", 2)]
    [InlineData("%2$s then %1$s", 2)]
    [InlineData("%s %s %s", 2)]
    [InlineData("%1$s %s", 2)]
    [InlineData("100%", 2)]
    [InlineData("%", 2)]
    [InlineData("%%s", 2)]
    [InlineData("%d items", 1)]
    [InlineData("%9$s", 2)]
    [InlineData("%1$", 2)]
    [InlineData("no arguments %s", 0)]
    public void TranslationPiecesFillAsTranslateStringDoes(string rule, int count)
    {
        List<string> arguments = new[] { "Kaan_07", "FrostyOtter88" }.Take(count).ToList();
        List<ChatParser.TranslationPiece>? pieces = ChatParser.TranslationPieces(rule, arguments.Count);

        Assert.NotNull(pieces);
        string joined = string.Concat(pieces.Select(piece => piece.Text ?? arguments[piece.Argument]));
        Assert.Equal(ChatParser.TranslateString(rule, arguments), joined);
    }

    [Fact]
    public void APieceThatTranslateStringCannotFillHasNone()
    {
        Assert.Null(ChatParser.TranslationPieces("%0$s", 1));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => ChatParser.TranslateString("%0$s", ["x"]));
    }

    [Fact]
    public void ATranslationsArgumentsKeepTheirOwnLook()
    {
        JsonObject line = SystemLine(C(("translate", "multiplayer.player.joined"), ("color", "yellow"),
            ("with", new object[] { T("FrostyOtter88", "aqua") })));

        Assert.Equal("FrostyOtter88 joined the game", (string)line["text"]!);
        Assert.Equal("[[13,\"b\"],[16,\"e\"]]", Spans(line));
        Assert.Null(line["ev"]);
    }

    [Fact]
    public void AGradientNameIsASpanALetter()
    {
        string[] colors = ["#ff5500", "#ff7700", "#ff9900", "#ffbb00", "#ffdd00"];
        object[] letters = "Lavas".Select((letter, i) => (object)T(letter.ToString(), colors[i])).ToArray();
        JsonObject line = SystemLine(C(("text", "[VIP] "), ("color", "gold"), ("extra", letters.Append(T(": gg", "white")).ToArray())));

        Assert.Equal("[VIP] Lavas: gg", (string)line["text"]!);
        Assert.Equal("[[6,\"6\"],[1,\"#ff5500\"],[1,\"#ff7700\"],[1,\"#ff9900\"],[1,\"#ffbb00\"],[1,\"#ffdd00\"],[4,\"f\"]]",
            Spans(line));
    }

    [Fact]
    public void FormattingCodesInsideATextApply()
    {
        JsonObject line = SystemLine(T("§6[VIP] §fNetherKurt§7: selam §lherkese"));

        Assert.Equal("[VIP] NetherKurt: selam herkese", (string)line["text"]!);
        Assert.Equal("[[6,\"6\"],[10,\"f\"],[8,\"7\"],[7,\"7l\"]]", Spans(line));
    }

    [Fact]
    public void AdjacentPartsOfOneLookAreOneSpanAndAPlainLineHasNone()
    {
        JsonObject merged = SystemLine(C(("text", ""), ("extra", new object[] { T("Pixel", "red"), T("_Baykus", "red") })));
        Assert.Equal("[[12,\"c\"]]", Spans(merged));

        JsonObject plain = SystemLine(C(("text", "Sunucu yeniden başlıyor")));
        Assert.Null(plain["st"]);
        Assert.Null(plain["ev"]);
    }

    [Fact]
    public void ASignedLineIsPutTogetherWithItsDecoration()
    {
        Dictionary<string, object> sender = C(("text", "FrostyOtter88"), ("color", "aqua"),
            ("click_event", C(("action", "suggest_command"), ("command", "/tell FrostyOtter88 "))),
            ("hover_event", C(("action", "show_entity"), ("id", "minecraft:player"),
                ("uuid", new object[] { 1, 2, 3, 4 }), ("name", T("FrostyOtter88")))));
        ChatMessage message = new("selam herkese", false, -1, Guid.NewGuid(), null, ChatParser.ParseText(sender), null, 0,
            null, true)
        {
            chatTypeDecoration = new ChatParser.ChatTypeDecoration("commands.message.display.incoming",
                [ChatParser.ChatTypeParameter.Sender, ChatParser.ChatTypeParameter.Content],
                C(("color", "gray"), ("italic", (byte)1))),
            rawSender = sender,
        };
        string messageText = ChatParser.ParseSignedChat(message);

        ConsoleKinds.WriteChat(message, messageText, _ => "FrostyOtter88");
        JsonObject line = Single();

        Assert.Equal("FrostyOtter88 whispers to you: selam herkese", (string)line["text"]!);
        Assert.Equal("[[13,\"bo\",0],[31,\"7o\"]]", Spans(line));
        Assert.Equal(
            "[{\"click\":{\"a\":\"suggest_command\",\"v\":\"/tell FrostyOtter88 \"},\"hover\":{\"entity\":{\"id\":\"minecraft:player\",\"name\":\"FrostyOtter88\"}}}]",
            line["ev"]!.ToJsonString(Relaxed));
        Assert.DoesNotContain("uuid", line.ToJsonString(Relaxed));
    }

    [Fact]
    public void ASenderTheClientNamedItselfIsItsText()
    {
        // The server sent no name and the client took the tab list's: its text, no click and no hover.
        ChatMessage message = new("gg", false, -1, Guid.NewGuid(), null, "§eLavaPanda_7§r", null, 0, null, true)
        {
            chatTypeDecoration = new ChatParser.ChatTypeDecoration("chat.type.text",
                [ChatParser.ChatTypeParameter.Sender, ChatParser.ChatTypeParameter.Content]),
        };
        ConsoleKinds.WriteChat(message, ChatParser.ParseSignedChat(message), _ => null);
        JsonObject line = Single();

        Assert.Equal("<LavaPanda_7> gg", (string)line["text"]!);
        Assert.Equal("[[1,\"\"],[11,\"e\"],[4,\"\"]]", Spans(line));
        Assert.Null(line["ev"]);
    }

    [Fact]
    public void TheFourClicksAreReportedAndNoOther()
    {
        object[] parts =
        [
            C(("text", "[Kabul et]"), ("color", "green"), ("click_event", C(("action", "run_command"), ("command", "/party accept FrostyOtter88")))),
            C(("text", " [Yaz]"), ("clickEvent", C(("action", "suggest_command"), ("value", "/msg Mod_Kirpi ")))),
            C(("text", " [Kurallar]"), ("click_event", C(("action", "open_url"), ("url", "https://projects.gg/kurallar")))),
            C(("text", " [Kod]"), ("click_event", C(("action", "copy_to_clipboard"), ("value", "PRJ-5B2K")))),
            C(("text", " [Sayfa 2]"), ("click_event", C(("action", "change_page"), ("page", 2)))),
            C(("text", " [Pencere]"), ("click_event", C(("action", "show_dialog"), ("dialog", "projects:menu")))),
        ];
        JsonObject line = SystemLine(C(("text", ""), ("extra", parts)));

        JsonArray ev = line["ev"]!.AsArray();
        Assert.Equal(new[] { "run_command", "suggest_command", "open_url", "copy_to_clipboard" },
            ev.Select(e => (string)e!["click"]!["a"]!).ToArray());
        Assert.Equal("/party accept FrostyOtter88", (string)ev[0]!["click"]!["v"]!);
        Assert.Equal("[[10,\"a\",0],[6,\"\",1],[11,\"\",2],[6,\"\",3],[20,\"\"]]", Spans(line));
    }

    [Theory]
    [InlineData("open_url", "javascript:alert(1)")]
    [InlineData("open_url", "http://projects.gg")]
    [InlineData("open_url", "https://projects .gg")]
    [InlineData("open_url", "https://")]
    [InlineData("open_url", "HTTPS://projects.gg")]
    [InlineData("run_command", "party accept FrostyOtter88")]
    [InlineData("run_command", "/")]
    [InlineData("run_command", "/pay NetherKurt 100\u202E")]
    [InlineData("run_command", "/pay\nNetherKurt")]
    [InlineData("copy_to_clipboard", "\u2066gizli\u2069")]
    [InlineData("suggest_command", "")]
    [InlineData("custom", "projects:x")]
    public void AClickTheConsoleMustNotReportIsLeftOut(string action, string value)
    {
        // Before 1.21.5 (clickEvent) the game runs no command without its "/".
        JsonObject line = SystemLine(C(("text", "tıkla"), ("color", "red"), ("clickEvent", C(("action", action), ("value", value)))));

        Assert.Equal("[[5,\"c\"]]", Spans(line));
        Assert.Null(line["ev"]);
    }

    [Fact]
    public void ACommandOf1215MayLeaveOutItsSlash()
    {
        // 1.21.5+ (click_event): the game adds the "/" (Commands.trimOptionalPrefix), so the console reports it with one.
        JsonObject line = SystemLine(C(("text", "[Kabul et]"), ("color", "green"),
            ("click_event", C(("action", "run_command"), ("command", "party accept FrostyOtter88")))));

        Assert.Equal("/party accept FrostyOtter88", (string)line["ev"]![0]!["click"]!["v"]!);
    }

    [Fact]
    public void ALongClickValueIsNeverCut()
    {
        string command = "/" + new string('a', ConsoleLooks.MaxValueLength);
        JsonObject line = SystemLine(C(("text", "tıkla"), ("color", "red"), ("click_event", C(("action", "run_command"), ("command", command)))));
        Assert.Null(line["ev"]);

        string fits = "/" + new string('a', ConsoleLooks.MaxValueLength - 1);
        JsonObject kept = SystemLine(C(("text", "tıkla"), ("color", "red"), ("click_event", C(("action", "run_command"), ("command", fits)))));
        Assert.Equal(fits, (string)kept["ev"]![0]!["click"]!["v"]!);
    }

    [Fact]
    public void AChildsOwnClickHidesItsParents()
    {
        JsonObject line = SystemLine(C(("text", "[Kabul et]"), ("color", "green"),
            ("click_event", C(("action", "run_command"), ("command", "/party accept FrostyOtter88"))),
            ("extra", new object[] { C(("text", " [?]"), ("click_event", C(("action", "change_page"), ("page", 1)))) })));

        Assert.Equal("[[10,\"a\",0],[4,\"a\"]]", Spans(line));
        Assert.Single(line["ev"]!.AsArray());
    }

    [Fact]
    public void AHoverTextIsWalkedOnceWithoutItsOwnEvents()
    {
        // JSON (1.16-1.20.2): "contents"; a click and a hover inside the hover are not followed.
        string json = "{\"text\":\"[Kabul et]\",\"color\":\"green\",\"hoverEvent\":{\"action\":\"show_text\",\"contents\":"
                      + "{\"text\":\"Partiye katılmak için tıkla\",\"color\":\"green\",\"clickEvent\":{\"action\":\"run_command\",\"value\":\"/op FrostyOtter88\"},"
                      + "\"hoverEvent\":{\"action\":\"show_text\",\"contents\":\"iç içe\"},\"extra\":[{\"text\":\"\\nDavet 60 saniye\",\"color\":\"gray\"}]}}}";
        JsonObject line = SystemLine(json);

        Assert.Equal("[[10,\"a\",0]]", Spans(line));
        JsonNode hover = line["ev"]![0]!["hover"]!;
        Assert.Equal("Partiye katılmak için tıkla\nDavet 60 saniye", (string)hover["text"]!);
        Assert.Equal("[[27,\"a\"],[16,\"7\"]]", hover["st"]!.ToJsonString(Relaxed));
        Assert.Null(line["ev"]![0]!["click"]);
        Assert.DoesNotContain("/op", line.ToJsonString(Relaxed));
    }

    [Fact]
    public void AHoverTextIsCappedAndClean()
    {
        string many = string.Join("\r\n", Enumerable.Range(1, 12).Select(i => "satır " + i));
        JsonObject lines8 = SystemLine(C(("text", "?"), ("hover_event", C(("action", "show_text"), ("value", many)))));
        string text = (string)lines8["ev"]![0]!["hover"]!["text"]!;
        Assert.Equal(ConsoleLooks.MaxHoverLines, text.Split('\n').Length);
        Assert.EndsWith("satır 8…", text);

        string marks = "a\u202Eb\u2066c\u0007d\u200Fe";
        JsonObject clean = SystemLine(C(("text", "?"), ("hover_event", C(("action", "show_text"), ("value", marks)))));
        Assert.Equal("abcde", (string)clean["ev"]![0]!["hover"]!["text"]!);

        JsonObject longText = SystemLine(C(("text", "?"), ("hover_event", C(("action", "show_text"), ("value", new string('ğ', 400))))));
        string cut = (string)longText["ev"]![0]!["hover"]!["text"]!;
        Assert.Equal(ConsoleLooks.MaxHoverText, cut.Length);
        Assert.EndsWith("…", cut);

        // More than 16 looks: the text without them.
        object[] colorful = Enumerable.Range(0, 20).Select(i => (object)T("x", i % 2 == 0 ? "red" : "blue")).ToArray();
        JsonObject many16 = SystemLine(C(("text", "?"), ("hover_event", C(("action", "show_text"), ("value", C(("text", ""), ("extra", colorful)))))));
        Assert.Null(many16["ev"]![0]!["hover"]!["st"]);
        Assert.Equal(20, ((string)many16["ev"]![0]!["hover"]!["text"]!).Length);
    }

    [Fact]
    public void AnItemHoverIsTheBridgesItem()
    {
        Dictionary<string, object> components = C(
            ("minecraft:custom_name", T("Ejderha Kılıcı", "gold")),
            ("minecraft:lore", new object[] { T("Hasar +12", "gray"), T("") }),
            ("minecraft:enchantments", C(("levels", C(("minecraft:sharpness", 5), ("bad id", 3))))),
            ("minecraft:damage", 12),
            ("minecraft:custom_data", C(("secret", "x"))));
        JsonObject line = SystemLine(C(("text", "[Kılıç]"), ("color", "aqua"),
            ("hover_event", C(("action", "show_item"), ("id", "minecraft:diamond_sword"), ("count", 1), ("components", components)))));

        JsonNode item = line["ev"]![0]!["hover"]!["item"]!;
        Assert.Equal(
            "{\"id\":\"minecraft:diamond_sword\",\"count\":1,\"name\":\"Ejderha Kılıcı\",\"nameStyle\":[[14,\"6o\"]],"
            + "\"lore\":[\"Hasar +12\",\"\"],\"loreStyle\":[[[9,\"7o\"]],null],\"ench\":{\"minecraft:sharpness\":5},\"damage\":12}",
            item.ToJsonString(Relaxed));
    }

    [Fact]
    public void AHeadHoverCarriesItsSkinAndName()
    {
        const string hash = "8a6d5e1f0c3b4a29e7d6c5b4a3928170fe6d5c4b3a2918076f5e4d3c2b1a0998";
        string textures = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            "{\"textures\":{\"SKIN\":{\"url\":\"http://textures.minecraft.net/texture/" + hash + "\"}}}"));
        Dictionary<string, object> profile = C(("name", "FrostyOtter88"),
            ("properties", new object[] { C(("name", "textures"), ("value", textures)) }));
        JsonObject line = SystemLine(C(("text", "[Kafa]"),
            ("hover_event", C(("action", "show_item"), ("id", "minecraft:player_head"), ("count", 1),
                ("components", C(("minecraft:profile", profile)))))));

        JsonNode item = line["ev"]![0]!["hover"]!["item"]!;
        Assert.Equal(hash, (string)item["skin"]!);
        Assert.Equal("FrostyOtter88", (string)item["components"]!["minecraft:profile"]!["name"]!);
    }

    [Fact]
    public void AnOldItemHoverGivesItsIdAndCount()
    {
        string json = "{\"text\":\"[Elma]\",\"hoverEvent\":{\"action\":\"show_item\",\"contents\":{\"id\":\"apple\",\"count\":3,\"tag\":\"{display:{Name:'\\\"x\\\"'}}\"}}}";
        JsonObject line = SystemLine(json);
        Assert.Equal("{\"id\":\"minecraft:apple\",\"count\":3}", line["ev"]![0]!["hover"]!["item"]!.ToJsonString(Relaxed));

        // Before 1.16 the item was one SNBT text: no hover.
        string older = "{\"text\":\"[Elma]\",\"color\":\"red\",\"hoverEvent\":{\"action\":\"show_item\",\"value\":\"{id:apple,Count:1b}\"}}";
        Assert.Null(SystemLine(older)["ev"]);
    }

    [Fact]
    public void AnEntityHoverHasItsTypeAndNameButNeverItsId()
    {
        Dictionary<string, object> contents = C(("type", "minecraft:zombie"), ("id", new object[] { 7, 8, 9, 10 }),
            ("name", T("Kemik Ustası", "red")));
        JsonObject line = SystemLine(C(("text", "Kemik Ustası"), ("color", "red"),
            ("hoverEvent", C(("action", "show_entity"), ("contents", contents)))));

        Assert.Equal("{\"entity\":{\"id\":\"minecraft:zombie\",\"name\":\"Kemik Ustası\",\"st\":[[12,\"c\"]]}}",
            line["ev"]![0]!["hover"]!.ToJsonString(Relaxed));

        JsonObject unknown = SystemLine(C(("text", "?"), ("color", "red"),
            ("hover_event", C(("action", "show_entity"), ("id", "Not A Type")))));
        Assert.Null(unknown["ev"]);
    }

    [Fact]
    public void AtMostEightEventsTheRestWithout()
    {
        object[] parts = Enumerable.Range(1, 10)
            .Select(i => (object)C(("text", "[" + i + "]"), ("click_event", C(("action", "copy_to_clipboard"), ("value", "kod" + i)))))
            .ToArray();
        JsonObject line = SystemLine(C(("text", ""), ("color", "gold"), ("extra", parts)));

        Assert.Equal(ConsoleLooks.MaxEvents, line["ev"]!.AsArray().Count);
        JsonArray spans = line["st"]!.AsArray();
        Assert.Equal(7, (int)spans[7]![2]!);
        Assert.Equal(2, spans[^1]!.AsArray().Count); // "[9][10]": no event, one span
        Assert.Equal(((string)line["text"]!).Length, Covered(line));
    }

    [Fact]
    public void MoreThanSixtyFourSpansIsNoLooks()
    {
        object[] parts = Enumerable.Range(0, 65).Select(i => (object)T("x", i % 2 == 0 ? "red" : "blue")).ToArray();
        JsonObject line = SystemLine(C(("text", ""), ("extra", parts)));

        Assert.Equal(new string('x', 65), (string)line["text"]!);
        Assert.Null(line["st"]);
        Assert.Null(line["ev"]);
    }

    [Fact]
    public void PastTheBudgetHoversGoFirstThenEvents()
    {
        // Eight events, each a long hover with looks: too much, so the hovers go and the clicks stay.
        object[] parts = Enumerable.Range(0, 8).Select(i => (object)C(("text", "[" + i + "]"),
            ("click_event", C(("action", "copy_to_clipboard"), ("value", "kod" + i))),
            ("hover_event", C(("action", "show_text"), ("value", C(("text", ""), ("extra",
                Enumerable.Range(0, 16).Select(j => (object)T(new string('ş', 15), "#" + (j * 7 + i).ToString("x6"))).ToArray())))))))
            .ToArray();
        JsonObject line = SystemLine(C(("text", ""), ("extra", parts)));

        JsonArray ev = line["ev"]!.AsArray();
        Assert.Equal(8, ev.Count);
        Assert.All(ev, e => Assert.Null(e!["hover"]));
        Assert.True(Encoding.UTF8.GetByteCount(line["st"]!.ToJsonString(Relaxed) + ev.ToJsonString(Relaxed)) <= ConsoleLooks.MaxLooksBytes);

        // Eight clicks of long values in two-byte letters: too much even without hovers, so the events go.
        object[] heavy = Enumerable.Range(0, 8).Select(i => (object)C(("text", "[" + i + "]"), ("color", i % 2 == 0 ? "red" : "blue"),
            ("click_event", C(("action", "copy_to_clipboard"), ("value", i + new string('ğ', 255)))))).ToArray();
        JsonObject spansOnly = SystemLine(C(("text", ""), ("extra", heavy)));
        Assert.Null(spansOnly["ev"]);
        Assert.Equal(8, spansOnly["st"]!.AsArray().Count);
    }

    [Fact]
    public void HostileLinesStayLinesWithoutLooks()
    {
        // Ten thousand parts in two colours: written at once, without looks.
        object[] parts = Enumerable.Range(0, 10_000).Select(i => (object)T("x", i % 2 == 0 ? "red" : "blue")).ToArray();
        Stopwatch watch = Stopwatch.StartNew();
        JsonObject many = SystemLine(C(("text", ""), ("extra", parts)));
        Assert.True(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms");
        Assert.Null(many["st"]);

        // Ten thousand parts of one colour are one span.
        object[] same = Enumerable.Range(0, 10_000).Select(_ => (object)T("x", "red")).ToArray();
        Assert.Equal("[[8192,\"c\"]]", Spans(SystemLine(C(("text", ""), ("extra", same)))));

        // Nesting deeper than the walk goes: no looks from the components.
        Dictionary<string, object> deep = T("dip", "red");
        for (int i = 0; i < 100; i++)
            deep = C(("text", ""), ("extra", new object[] { deep }), ("click_event", C(("action", "run_command"), ("command", "/x"))));
        JsonObject nested = SystemLine(deep);
        Assert.Equal("dip", (string)nested["text"]!);
        Assert.Null(nested["ev"]);

        // A surrogate pair split between two parts: no looks rather than half a letter.
        JsonObject split = SystemLine(C(("text", ""), ("extra", new object[] { T("\uD83D", "red"), T("\uDE00", "blue") })));
        Assert.Equal("😀", (string)split["text"]!);
        Assert.Null(split["st"]);
    }

    [Fact]
    public void AWalkThatDoesNotComeToTheTextHasNoLooks()
    {
        Dictionary<string, object> component = T("Etkinliğe 5 dakika kaldı", "yellow");
        Assert.Null(ConsoleLooks.OfRaw(component, null, "Etkinliğe 6 dakika kaldı"));
        Assert.NotNull(ConsoleLooks.OfRaw(component, null, "Etkinliğe 5 dakika kaldı"));

        // A line's composed text differs from the client's: its own formatted text gives the looks (no events).
        ChatMessage message = new("§cbaşka", null, false, -1, Guid.Empty, true) { rawContent = component };
        ConsoleKinds.WriteChat(message, "§cbaşka", _ => null);
        JsonObject line = Single();
        Assert.Equal("[[5,\"c\"]]", Spans(line));
        Assert.Null(line["ev"]);
    }

    [Fact]
    public void ACutLineKeepsTheLooksOfWhatIsLeft()
    {
        JsonObject line = SystemLine(C(("text", ""), ("extra", new object[] { T(new string('a', 9000), "red"), T("son", "blue") })));
        string text = (string)line["text"]!;
        Assert.Equal(ConsoleKinds.MaxTextLength, text.Length);
        Assert.EndsWith("…", text);
        Assert.Equal("[[8192,\"c\"]]", Spans(line));
    }

    [Fact]
    public void TheClientsOwnLinesTitlesAndTheActionBarHaveLooks()
    {
        ConsoleKinds.WriteConsole("§8[MCC] §aBağlandı");
        Assert.Equal("[[6,\"8\"],[8,\"a\"]]", Spans(Single()));

        Dictionary<string, object> title = C(("text", "Hoş geldin!"), ("color", "gold"), ("bold", (byte)1));
        ConsoleKinds.WriteTitle(false, ChatParser.ParseText(title), title);
        Assert.Equal("[[11,\"6l\"]]", Spans(Single()));

        ConsoleKinds.WriteActionBar("§eGünlük ödüle §f01:59");
        Assert.Equal("[[13,\"e\"],[5,\"f\"]]", Spans(Single()));
    }

    [Fact]
    public void ATypicalChatLineAndItsBytes()
    {
        Dictionary<string, object> sender = C(("text", "FrostyOtter88"), ("color", "aqua"),
            ("click_event", C(("action", "suggest_command"), ("command", "/tell FrostyOtter88 "))),
            ("hover_event", C(("action", "show_entity"), ("id", "minecraft:player"), ("name", T("FrostyOtter88")))));
        ChatMessage message = new("lobiye geliyorum", false, -1, Guid.NewGuid(), null, ChatParser.ParseText(sender), null, 0,
            null, true)
        {
            chatTypeDecoration = new ChatParser.ChatTypeDecoration("chat.type.text",
                [ChatParser.ChatTypeParameter.Sender, ChatParser.ChatTypeParameter.Content]),
            rawSender = sender,
        };
        ConsoleKinds.WriteChat(message, ChatParser.ParseSignedChat(message), _ => "FrostyOtter88");
        string written = Assert.Single(lines)[Prefix.Length..];

        Assert.Equal(
            "{\"e\":\"line\",\"src\":\"player\",\"text\":\"<FrostyOtter88> lobiye geliyorum\",\"who\":\"FrostyOtter88\","
            + "\"st\":[[1,\"\"],[13,\"b\",0],[18,\"\"]],"
            + "\"ev\":[{\"click\":{\"a\":\"suggest_command\",\"v\":\"/tell FrostyOtter88 \"},\"hover\":{\"entity\":{\"id\":\"minecraft:player\",\"name\":\"FrostyOtter88\"}}}]}",
            written);
    }

    [Fact]
    public void WithTheKindsOffNothingIsKept()
    {
        bool previous = ConsoleKinds.Enabled;
        try
        {
            object raw = T("x", "red");
            ConsoleKinds.Enabled = false;
            Assert.Null(ConsoleKinds.Keep(raw));
            ConsoleKinds.Enabled = true;
            Assert.Same(raw, ConsoleKinds.Keep(raw));
        }
        finally
        {
            ConsoleKinds.Enabled = previous;
        }
    }
}
