using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Tests;

/// <summary>Client 4.2.3: a head's skin and the spans of names and lore (docs/agent-bridge.md).</summary>
public sealed class AgentBridgeTextTests
{
    private static readonly DataTypes dataTypes = new(Protocol18Handler.MC_26_3_Version);
    private static readonly ItemPalette palette = new ItemPalette263();
    private static readonly SubComponentRegistry1212 subComponents = new(dataTypes);

    private const string Hash = "8a6d5e1f0c3b4a29e7d6c5b4a3928170fe6d5c4b3a2918076f5e4d3c2b1a0998";

    private static JsonObject Render(Item item)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (Utf8JsonWriter writer = new(buffer))
            AgentBridgeItems.Write(writer, 3, item);
        return JsonNode.Parse(buffer.WrittenSpan)!.AsObject();
    }

    private static string Textures(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    private static Item Head(params ProfileProperty[] properties)
    {
        ProfileComponent profile = new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:profile",
            HasName = true,
            Name = "FrostyOtter88",
            ProfileProperties = [.. properties],
        };
        return new Item(ItemType.PlayerHead, 1, null) { Components = [profile] };
    }

    [Fact]
    public void AHeadCarriesItsSkinHash()
    {
        string value = Textures($$"""{"timestamp":1,"textures":{"SKIN":{"url":"http://textures.minecraft.net/texture/{{Hash}}"}}}""");
        JsonObject json = Render(Head(new ProfileProperty("textures", value, true, "sig")));

        Assert.Equal(Hash, (string)json["skin"]!);
        Assert.Equal("FrostyOtter88", (string)json["components"]!["minecraft:profile"]!["name"]!);
    }

    [Theory]
    // Another host, a cape only, too short a hash, not base64, not JSON.
    [InlineData("""{"textures":{"SKIN":{"url":"https://example.com/texture/8a6d5e1f0c3b4a29e7d6"}}}""")]
    [InlineData("""{"textures":{"CAPE":{"url":"https://textures.minecraft.net/texture/8a6d5e1f0c3b4a29e7d6"}}}""")]
    [InlineData("""{"textures":{"SKIN":{"url":"https://textures.minecraft.net/texture/8a6d"}}}""")]
    [InlineData("""{"textures":{"SKIN":{"url":"https://textures.minecraft.net/texture/8a6d5e1f0c3b4a29e7d6/../x"}}}""")]
    public void OnlyMojangsTextureServerCounts(string payload)
    {
        Assert.Null(Render(Head(new ProfileProperty("textures", Textures(payload), false, null)))["skin"]);
    }

    [Fact]
    public void HeadPluginsBase64AndLenientPayloadsRead()
    {
        // URL-safe, unpadded, with a line break, keys without quotes (some head plugins write these).
        string lenient = Textures($$"""{textures:{SKIN:{url:"https://textures.minecraft.net/texture/{{Hash.ToUpperInvariant()}}"}}}""");
        string mangled = lenient.TrimEnd('=').Replace('+', '-').Replace('/', '_').Insert(8, "\n");

        Assert.Equal(Hash, AgentBridgeText.SkinHash(mangled));
        Assert.Null(AgentBridgeText.SkinHash("not base64 at all!"));
        Assert.Null(AgentBridgeText.SkinHash(Textures("[1, 2, 3]")));
        Assert.Null(Render(Head())["skin"]);
    }

    private static Dictionary<string, object> Text(string text, params (string Key, object Value)[] style)
    {
        Dictionary<string, object> nbt = new() { ["text"] = text };
        foreach ((string key, object value) in style)
            nbt[key] = value;
        return nbt;
    }

    private static Item Named(Dictionary<string, object> name, params Dictionary<string, object>[] lore)
    {
        CustomNameComponent custom = new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:custom_name",
            CustomNameNbt = name,
            CustomName = ChatParser.ParseText(name),
        };
        LoreNameComponent1206 lines = new(dataTypes, palette, subComponents) { ComponentName = "minecraft:lore" };
        foreach (Dictionary<string, object> line in lore)
        {
            lines.LinesNbt.Add(line);
            lines.Lines.Add(ChatParser.ParseText(line));
        }

        List<StructuredComponent> components = [custom];
        if (lore.Length > 0)
            components.Add(lines);
        return new Item(ItemType.DiamondSword, 1, null) { Components = components };
    }

    private static List<(int, string)> Spans(JsonNode? node) =>
        node!.AsArray().Select(span => ((int)span![0]!, (string)span[1]!)).ToList();

    [Fact]
    public void ANameKeepsItsColourAndTheGamesItalic()
    {
        JsonObject gold = Render(Named(Text("Kılıç", ("color", "gold"), ("bold", (byte)1))));
        Assert.Equal("Kılıç", (string)gold["name"]!);
        Assert.Equal(new[] { (5, "6lo") }, Spans(gold["nameStyle"]));

        // A server's name that turns the italic off; a hex colour.
        JsonObject flat = Render(Named(Text("Yakut", ("color", "#E0115F"), ("italic", (byte)0))));
        Assert.Equal(new[] { (5, "#e0115f") }, Spans(flat["nameStyle"]));
    }

    [Fact]
    public void LoreSpansFollowTheComponentTreeAndTheGamesCodes()
    {
        Dictionary<string, object> damage = Text("Hasar ", ("color", "gray"), ("italic", (byte)0));
        damage["extra"] = new object[] { Text("+12", ("color", "red")) };
        Item item = Named(
            Text("Kılıç"),
            damage,
            Text(""),
            Text("§aYeşil §lkalın §rsade"),
            new Dictionary<string, object> { [""] = "düz" });

        JsonObject json = Render(item);

        Assert.Equal(new[] { "Hasar +12", "", "Yeşil kalın sade", "düz" }, json["lore"]!.AsArray().Select(n => (string)n!));
        JsonArray styles = json["loreStyle"]!.AsArray();
        Assert.Equal(4, styles.Count);
        // The extra keeps its parent's "italic": false.
        Assert.Equal(new[] { (6, "7"), (3, "c") }, Spans(styles[0]));
        Assert.Null(styles[1]);
        // A colour code turns the lore's italic off; §l adds bold; §r goes back to the line's own look.
        Assert.Equal(new[] { (6, "a"), (6, "al"), (4, "o") }, Spans(styles[2]));
        // A line without a look of its own is the game's italic lore.
        Assert.Equal(new[] { (3, "o") }, Spans(styles[3]));
    }

    [Fact]
    public void AnItemNameInTheFieldsOwnLookHasNoSpans()
    {
        ItemNameComponent name = new(dataTypes, palette, subComponents)
        {
            ComponentName = "minecraft:item_name",
            ItemNameNbt = Text("Elmas"),
            ItemName = "Elmas",
        };
        JsonObject json = Render(new Item(ItemType.Diamond, 1, null) { Components = [name] });

        Assert.Equal("Elmas", (string)json["itemName"]!);
        Assert.Null(json["itemNameStyle"]);
        Assert.Null(json["nameStyle"]);
        Assert.Null(json["loreStyle"]);
    }

    [Fact]
    public void SpansCoverThePlainTextExactly()
    {
        // Cut where Plain() cuts, merged where looks meet.
        Dictionary<string, object> longLine = Text(new string('x', 300), ("color", "aqua"));
        List<(int, string)>? spans = AgentBridgeText.Spans(longLine, AgentBridgeText.Look.Lore,
            AgentBridgeItems.Plain(ChatParser.ParseText(longLine)));
        Assert.Equal(new[] { (AgentBridgeItems.MaxTextLength, "bo") }, spans);

        Dictionary<string, object> split = Text("a", ("color", "red"));
        split["extra"] = new object[] { Text("b", ("color", "red")), "c" };
        Assert.Equal(new[] { (3, "c") }, AgentBridgeText.Spans(split, AgentBridgeText.Look.None, "abc"));

        // A walk that does not come to the plain text writes no spans.
        Assert.Null(AgentBridgeText.Spans(split, AgentBridgeText.Look.None, "abd"));
    }

    [Fact]
    public void JsonTextOfOlderVersionsWalksTheSameWay()
    {
        Assert.Equal(new[] { (5, "6"), (3, "6l") },
            AgentBridgeText.Spans("""{"text":"Eski ","color":"gold","italic":false,"extra":[{"text":"süs","bold":true}]}""",
                AgentBridgeText.Look.Lore, "Eski süs"));
        // A list is its first part with the others after it, as that part's extras.
        Assert.Equal(new[] { (4, "c") }, AgentBridgeText.Spans("""[{"text":"ab","color":"red"},"cd"]""", AgentBridgeText.Look.None, "abcd"));
        // Legacy text (1.8–1.12) keeps its codes.
        Assert.Equal(new[] { (5, "6"), (3, "6l") }, AgentBridgeText.Spans("§6Eski §lsüs", AgentBridgeText.Look.Lore, "Eski süs"));
    }

    [Fact]
    public void LegacyNbtItemsCarrySkinsAndSpans()
    {
        Dictionary<string, object> nbt = new()
        {
            ["display"] = new Dictionary<string, object>
            {
                ["Name"] = """{"text":"Eski Kılıç","color":"gold"}""",
                ["Lore"] = new object[] { """{"text":"Efsane","color":"light_purple","italic":false}""" },
            },
            ["SkullOwner"] = new Dictionary<string, object>
            {
                ["Name"] = "LavaPanda_7",
                ["Properties"] = new Dictionary<string, object>
                {
                    ["textures"] = new object[]
                    {
                        new Dictionary<string, object>
                        {
                            ["Value"] = Textures($$"""{"textures":{"SKIN":{"url":"https://textures.minecraft.net/texture/{{Hash}}"}}}"""),
                        },
                    },
                },
            },
        };

        JsonObject json = Render(new Item(ItemType.PlayerHead, 1, nbt));

        Assert.Equal(Hash, (string)json["skin"]!);
        Assert.Equal("Eski Kılıç", (string)json["name"]!);
        Assert.Equal(new[] { (10, "6o") }, Spans(json["nameStyle"]));
        Assert.Equal(new[] { (6, "d") }, Spans(json["loreStyle"]!.AsArray()[0]));
    }
}
