using MinecraftClient.Protocol.Dialogs;

namespace MinecraftClient.Tests;

/// <summary>
/// The dialog parser reads what the game's codec writes: a field at its default is left out of the packet, so a button
/// list without columns has the game's two (CONSOLE-E2E's finding: a multi_action set to columns:2 came as one column).
/// </summary>
public sealed class DialogNbtParserTests
{
    private static Dictionary<string, object> C(params (string Key, object Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value);

    private static Dictionary<string, object> Command(string label, string command) =>
        C(("label", label), ("action", C(("type", "minecraft:run_command"), ("command", command))));

    private static Dictionary<string, object> Dialog(string type, params (string Key, object Value)[] more)
    {
        Dictionary<string, object> nbt = C(("type", type), ("title", "Kayit Formu"));
        foreach ((string key, object value) in more)
            nbt[key] = value;
        return nbt;
    }

    [Theory]
    [InlineData("minecraft:multi_action")]
    [InlineData("minecraft:dialog_list")]
    [InlineData("minecraft:server_links")]
    public void AButtonListWithoutColumnsHasTheGamesTwo(string type)
    {
        Dictionary<string, object> nbt = type == "minecraft:multi_action"
            ? Dialog(type, ("actions", new object[] { Command("Gonder", "say a"), Command("Duz", "say b") }))
            : Dialog(type);
        Assert.Equal(2, new DialogNbtParser().Parse(nbt).Columns);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ColumnsTheServerSetsAreKept(int columns)
    {
        Dictionary<string, object> nbt = Dialog("minecraft:multi_action",
            ("actions", new object[] { Command("Gonder", "say a") }), ("columns", columns));
        Assert.Equal(columns, new DialogNbtParser().Parse(nbt).Columns);
    }

    [Fact]
    public void ANoticeIsOneColumnAndAConfirmationTwo()
    {
        Assert.Equal(1, new DialogNbtParser().Parse(Dialog("minecraft:notice")).Columns);
        Assert.Equal(2, new DialogNbtParser().Parse(Dialog("minecraft:confirmation",
            ("yes", Command("Evet", "say e")), ("no", Command("Hayir", "say h")))).Columns);
    }
}
