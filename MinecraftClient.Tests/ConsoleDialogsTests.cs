using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Dialogs;

namespace MinecraftClient.Tests;

/// <summary>
/// Dialog cards (docs/agent-bridge.md, "Client 4.2.4: dialog cards"): an open dialog is one event line a card can draw,
/// a dialog outside the rules is reported closed, and a press does only what the card showed: on the dialog the player
/// saw, a command only as the player confirmed it, never while the client is still configuring.
/// </summary>
[Collection(nameof(ConsoleKindsOutput))]
public sealed class ConsoleDialogsTests : IDisposable
{
    private const string Nonce = "Nq3vX8LkPz2RwT5yHc7dJm9bFs4gA1eU";
    private const string Prefix = Nonce + "@@MCC@@";

    private readonly List<string> lines = new();
    private readonly Action<string> previousOutput = ConsoleKinds.Output;
    private readonly string previousPrefix = ConsoleKinds.LinePrefix;
    private readonly bool previousEnabled = ConsoleKinds.Enabled;
    private readonly FakeClient client = new();
    private readonly DialogManager dialogs;

    public ConsoleDialogsTests()
    {
        ConsoleKinds.Output = lines.Add;
        ConsoleKinds.LinePrefix = LauncherBridge.BuildLinePrefix(Nonce);
        ConsoleKinds.Enabled = true;
        dialogs = new DialogManager(client);
        ConsoleDialogs.Attach(dialogs);
    }

    public void Dispose()
    {
        ConsoleKinds.Output = previousOutput;
        ConsoleKinds.LinePrefix = previousPrefix;
        ConsoleKinds.Enabled = previousEnabled;
    }

    private sealed class FakeClient : IDialogClient
    {
        public readonly List<string> Sent = [];
        public readonly List<string> Custom = [];

        public void Info(string text)
        {
        }

        public void SendText(string text) => Sent.Add(text);

        public bool SendCustomClickAction(string id, Dictionary<string, object>? payload)
        {
            Custom.Add(id);
            return true;
        }
    }

    private static DialogButton Run(int index, string label, string template) =>
        new(index, label, new DialogActionDefinition(DialogActionKind.RunCommand, Value: template));

    private static readonly DialogButton[] Standard =
    [
        Run(1, "§aKayıt ol", "kayit $(name) $(agree) $(team) $(amount)"),
        new(2, "Kabul", new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:rules", Type: "minecraft:custom")),
        new(3, "Kurallar", new DialogActionDefinition(DialogActionKind.OpenUrl, Value: "https://projects.gg/kurallar")),
        new(4, "Kapat", null),
    ];

    private static DialogDefinition Rules(params DialogButton[] buttons) => new(
        "minecraft:multi_action",
        "§6§lKurallar",
        null,
        CanCloseWithEscape: true,
        Pause: false,
        DialogAfterAction.Close,
        [new DialogBody(DialogBodyKind.PlainMessage, "§7Sunucuya hoş geldin.\nKuralları oku.")],
        [
            new DialogInput("name", DialogInputKind.Text, "Adın", "", MaxLength: 16),
            new DialogInput("agree", DialogInputKind.Boolean, "Kabul ediyorum", "false", OnTrue: "evet", OnFalse: "hayir"),
            new DialogInput("team", DialogInputKind.SingleOption, "Takım", "red",
                Options: [new DialogOption("red", "§cKırmızı", true), new DialogOption("blue", "Mavi", false)]),
            new DialogInput("amount", DialogInputKind.NumberRange, "Miktar", "5", Start: 0, End: 10, InitialNumber: 5, Step: 1),
        ],
        buttons,
        null,
        Columns: 2);

    private JsonObject Last()
    {
        string line = lines[^1];
        Assert.StartsWith(Prefix, line);
        return JsonNode.Parse(line[Prefix.Length..])!.AsObject();
    }

    private JsonObject Act(string json) => JsonNode.Parse(ConsoleDialogs.Act(dialogs, json))!.AsObject();

    private JsonObject Press(int rev, int index, Dictionary<string, string>? inputs = null, string? cmd = null)
    {
        JsonObject press = new() { ["rev"] = rev, ["i"] = index };
        if (inputs is not null)
        {
            JsonObject values = new();
            foreach ((string key, string value) in inputs)
                values[key] = value;
            press["inputs"] = values;
        }

        if (cmd is not null)
            press["cmd"] = cmd;
        return Act(press.ToJsonString());
    }

    [Fact]
    public void AnOpenDialogIsOneEventLineACardCanDraw()
    {
        dialogs.Show(Rules(Standard), DialogPhase.Play);

        JsonObject e = Last();
        Assert.Equal("dialog", (string?)e["e"]);
        Assert.Equal(1, (int)e["rev"]!);
        JsonObject d = e["dialog"]!.AsObject();
        Assert.Equal("Kurallar", (string?)d["title"]);
        Assert.Equal("[[8,\"6l\"]]", d["titleSt"]!.ToJsonString());
        Assert.Equal("Sunucuya hoş geldin.\nKuralları oku.", (string?)d["body"]![0]!["text"]);
        Assert.Equal("[[35,\"7\"]]", d["body"]![0]!["st"]!.ToJsonString());

        JsonArray inputs = d["inputs"]!.AsArray();
        Assert.Equal(new[] { "text", "bool", "option", "range" }, inputs.Select(input => (string)input!["kind"]!));
        Assert.Equal(16, (int)inputs[0]!["max"]!);
        Assert.Equal("evet", (string?)inputs[1]!["onTrue"]);
        Assert.Equal("Kırmızı", (string?)inputs[2]!["options"]![0]!["label"]);
        Assert.Equal("red", (string?)inputs[2]!["initial"]);
        Assert.Equal(5, (double)inputs[3]!["initial"]!);

        JsonArray buttons = d["buttons"]!.AsArray();
        Assert.Equal(new[] { "run_command", "custom", "open_url", "close" }, buttons.Select(button => (string)button!["a"]!));
        // A command button shows its template as the client runs it (with "/"); a custom click's payload stays here.
        Assert.Equal("/kayit $(name) $(agree) $(team) $(amount)", (string?)buttons[0]!["v"]);
        Assert.Null(buttons[1]!["v"]);
        Assert.Equal("[[8,\"a\"]]", buttons[0]!["labelSt"]!.ToJsonString());
        Assert.True((bool)d["cancel"]!);
        Assert.True((bool)d["play"]!);
        Assert.Equal(2, (int)d["cols"]!);
    }

    public static IEnumerable<object[]> OutsideTheRules()
    {
        yield return [Rules(new DialogButton(1, "Git", new DialogActionDefinition(DialogActionKind.OpenUrl, Value: "https://projects.gg@evil.example/")))];
        yield return [Rules(new DialogButton(1, "Git", new DialogActionDefinition(DialogActionKind.OpenUrl, Value: "https://xn--prjects-6ve.gg/")))];
        yield return [Rules(Run(1, "Öde", "pay NetherKurt" + char.ConvertFromUtf32(0x200B) + " 100"))];
        yield return [Rules(Run(1, "Uzun", "say " + new string('x', 260)))];
        yield return [Rules(new DialogButton(1, "?", new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_tag")))];
        yield return [Rules(Standard) with { Inputs = [new DialogInput("bad key", DialogInputKind.Text, "x", "")] }];
        // Inputs are never cut (a command needs every one): more than 16 close the card.
        yield return [Rules(Standard) with { Inputs = Enumerable.Range(0, 17).Select(i => new DialogInput("k" + i, DialogInputKind.Text, "x", "")).ToList() }];
        yield return [Rules(Standard) with { IsResolved = false }];
    }

    [Fact]
    public void LongListsAreCutNotDropped()
    {
        DialogButton[] many = Enumerable.Range(1, 70)
            .Select(i => new DialogButton(i, "Sunucu " + i, new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:pick", Type: "minecraft:custom")))
            .ToArray();
        dialogs.Show(Rules(many) with
        {
            Body = Enumerable.Range(0, 20).Select(i => new DialogBody(DialogBodyKind.PlainMessage, "satır " + i)).ToList(),
        }, DialogPhase.Play);

        JsonObject d = Last()["dialog"]!.AsObject();
        Assert.Equal(64, d["buttons"]!.AsArray().Count);
        Assert.Equal(16, d["body"]!.AsArray().Count);
        Assert.True((bool)d["cut"]!);

        dialogs.Show(Rules(Standard), DialogPhase.Play);
        Assert.Null(Last()["dialog"]!["cut"]);
    }

    [Fact]
    public void TheCardGetsTheGameSOwnDetails()
    {
        DialogButton tipped = new(2, "Kabul", new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:rules", Type: "minecraft:custom"),
            Tooltip: "§eKuralları kabul eder", Width: 120);
        DialogDefinition dialog = Rules(Standard[0], tipped, Standard[2]) with
        {
            AfterAction = DialogAfterAction.WaitForResponse,
            ExitButton = new DialogButton(0, "§cVazgeç", null),
            Body =
            [
                new DialogBody(DialogBodyKind.Item, "Ödül", "minecraft:item",
                    Item: new Dictionary<string, object> { ["id"] = "minecraft:diamond", ["count"] = 2 },
                    Description: "Ödül", ShowTooltip: false),
                new DialogBody(DialogBodyKind.Item, "[Eşya]", "minecraft:item",
                    Item: new Dictionary<string, object> { ["id"] = "minecraft:emerald" }),
            ],
            Inputs =
            [
                new DialogInput("note", DialogInputKind.Text, "Not", "", MaxLength: 100, Multiline: true, MaxLines: 4, Height: 64),
                new DialogInput("amount", DialogInputKind.NumberRange, "Miktar", "5", Start: 0, End: 10, InitialNumber: 5, Step: 1),
            ],
        };
        dialogs.Show(dialog, DialogPhase.Play);

        JsonObject d = Last()["dialog"]!.AsObject();
        JsonArray buttons = d["buttons"]!.AsArray();
        Assert.Equal("Kuralları kabul eder", (string?)buttons[1]!["tip"]);
        Assert.NotNull(buttons[1]!["tipSt"]);
        Assert.Equal(120, (int)buttons[1]!["w"]!);
        Assert.Null(buttons[0]!["tip"]);
        Assert.Equal("wait", (string?)d["after"]);
        Assert.Equal("Vazgeç", (string?)d["exit"]!["label"]);

        JsonArray body = d["body"]!.AsArray();
        Assert.Equal(2, body.Count);
        Assert.Equal("Ödül", (string?)body[0]!["text"]);
        Assert.Equal("minecraft:diamond", (string?)body[0]!["item"]!["id"]);
        Assert.True((bool)body[0]!["noTip"]!);
        // An item without a description of its own: no placeholder text.
        Assert.Equal("", (string?)body[1]!["text"]);
        Assert.Equal("minecraft:emerald", (string?)body[1]!["item"]!["id"]);

        JsonArray inputs = d["inputs"]!.AsArray();
        Assert.Equal(4, (int)inputs[0]!["lines"]!);
        Assert.Equal(64, (int)inputs[0]!["height"]!);
        Assert.Equal("%s: %s", (string?)inputs[1]!["fmt"]);
    }

    [Theory]
    [InlineData(null, "%s: %s")]
    [InlineData("options.generic_value", "%s: %s")]
    [InlineData("no.such.key", null)]
    public void ANumberInputSaysItsLabelFormat(string? key, string? expected)
    {
        Assert.Equal(expected, ConsoleDialogs.LabelFormat(key));
    }

    [Theory]
    [MemberData(nameof(OutsideTheRules))]
    public void ADialogOutsideTheRulesIsReportedClosed(DialogDefinition dialog)
    {
        dialogs.Show(dialog, DialogPhase.Play);

        JsonObject e = Last();
        Assert.Equal("dialog", (string?)e["e"]);
        Assert.True((bool)e["clear"]!);
        Assert.Null(e["dialog"]);
        Assert.Equal("stale", (string?)Press(1, 1)["why"]);
    }

    [Fact]
    public void ACommandRunsOnlyAsThePlayerConfirmedIt()
    {
        dialogs.Show(Rules(Standard), DialogPhase.Play);
        Dictionary<string, string> inputs = new() { ["name"] = "Kaan_07", ["agree"] = "true", ["team"] = "blue", ["amount"] = "7" };

        Assert.Equal("cmd", (string?)Press(1, 1, inputs, "/kayit Kaan_07 evet blue 8")["why"]);
        Assert.Equal("cmd", (string?)Press(1, 1, inputs)["why"]);
        Assert.Empty(client.Sent);

        JsonObject done = Press(1, 1, inputs, "/kayit Kaan_07 evet blue 7");
        Assert.True((bool)done["ok"]!);
        Assert.Equal(new[] { "/kayit Kaan_07 evet blue 7" }, client.Sent);
        // The dialog closes after the press, as in the game: the card goes too.
        Assert.True((bool)Last()["clear"]!);
    }

    [Fact]
    public void InputsAreCheckedAndFilledInAsTheGameFillsThem()
    {
        dialogs.Show(Rules(Standard), DialogPhase.Play);

        Assert.Equal("input", (string?)Press(1, 2, new() { ["team"] = "green" })["why"]);
        Assert.Equal("input", (string?)Press(1, 2, new() { ["amount"] = "11" })["why"]);
        Assert.Equal("input", (string?)Press(1, 2, new() { ["name"] = new string('a', 17) })["why"]);
        Assert.Equal("input", (string?)Press(1, 2, new() { ["name"] = "a" + char.ConvertFromUtf32(0x200B) + "b" })["why"]);
        Assert.Equal("input", (string?)Press(1, 2, new() { ["name"] = "a\nb" })["why"]);
        Assert.Equal("input", (string?)Press(1, 2, new() { ["nope"] = "x" })["why"]);
        Assert.Empty(client.Custom);

        // A text goes into the command as the game puts it there: its backslashes and quotes escaped.
        JsonObject done = Press(1, 1, new() { ["name"] = "Ka\"an\\" }, "/kayit Ka\\\"an\\\\ hayir red 5");
        Assert.True((bool)done["ok"]!);
        Assert.Equal(new[] { "/kayit Ka\\\"an\\\\ hayir red 5" }, client.Sent);
    }

    [Fact]
    public void AStaleUnknownOrUnreadablePressDoesNothing()
    {
        dialogs.Show(Rules(Standard), DialogPhase.Play);
        dialogs.Show(Rules(Standard), DialogPhase.Play);

        Assert.Equal("stale", (string?)Press(1, 2)["why"]);
        Assert.Equal("button", (string?)Press(2, 9)["why"]);
        Assert.Equal("line", (string?)Act("{\"rev\":2,\"i\":2,\"inputs\":{\"name\":\"%username%\"}}")["why"]);
        Assert.Equal("line", (string?)Act("not json")["why"]);
        Assert.Equal("line", (string?)Act("{\"rev\":\"2\",\"i\":2}")["why"]);
        Assert.Empty(client.Custom);

        Assert.True((bool)Press(2, 2)["ok"]!);
        Assert.Equal(new[] { "projects:rules" }, client.Custom);
        Assert.Empty(client.Sent);
    }

    [Fact]
    public void NoCommandRunsWhileTheClientIsConfiguring()
    {
        dialogs.Show(Rules(Standard), DialogPhase.Configuration);
        Assert.False((bool)Last()["dialog"]!["play"]!);

        Assert.Equal("phase", (string?)Press(1, 1, cmd: "/kayit  hayir red 5")["why"]);
        Assert.Empty(client.Sent);
        // A custom click answers a server that holds the join on its dialog.
        Assert.True((bool)Press(1, 2)["ok"]!);
        Assert.Equal(new[] { "projects:rules" }, client.Custom);
    }

    [Fact]
    public void ClosingNeverRunsACommand()
    {
        DialogDefinition notice = new("minecraft:notice", "Bildirim", null, true, false, DialogAfterAction.Close, [], [],
            [Run(1, "Tamam", "spawn")], new DialogActionDefinition(DialogActionKind.RunCommand, Value: "spawn"));
        dialogs.Show(notice, DialogPhase.Play);
        Assert.False((bool)Last()["dialog"]!["cancel"]!);
        Assert.Equal("cancel", (string?)Act("{\"rev\":1,\"cancel\":true}")["why"]);
        Assert.Empty(client.Sent);

        dialogs.Show(Rules(Standard), DialogPhase.Play);
        Assert.True((bool)Act("{\"rev\":2,\"cancel\":true}")["ok"]!);
        Assert.True((bool)Last()["clear"]!);
        Assert.Empty(client.Sent);
    }

    [Fact]
    public void ANestedDialogIsTheNextRevision()
    {
        DialogDefinition inner = Rules(Standard) with { Title = "İç pencere" };
        dialogs.Show(Rules(new DialogButton(1, "Aç", new DialogActionDefinition(DialogActionKind.ShowDialog, NestedDialog: inner))),
            DialogPhase.Play);
        Assert.Equal("dialog", (string?)Last()["dialog"]!["buttons"]![0]!["a"]);

        Assert.True((bool)Press(1, 1)["ok"]!);
        JsonObject e = Last();
        Assert.Equal(2, (int)e["rev"]!);
        Assert.Equal("İç pencere", (string?)e["dialog"]!["title"]);
    }

    [Fact]
    public void TextsAreCutButNoValueIs()
    {
        DialogDefinition dialog = Rules(Run(1, new string('b', 100), "say " + new string('x', 250))) with
        {
            Title = new string('t', 200),
        };
        dialogs.Show(dialog, DialogPhase.Play);

        JsonObject d = Last()["dialog"]!.AsObject();
        string title = (string)d["title"]!;
        Assert.Equal(ConsoleDialogs.MaxTitle, title.Length);
        Assert.EndsWith("…", title);
        Assert.Equal(ConsoleDialogs.MaxLabel, ((string)d["buttons"]![0]!["label"]!).Length);
        Assert.Equal("/say " + new string('x', 250), (string?)d["buttons"]![0]!["v"]);
    }
}
