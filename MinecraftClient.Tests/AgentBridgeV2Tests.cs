using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_2;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

namespace MinecraftClient.Tests;

/// <summary>
/// The agent bridge's version 2 (docs/agent-bridge.md, "Version 2"): event groups, the player inventory, drop and use,
/// and the potion and color of items. Version 1's tests (AgentBridgeTests.cs) are unchanged.
/// </summary>
public sealed class AgentBridgeV2Tests
{
    private static AgentOperation Parse(string json, long arrival = 7)
    {
        Assert.True(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), arrival, out AgentOperation? operation,
            out _, out _, out string? reason), reason);
        return operation!;
    }

    private static string Refuse(string json)
    {
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 1, out _, out _, out _, out string? reason));
        return reason!;
    }

    [Fact]
    public void HelloSaysVersionTwo()
    {
        Assert.Equal(2, AgentBridge.ProtocolVersion);
    }

    [Theory]
    [InlineData(null, (int)AgentBridge.EventGroups.All)]
    [InlineData("inv,win,held", (int)(AgentBridge.EventGroups.Inventory | AgentBridge.EventGroups.Windows | AgentBridge.EventGroups.Held))]
    [InlineData(" chat , pos ,", (int)(AgentBridge.EventGroups.Chat | AgentBridge.EventGroups.Position))]
    [InlineData("players,bogus", (int)AgentBridge.EventGroups.Players)]
    [InlineData("CHAT", (int)AgentBridge.EventGroups.None)]
    [InlineData("", (int)AgentBridge.EventGroups.None)]
    public void EventGroupsComeFromTheVariable(string? value, int expected)
    {
        Assert.Equal((AgentBridge.EventGroups)expected, AgentBridge.ParseEventGroups(value));
    }

    [Fact]
    public void DropsAreParsed()
    {
        AgentOperation one = Parse("""{"op":"drop","id":"d1","win":0,"ws":0,"slot":10,"stack":false}""");
        Assert.Equal(("drop", "d1", 10, false, true), (one.Op, one.Id, one.Slot, one.Stack, one.OnPlayerInventory));

        AgentOperation stack = Parse("""{"op":"drop","win":3,"ws":57,"slot":3,"stack":true}""");
        Assert.Equal((3, 57L, true, false), (stack.Window, stack.WindowSeq, stack.Stack, stack.OnPlayerInventory));

        Assert.False(Parse("""{"op":"drop","win":0,"ws":0,"slot":36}""").Stack);
    }

    [Fact]
    public void UseAndPlayerInventoryClicksAreParsed()
    {
        Assert.Equal("use", Parse("""{"op":"use","id":"u1"}""").Op);

        AgentOperation click = Parse("""{"op":"click","win":0,"ws":0,"slot":9,"button":0,"mode":"pickup"}""");
        Assert.True(click.OnPlayerInventory);
        Assert.False(Parse("""{"op":"click","win":3,"ws":42,"slot":9,"mode":"pickup"}""").OnPlayerInventory);
        // Version 1's shape with window 0 and a real "ws" is not the player inventory: it stays a window operation.
        Assert.False(Parse("""{"op":"click","win":0,"ws":5,"slot":9,"mode":"pickup"}""").OnPlayerInventory);
    }

    [Theory]
    [InlineData("""{"op":"drop","ws":0,"slot":10}""", "bad_request")]
    [InlineData("""{"op":"drop","win":0,"ws":0,"slot":-999}""", "bad_request")]
    [InlineData("""{"op":"drop","win":0,"ws":0,"slot":10,"stack":1}""", "bad_request")]
    [InlineData("""{"op":"drop","win":0,"ws":0,"slot":10,"stack":"true"}""", "bad_request")]
    [InlineData("""{"op":"click","win":3,"ws":0,"slot":1,"mode":"pickup"}""", "bad_request")]
    [InlineData("""{"op":"close","win":0,"ws":0}""", "bad_request")]
    [InlineData("""{"op":"rename","win":0,"ws":0,"text":"1"}""", "bad_request")]
    [InlineData("""{"op":"hold","slot":1,"ws":0}""", "bad_request")]
    [InlineData("""{"op":"drop","win":0,"ws":-1,"slot":10}""", "bad_request")]
    [InlineData("""{"op":"drop","win":0,"ws":0,"slot":10,"stack":true,"stack":false}""", "bad_request")]
    [InlineData("""{"op":"throw","win":0,"ws":0,"slot":10}""", "unknown_op")]
    public void MalformedVersionTwoOperationsAreRefused(string json, string reason)
    {
        Assert.Equal(reason, Refuse(json));
    }

    [Theory]
    [InlineData(5, null)] // armor
    [InlineData(9, null)] // main
    [InlineData(44, null)] // hotbar
    [InlineData(45, null)] // off hand
    [InlineData(0, "bad_slot")] // crafting result
    [InlineData(4, "bad_slot")] // crafting grid
    [InlineData(46, "bad_slot")]
    public void PlayerInventorySlotsAreArmorMainHotbarAndOffHand(int slot, string? reason)
    {
        AgentOperation click = Parse($$"""{"op":"click","win":0,"ws":0,"slot":{{slot}},"mode":"pickup"}""", arrival: 9);
        Assert.Equal(reason, AgentBridge.PlayerInventoryRefusal(click, false, true, true, 8));
    }

    [Fact]
    public void PlayerInventoryWaitsForItsFirstViewAndNoWindow()
    {
        AgentOperation drop = Parse("""{"op":"drop","win":0,"ws":0,"slot":12}""", arrival: 5);

        Assert.Equal("wrong_window", AgentBridge.PlayerInventoryRefusal(drop, true, true, true, 1));
        Assert.Equal("inventory_disabled", AgentBridge.PlayerInventoryRefusal(drop, false, false, true, 1));
        Assert.Equal("not_ready", AgentBridge.PlayerInventoryRefusal(drop, false, true, false, 0));
        // Sent before the session's first "inv" was written: meant for an older view.
        Assert.Equal("stale", AgentBridge.PlayerInventoryRefusal(drop, false, true, true, 5));
        Assert.Null(AgentBridge.PlayerInventoryRefusal(drop, false, true, true, 4));
    }

    [Fact]
    public void UseIsAtMostOneASecond()
    {
        AgentBridge.OperationBudget budget = new(1);
        Assert.True(budget.TryTake());
        Assert.False(budget.TryTake());
    }

    private static readonly DataTypes dataTypes = new(Protocol18Handler.MC_26_3_Version);
    private static readonly ItemPalette palette = new ItemPalette263();
    private static readonly SubComponentRegistry1212 subComponents = new(dataTypes);

    private static JsonObject Render(Item item)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (Utf8JsonWriter writer = new(buffer))
            AgentBridgeItems.Write(writer, 20, item);
        return JsonNode.Parse(buffer.WrittenSpan)!.AsObject();
    }

    private static PotionContentsComponent1212 Potion(int? id, int? customColor = null) =>
        new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:potion_contents",
            HasPotionId = id is not null,
            PotionId = id ?? 0,
            HasCustomColor = customColor is not null,
            CustomColor = customColor ?? 0,
        };

    [Fact]
    public void PotionsAreNamedByTheGamesRegistry()
    {
        Assert.Equal(46, PotionRegistry.Count);
        Assert.Equal("minecraft:water", PotionRegistry.Name(0));
        Assert.Equal("minecraft:strong_swiftness", PotionRegistry.Name(15));
        Assert.Equal("minecraft:infested", PotionRegistry.Name(45));
        Assert.Null(PotionRegistry.Name(46));
        Assert.Null(PotionRegistry.Name(-1));

        JsonObject json = Render(new Item(ItemType.Potion, 1, null) { Components = [Potion(15)] });
        Assert.Equal("minecraft:strong_swiftness", (string)json["potion"]!);
        Assert.Null(json["color"]);
        Assert.Null(json["unparsed"]);
    }

    [Fact]
    public void AnUnknownPotionMakesTheItemUnparsed()
    {
        JsonObject json = Render(new Item(ItemType.SplashPotion, 1, null) { Components = [Potion(99)] });
        Assert.Null(json["potion"]);
        Assert.True((bool)json["unparsed"]!);
    }

    [Fact]
    public void ColorsAreRgbIntegersDyedFirst()
    {
        JsonObject custom = Render(new Item(ItemType.Potion, 1, null) { Components = [Potion(null, unchecked((int)0xFF2E5A7B))] });
        Assert.Equal(0x2E5A7B, (int)custom["color"]!);
        Assert.Null(custom["potion"]);

        DyeColorComponent1215 dye = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:dyed_color", Color = 0xA06540 };
        JsonObject dyed = Render(new Item(ItemType.LeatherHelmet, 1, null) { Components = [dye, Potion(13, 0x00FF00)] });
        Assert.Equal(0xA06540, (int)dyed["color"]!);
        Assert.Equal("minecraft:swiftness", (string)dyed["potion"]!);
    }

    [Fact]
    public void LegacyNbtGivesThePotionAndColorToo()
    {
        Dictionary<string, object> potionNbt = new() { ["Potion"] = "long_swiftness", ["CustomPotionColor"] = 0x112233 };
        JsonObject potion = Render(new Item(ItemType.Potion, 1, potionNbt));
        Assert.Equal("minecraft:long_swiftness", (string)potion["potion"]!);
        Assert.Equal(0x112233, (int)potion["color"]!);

        Dictionary<string, object> leatherNbt = new() { ["display"] = new Dictionary<string, object> { ["color"] = 0x445566 } };
        JsonObject leather = Render(new Item(ItemType.LeatherChestplate, 1, leatherNbt));
        Assert.Equal(0x445566, (int)leather["color"]!);
        Assert.Null(leather["potion"]);
    }

    [Fact]
    public void ItemArtKeysAreNumbersAndIds()
    {
        CustomModelDataComponent modelData = new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:custom_model_data",
            Floats = [1001.9f],
        };
        ItemModelComponent model = new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:item_model",
            Identifier = "itemsadder:ruby_sword",
        };
        JsonObject json = Render(new Item(ItemType.DiamondSword, 1, null) { Components = [modelData, model] });
        Assert.Equal(1001, (int)json["cmd"]!);
        Assert.Equal("itemsadder:ruby_sword", (string)json["model"]!);

        // 1.21.4+: the first float, floored, only when finite and within an int.
        Assert.Equal(-4, AgentBridgeItems.ModelDataNumber(-3.5f));
        Assert.Equal(int.MinValue, AgentBridgeItems.ModelDataNumber(-2147483648f));
        Assert.Null(AgentBridgeItems.ModelDataNumber(float.NaN));
        Assert.Null(AgentBridgeItems.ModelDataNumber(float.PositiveInfinity));
        Assert.Null(AgentBridgeItems.ModelDataNumber(3e9f));

        // 1.20.5-1.21.3: the component's int; before 1.20.5: the NBT's CustomModelData; nothing without them.
        CustomModelDataComponent1206 old = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:custom_model_data", Value = 42 };
        Assert.Equal(42, (int)Render(new Item(ItemType.Paper, 1, null) { Components = [old] })["cmd"]!);
        Assert.Equal(7, (int)Render(new Item(ItemType.Paper, 1, new Dictionary<string, object> { ["CustomModelData"] = 7 }))["cmd"]!);
        CustomModelDataComponent empty = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:custom_model_data" };
        JsonObject plain = Render(new Item(ItemType.Paper, 1, null) { Components = [empty] });
        Assert.Null(plain["cmd"]);
        Assert.Null(plain["model"]);
    }

    [Fact]
    public void WindowTitlesKeepPrivateUseGlyphs()
    {
        // ItemsAdder draws menu backgrounds with Private Use Area glyphs: only formatting codes go.
        string glyphs = new string(new[] { (char)0xE001, (char)0xF801, (char)0xF8FF });
        Assert.Equal(glyphs + "Kitler", AgentBridgeItems.Plain("§f" + glyphs + "§6Kitler"));
    }

    [Fact]
    public void RawTitlesKeepFontsAndGlyphs()
    {
        // 26.3: a custom menu title, a resource pack glyph drawing the background, then the visible name.
        DataTypes data = new(Protocol18Handler.MC_26_3_Version);
        Dictionary<string, object> title = new()
        {
            ["text"] = "",
            ["extra"] = new object[]
            {
                new Dictionary<string, object> { ["text"] = "\uE001\uF801", ["font"] = "projects:gui", ["color"] = "white" },
                new Dictionary<string, object> { ["text"] = "Kitler" },
            },
        };
        string rendered = data.ReadNextChat(new Queue<byte>(data.GetNbt(title)), null, out object raw);
        Assert.Contains("Kitler", rendered);

        JsonObject json = JsonNode.Parse(AgentBridgeItems.RawComponent(raw)!)!.AsObject();
        JsonObject glyph = json["extra"]![0]!.AsObject();
        Assert.Equal(("projects:gui", "\uE001\uF801", "white"), ((string)glyph["font"]!, (string)glyph["text"]!, (string)glyph["color"]!));
        Assert.Equal("Kitler", (string)json["extra"]![1]!["text"]!);

        // 1.16.5: the JSON text, written compactly on one line.
        DataTypes old = new(754);
        old.ReadNextChat(new Queue<byte>(old.GetString("{ \"text\": \"\\uE001\",\n \"font\": \"projects:gui\" }")), null,
            out object oldRaw);
        string oldJson = Encoding.UTF8.GetString(AgentBridgeItems.RawComponent(oldRaw)!);
        Assert.DoesNotContain('\n', oldJson);
        Assert.Equal(("projects:gui", "\uE001"), ((string)JsonNode.Parse(oldJson)!["font"]!, (string)JsonNode.Parse(oldJson)!["text"]!));

        // A plain string root and text that is not JSON become JSON strings; too large or nothing: no raw title.
        Assert.Equal("\"Menü\"", Encoding.UTF8.GetString(AgentBridgeItems.RawComponent(new Dictionary<string, object> { [""] = "Menü" })!));
        Assert.Equal("\"Sandık\"", Encoding.UTF8.GetString(AgentBridgeItems.RawComponent("Sandık")!));
        Assert.Null(AgentBridgeItems.RawComponent("\"" + new string('x', AgentBridgeItems.MaxRawComponentBytes) + "\""));
        Assert.Null(AgentBridgeItems.RawComponent(null));
    }

    [Fact]
    public void IllFormedTextNeverBreaksAnEvent()
    {
        Assert.Equal("a\uFFFDb", AgentBridgeItems.WellFormed("a\uD800b"));
        Assert.Equal("a\uFFFD", AgentBridgeItems.WellFormed("a\uDC00"));
        Assert.Equal("😀x", AgentBridgeItems.WellFormed("😀x"));
        Assert.Same("plain", AgentBridgeItems.WellFormed("plain"));

        // A cut never ends on the first half of a pair.
        string cut = AgentBridgeItems.Plain(new string('x', AgentBridgeItems.MaxTextLength - 1) + "😀");
        Assert.Equal(AgentBridgeItems.MaxTextLength - 1, cut.Length);

        // An anvil name with a lone surrogate used to make the JSON writer throw.
        CustomNameComponent name = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:custom_name", CustomName = "Kılıç\uD800" };
        JsonObject item = Render(new Item(ItemType.Paper, 1, null) { Components = [name] });
        Assert.Equal("Kılıç\uFFFD", (string)item["name"]!);
    }
}
