using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

public sealed class AgentBridgeClickTests
{
    private static Dictionary<string, object> Text(string text, Dictionary<string, object>? click = null,
        params object[] extra)
    {
        Dictionary<string, object> component = new() { ["text"] = text };
        if (click is not null)
            component["click_event"] = click;
        if (extra.Length > 0)
            component["extra"] = extra;
        return component;
    }

    [Fact]
    public void ModernNbtClickOnTheRootIsFound()
    {
        // The shape Skript's "formatted" gives a pm.sk line: a root carrying the click, coloured children.
        var pm = Text("", new() { ["action"] = "suggest_command", ["command"] = "/m Karsi " },
            Text("✉ »» Karsi: "), Text("selam"));

        List<ChatClick> clicks = new();
        ChatClicks.Collect(pm, clicks);

        Assert.Equal(new[] { new ChatClick("suggest_command", "/m Karsi ") }, clicks);
    }

    [Fact]
    public void LegacyNbtClicksInExtraAndWithAreFoundInOrderWithoutDuplicates()
    {
        Dictionary<string, object> run = new() { ["action"] = "run_command", ["value"] = "/tpa Karsi" };
        Dictionary<string, object> name = new() { ["text"] = "Karsi", ["clickEvent"] = run };
        Dictionary<string, object> again = new() { ["text"] = " » ", ["clickEvent"] = run };
        Dictionary<string, object> url = new()
        {
            ["translate"] = "chat.link",
            ["with"] = new object[] { new Dictionary<string, object>
            {
                ["text"] = "site", ["clickEvent"] = new Dictionary<string, object> { ["action"] = "open_url", ["value"] = "https://projects.gg" },
            } },
        };
        var line = Text("", null, name, again, url, "plain string child");

        List<ChatClick> clicks = new();
        ChatClicks.Collect(line, clicks);

        Assert.Equal(new[] { new ChatClick("run_command", "/tpa Karsi"), new ChatClick("open_url", "https://projects.gg") }, clicks);
    }

    [Fact]
    public void JsonClicksOfBothFormatsAreFound()
    {
        const string json = """
            {"text":"","extra":[{"text":"a","clickEvent":{"action":"suggest_command","value":"/m A "}},
            {"text":"b","click_event":{"action":"change_page","page":3}}]}
            """;
        List<ChatClick> clicks = new();
        ChatClicks.CollectJson(json, clicks);

        Assert.Equal(new[] { new ChatClick("suggest_command", "/m A "), new ChatClick("change_page", "3") }, clicks);
    }

    [Fact]
    public void PlainTextAndBrokenJsonHaveNoClicks()
    {
        List<ChatClick> clicks = new();
        ChatClicks.CollectJson("✉ »» Karsi: {not json", clicks);
        ChatClicks.CollectJson("{\"text\":", clicks);
        Assert.Empty(clicks);
    }

    [Fact]
    public void DuplicateJsonKeysNeverThrow()
    {
        // System.Text.Json reports duplicate keys only when the object is first read.
        List<ChatClick> clicks = new();
        ChatClicks.CollectJson("""{"text":"a","clickEvent":{"action":"run_command","value":"/x"},"clickEvent":{}}""", clicks);
        ChatClicks.CollectJson("""[{"text":"a","text":"b"}]""", clicks);
        Assert.True(clicks.Count <= 1);
    }

    [Fact]
    public void ClicksAreCappedAndLongValuesCut()
    {
        object[] children = Enumerable.Range(0, 40)
            .Select(i => (object)Text("x", new() { ["action"] = "run_command", ["command"] = $"/c {i} " + new string('a', 400) }))
            .ToArray();
        List<ChatClick> clicks = new();
        ChatClicks.Collect(Text("", null, children), clicks);

        Assert.Equal(ChatClicks.MaxClicks, clicks.Count);
        Assert.All(clicks, click => Assert.True(click.Value.Length <= ChatClicks.MaxValueLength));
    }
}

public sealed class AgentOperationTests
{
    private static AgentOperation Parse(string json)
    {
        Assert.True(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 7, out AgentOperation? operation,
            out _, out _, out string? reason), reason);
        return operation!;
    }

    private static string Refuse(string json)
    {
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 1, out _, out _, out _, out string? reason));
        return reason!;
    }

    [Fact]
    public void ClickIsParsed()
    {
        AgentOperation click = Parse("""{"op":"click","id":"a-1","win":3,"ws":42,"slot":12,"button":0,"mode":"pickup"}""");
        Assert.Equal(("click", "a-1", 3, 42L, 12, 0, "pickup", 7L),
            (click.Op, click.Id, click.Window, click.WindowSeq, click.Slot, click.Button, click.Mode, click.Arrival));
    }

    [Fact]
    public void OtherOperationsAreParsed()
    {
        Assert.Equal("37500", Parse("""{"op":"rename","win":4,"ws":50,"text":"37500"}""").Text);
        Assert.Equal(3, Parse("""{"op":"close","win":3,"ws":42}""").Window);
        Assert.Equal(8, Parse("""{"op":"hold","slot":8}""").Slot);
        Assert.Equal("win", Parse("""{"op":"snapshot","what":"win"}""").What);
    }

    [Theory]
    [InlineData("""{"op":"say","text":"/pay x 1"}""", "unknown_op")]
    [InlineData("""{"op":"click","win":3,"ws":1,"slot":-999,"mode":"pickup"}""", "bad_request")]
    [InlineData("""{"op":"click","win":3,"ws":1,"slot":1.5,"mode":"pickup"}""", "bad_request")]
    [InlineData("""{"op":"click","ws":1,"slot":1,"mode":"pickup"}""", "bad_request")]
    [InlineData("""{"op":"rename","text":"1"}""", "bad_request")]
    [InlineData("""{"op":"hold","slot":9}""", "bad_request")]
    [InlineData("""{"op":"snapshot","what":"world"}""", "bad_request")]
    [InlineData("""{"op":"close","id":"bad id!","win":1,"ws":1}""", "bad_id")]
    [InlineData("""{"op":"rename","win":4,"ws":5,"text":"37500","text":"Elytra"}""", "bad_request")]
    [InlineData("""{"op":"hold","slot":1,"op":"click"}""", "bad_request")]
    [InlineData("""[1,2]""", "bad_request")]
    [InlineData("""not json""", "bad_request")]
    public void MalformedOperationsAreRefused(string json, string reason)
    {
        Assert.Equal(reason, Refuse(json));
    }

    [Theory]
    [InlineData("37500", true)]
    [InlineData("0", true)]
    [InlineData("999999999999", true)]
    [InlineData("9999999999999", false)]
    [InlineData("37.5k", false)]
    [InlineData("37 500", false)]
    [InlineData("Elytra", false)]
    [InlineData("", false)]
    [InlineData("١٢٣", false)]
    public void AnvilTextIsDigitsOnly(string text, bool valid)
    {
        Assert.Equal(valid, AgentOperation.IsValidRenameText(text));
    }
}

public sealed class AgentBridgeItemTests
{
    private static readonly DataTypes dataTypes = new(Protocol18Handler.MC_26_3_Version);
    private static readonly ItemPalette palette = new ItemPalette263();
    private static readonly SubComponentRegistry1212 subComponents = new(dataTypes);

    [Theory]
    [InlineData(ItemType.Elytra, "minecraft:elytra")]
    [InlineData(ItemType.DiscFragment5, "minecraft:disc_fragment_5")]
    [InlineData(ItemType.MusicDisc11, "minecraft:music_disc_11")]
    [InlineData(ItemType.NetheriteUpgradeSmithingTemplate, "minecraft:netherite_upgrade_smithing_template")]
    [InlineData(ItemType.Spawner, "minecraft:spawner")]
    [InlineData(ItemType.CarrotOnAStick, "minecraft:carrot_on_a_stick")]
    [InlineData(ItemType.JackOLantern, "minecraft:jack_o_lantern")]
    [InlineData(ItemType.WarpedFungusOnAStick, "minecraft:warped_fungus_on_a_stick")]
    public void RegistryNamesFollowTheGame(ItemType type, string expected)
    {
        Assert.Equal(expected, AgentBridgeItems.RegistryName(type));
    }

    private static JsonObject Render(Item item)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (Utf8JsonWriter writer = new(buffer))
            AgentBridgeItems.Write(writer, 16, item);
        return JsonNode.Parse(buffer.WrittenSpan)!.AsObject();
    }

    private static Item Elytra()
    {
        EnchantmentsComponent1215 enchantments = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:enchantments" };
        enchantments.Enchantments.Add(new Enchantment(Enchantments.Unbreaking, 3));
        enchantments.Enchantments.Add(new Enchantment(Enchantments.Mending, 1));
        DamageComponent damage = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:damage", Damage = 300 };
        return new Item(ItemType.Elytra, 1, null) { Components = [enchantments, damage] };
    }

    [Fact]
    public void EnchantmentsAndDamageComeFromTheComponents()
    {
        JsonObject json = Render(Elytra());

        Assert.Equal(16, (int)json["slot"]!);
        Assert.Equal("minecraft:elytra", (string)json["id"]!);
        Assert.Equal(3, (int)json["ench"]!["minecraft:unbreaking"]!);
        Assert.Equal(1, (int)json["ench"]!["minecraft:mending"]!);
        Assert.Equal(300, (int)json["damage"]!);
        Assert.Equal(new[] { "minecraft:enchantments", "minecraft:damage" }, json["cnames"]!.AsArray().Select(n => (string)n!));
        Assert.Null(json["unparsed"]);
        Assert.Equal(32, ((string)json["fp"]!).Length);
    }

    [Fact]
    public void StoredEnchantmentsAreNotActiveOnes()
    {
        StoredEnchantmentsComponent1215 stored = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:stored_enchantments" };
        stored.Enchantments.Add(new Enchantment(Enchantments.Sharpness, 5));
        JsonObject json = Render(new Item(ItemType.EnchantedBook, 1, null) { Components = [stored] });

        Assert.Null(json["ench"]);
        Assert.Equal(5, (int)json["stored"]!["minecraft:sharpness"]!);
    }

    [Fact]
    public void RemovedDefaultsAndUnknownEnchantmentsAreReported()
    {
        EnchantmentsComponent1215 enchantments = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:enchantments" };
        enchantments.Enchantments.Add(new Enchantment((Enchantments)(-1), 5));
        Item item = new(ItemType.Elytra, 1, null)
        {
            Components = [enchantments],
            RemovedComponents = ["minecraft:glider"],
        };

        JsonObject json = Render(item);

        Assert.Equal(new[] { "minecraft:glider" }, json["removed"]!.AsArray().Select(n => (string)n!));
        Assert.True((bool)json["unparsed"]!);
    }

    [Fact]
    public void FingerprintsTellStacksApart()
    {
        Item plain = new(ItemType.Elytra, 1, null);
        Item noGlider = new(ItemType.Elytra, 1, null) { RemovedComponents = ["minecraft:glider"] };
        Item two = new(ItemType.Elytra, 2, null);

        Assert.Equal(AgentBridgeItems.Fingerprint(Elytra()), AgentBridgeItems.Fingerprint(Elytra()));
        Assert.NotEqual(AgentBridgeItems.Fingerprint(plain), AgentBridgeItems.Fingerprint(noGlider));
        Assert.NotEqual(AgentBridgeItems.Fingerprint(plain), AgentBridgeItems.Fingerprint(two));
        Assert.NotEqual(AgentBridgeItems.Fingerprint(plain), AgentBridgeItems.Fingerprint(Elytra()));
    }

    [Fact]
    public void NamesAndLoreArePlainSingleLines()
    {
        Assert.Equal("Kanat⏎sahte", AgentBridgeItems.Plain("§6Kanat\n§csahte"));
        Assert.Equal(AgentBridgeItems.MaxTextLength, AgentBridgeItems.Plain(new string('x', 1000)).Length);
    }
}

public sealed class AgentBridgeChannelTests
{
    private static (AgentBridgeChannel Channel, StreamReader Events, StreamWriter Operations) Open()
    {
        AnonymousPipeServerStream eventsServer = new(PipeDirection.In);
        AnonymousPipeClientStream eventsClient = new(PipeDirection.Out, eventsServer.ClientSafePipeHandle);
        AnonymousPipeServerStream operationsServer = new(PipeDirection.Out);
        AnonymousPipeClientStream operationsClient = new(PipeDirection.In, operationsServer.ClientSafePipeHandle);

        AgentBridgeChannel channel = new();
        channel.Start(eventsClient, operationsClient);
        return (channel, new StreamReader(eventsServer, Encoding.UTF8), new StreamWriter(operationsServer) { AutoFlush = true });
    }

    private static async Task<JsonObject> NextEvent(StreamReader events)
    {
        Task<string?> line = events.ReadLineAsync();
        Assert.Same(line, await Task.WhenAny(line, Task.Delay(5000)));
        return JsonNode.Parse(line.Result!)!.AsObject();
    }

    private static async Task<AgentOperation> NextOperation(AgentBridgeChannel channel)
    {
        for (int i = 0; i < 500; i++)
        {
            if (channel.TryTakeOperation(out AgentOperation? operation))
                return operation;
            await Task.Delay(10);
        }

        throw new TimeoutException();
    }

    [Fact]
    public async Task EventsAreNumberedJsonLines()
    {
        var (channel, events, _) = Open();

        channel.Emit("chat", w => w.WriteString("text", "a\nb"));
        channel.Emit("held", w => w.WriteNumber("slot", 2));

        JsonObject first = await NextEvent(events);
        JsonObject second = await NextEvent(events);
        Assert.Equal(("chat", 1L, "a\nb"), ((string)first["e"]!, (long)first["seq"]!, (string)first["text"]!));
        Assert.Equal(("held", 2L), ((string)second["e"]!, (long)second["seq"]!));
        Assert.True((long)second["at"]! >= (long)first["at"]!);
    }

    [Fact]
    public async Task OperationsArriveInOrderAndBadLinesAreAnswered()
    {
        var (channel, events, operations) = Open();

        await operations.WriteLineAsync("""{"op":"hold","id":"x1","slot":1}""");
        await operations.WriteLineAsync("""{"op":"hold","id":"x2","slot":12}""");
        await operations.WriteLineAsync(new string('a', AgentBridgeChannel.MaxOperationBytes + 10));
        await operations.WriteLineAsync("""{"op":"snapshot","id":"x3","what":"inv"}""");

        AgentOperation one = await NextOperation(channel);
        AgentOperation three = await NextOperation(channel);
        Assert.Equal(("x1", 1L), (one.Id, one.Arrival));
        Assert.Equal(("x3", 3L), (three.Id, three.Arrival));

        JsonObject badSlot = await NextEvent(events);
        JsonObject tooLong = await NextEvent(events);
        Assert.Equal(("err", "x2", "hold", "bad_request"),
            ((string)badSlot["e"]!, (string?)badSlot["id"], (string)badSlot["op"]!, (string)badSlot["reason"]!));
        Assert.Equal("too_long", (string)tooLong["reason"]!);
    }

    [Fact]
    public async Task OversizedEventsBecomeErrors()
    {
        var (channel, events, _) = Open();

        channel.Emit("chat", w => w.WriteString("text", new string('x', AgentBridgeChannel.MaxEventBytes)));

        JsonObject error = await NextEvent(events);
        Assert.Equal(("err", "too_large", "chat"), ((string)error["e"]!, (string)error["reason"]!, (string)error["event"]!));
    }

    [Fact]
    public async Task DescriptorNumbersOpenAsStreams()
    {
        // A pipe end known only by its number: an fd on Linux (the cloud host's case), a raw handle value on
        // Windows when no C runtime descriptor table was passed.
        using AnonymousPipeServerStream server = new(PipeDirection.In);
        int fd = (int)server.ClientSafePipeHandle.DangerousGetHandle();
        FileStream stream = AgentBridgeChannel.OpenDescriptor(fd, FileAccess.Write);
        server.ClientSafePipeHandle.SetHandleAsInvalid(); // the stream owns the descriptor now

        await stream.WriteAsync("ok\n"u8.ToArray());
        await stream.FlushAsync();
        await stream.DisposeAsync();

        using StreamReader reader = new(server);
        Assert.Equal("ok", await reader.ReadLineAsync());
    }
}

public sealed class AgentBridgeKindTests
{
    [Fact]
    public void SystemPlayerAndActionBarLinesAreToldApart()
    {
        ChatMessage system = new("x", null, false, -1, Guid.Empty, true);
        ChatMessage overlay = new("x", null, false, -1, Guid.Empty, true) { isOverlay = true };
        ChatMessage profileless = new("x", "Karsi", false, 1, Guid.Empty, true) { isProfileless = true };
        ChatMessage signed = new("x", false, 0, Guid.NewGuid(), null, "Karsi", null, 0, null, true);

        Assert.Equal("system", AgentBridge.ChatType(system));
        Assert.Equal("actionbar", AgentBridge.ChatType(overlay));
        Assert.Equal("player", AgentBridge.ChatType(profileless));
        Assert.Equal("player", AgentBridge.ChatType(signed));
    }

    [Theory]
    [InlineData(0, "player")]
    [InlineData(1, "system")]
    [InlineData(2, "actionbar")]
    public void LegacyChatPositionsMapToKinds(int position, string kind)
    {
        Assert.Equal(kind, AgentBridge.ChatType(new ChatMessage("x", null, true, position, Guid.Empty)));
    }

    [Fact]
    public void WindowTypesUseTheGameNames()
    {
        Assert.Equal("generic_9x6", AgentBridge.WindowTypeName(ContainerType.Generic_9x6));
        Assert.Equal("anvil", AgentBridge.WindowTypeName(ContainerType.Anvil));
        Assert.Equal("unknown", AgentBridge.WindowTypeName(ContainerType.Unknown));
    }

    [Fact]
    public void AtMostTenOperationsPassInABurst()
    {
        AgentBridge.OperationBudget budget = new(10);
        int passed = Enumerable.Range(0, 25).Count(_ => budget.TryTake());
        Assert.InRange(passed, 10, 11); // a token may refill while the loop runs
    }
}
