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
        public readonly List<Dictionary<string, object>?> Payloads = [];

        public void Info(string text)
        {
        }

        public void SendText(string text) => Sent.Add(text);

        public bool SendCustomClickAction(string id, Dictionary<string, object>? payload)
        {
            Custom.Add(id);
            Payloads.Add(payload);
            return true;
        }
    }

    /// <summary>A dynamic command: its template filled in from the inputs.</summary>
    private static DialogButton Run(int index, string label, string template) =>
        new(index, label, new DialogActionDefinition(DialogActionKind.RunCommand, Value: template, Type: "minecraft:dynamic/run_command"));

    /// <summary>A plain command, sent as it is.</summary>
    private static DialogButton Plain(int index, string label, string command) =>
        new(index, label, new DialogActionDefinition(DialogActionKind.RunCommand, Value: command, Type: "minecraft:run_command"));

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
        Assert.True((bool)buttons[0]!["tpl"]!);
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
        yield return [Rules(Plain(1, "Öde", "pay NetherKurt" + char.ConvertFromUtf32(0x200B) + " 100"))];
        yield return [Rules(Plain(1, "Uzun", "say " + new string('x', 260)))];
        // A template the game would refuse: no variable, a $( without ).
        yield return [Rules(Run(1, "Doğ", "spawn"))];
        yield return [Rules(Run(1, "Söyle", "say $(name"))];
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

        // An apostrophe too, as the game's escaping does; a section sign is no character the game's field takes.
        dialogs.Show(Rules(Standard), DialogPhase.Play);
        Assert.Equal("input", (string?)Press(2, 2, new() { ["name"] = "a\u00A7b" })["why"]);
        Assert.True((bool)Press(2, 1, new() { ["name"] = "don't" }, "/kayit don\\'t hayir red 5")["ok"]!);
        Assert.Equal("/kayit don\\'t hayir red 5", client.Sent[^1]);
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
            [Plain(1, "Tamam", "spawn")], new DialogActionDefinition(DialogActionKind.RunCommand, Value: "spawn", Type: "minecraft:run_command"));
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
        DialogDefinition dialog = Rules(Plain(1, new string('b', 100), "say " + new string('x', 250))) with
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
    [Fact]
    public void APlainCommandIsSentAsItIs()
    {
        dialogs.Show(Rules(Plain(1, "Git", "warp $(name)")), DialogPhase.Play);
        JsonObject button = Last()["dialog"]!["buttons"]![0]!.AsObject();
        Assert.Equal("/warp $(name)", (string?)button["v"]);
        Assert.False((bool)button["tpl"]!);

        Assert.Equal("cmd", (string?)Press(1, 1, new() { ["name"] = "Kaan_07" }, "/warp Kaan_07")["why"]);
        Assert.True((bool)Press(1, 1, new() { ["name"] = "Kaan_07" }, "/warp $(name)")["ok"]!);
        Assert.Equal(new[] { "/warp $(name)" }, client.Sent);
    }

    [Fact]
    public void AClickSendsWhatTheGameSends()
    {
        DialogInput[] inputs =
        [
            new("note", DialogInputKind.Text, "Not", "", MaxLength: 16),
            new("fast", DialogInputKind.Boolean, "Hızlı", "true", OnTrue: "evet", OnFalse: "hayir"),
            new("amount", DialogInputKind.NumberRange, "Miktar", "1", Start: 0, End: 10, InitialNumber: 1, Step: 3),
        ];
        DialogButton plain = new(1, "Kabul", new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:ok",
            Payload: new Dictionary<string, object> { ["v"] = 1 }, Type: "minecraft:custom"));
        DialogButton dynamic = new(2, "Gönder", new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:send",
            Payload: new Dictionary<string, object> { ["note"] = "eski", ["src"] = "kart" }, Type: "minecraft:dynamic/custom"));
        dialogs.Show(Rules(plain, dynamic) with { Inputs = inputs }, DialogPhase.Play);

        // A slider takes only its steps, counted from its initial value.
        Assert.Equal("input", (string?)Press(1, 2, new() { ["amount"] = "6" })["why"]);
        // minecraft:custom sends its payload as it is, never the inputs.
        Assert.True((bool)Press(1, 1, new() { ["note"] = "selam", ["amount"] = "7" })["ok"]!);
        Assert.Equal(new Dictionary<string, object> { ["v"] = 1 }, client.Payloads[^1]);

        // minecraft:dynamic/custom sends its additions with every input over them: a text, a byte, a float.
        dialogs.Show(Rules(plain, dynamic) with { Inputs = inputs }, DialogPhase.Play);
        Assert.True((bool)Press(2, 2, new() { ["note"] = "selam", ["fast"] = "false", ["amount"] = "7" })["ok"]!);
        Dictionary<string, object> sent = client.Payloads[^1]!;
        Assert.Equal("selam", sent["note"]);
        Assert.Equal("kart", sent["src"]);
        Assert.Equal((byte)0, sent["fast"]);
        Assert.Equal(7F, sent["amount"]);
    }

    [Fact]
    public void AfterAPressTheGameClosesKeepsOrWaits()
    {
        DialogButton ok = new(1, "Tamam", new DialogActionDefinition(DialogActionKind.CustomClick, Id: "projects:ok", Type: "minecraft:custom"));

        dialogs.Show(Rules(ok) with { AfterAction = DialogAfterAction.None }, DialogPhase.Play);
        Assert.True((bool)Press(1, 1)["ok"]!);
        Assert.Equal(1, dialogs.Current?.Revision);
        Assert.NotNull(Last()["dialog"]);

        // Waiting: the game's "waiting for response" screen takes the dialog's place.
        dialogs.Show(Rules(ok) with { AfterAction = DialogAfterAction.WaitForResponse }, DialogPhase.Play);
        Assert.True((bool)Press(2, 1)["ok"]!);
        JsonObject waiting = Last();
        Assert.Equal(3, (int)waiting["rev"]!);
        Assert.True((bool)waiting["wait"]!);
        Assert.Null(dialogs.Current);

        // The game's clear leaves the waiting screen; its "Back" is a close of its revision, and sends nothing.
        int count = lines.Count;
        dialogs.Clear();
        Assert.Equal(count, lines.Count);
        Assert.Equal("stale", (string?)Act("{\"rev\":2,\"cancel\":true}")["why"]);
        Assert.True((bool)Act("{\"rev\":3,\"cancel\":true}")["ok"]!);
        Assert.True((bool)Last()["clear"]!);
        Assert.Equal(3, (int)Last()["rev"]!);
        Assert.Null(dialogs.Waiting);
        Assert.Single(client.Custom.Distinct());

        // A new dialog takes the waiting screen's place.
        dialogs.Show(Rules(ok) with { AfterAction = DialogAfterAction.WaitForResponse }, DialogPhase.Play);
        Assert.True((bool)Press(4, 1)["ok"]!);
        dialogs.Show(Rules(Standard), DialogPhase.Play);
        Assert.Null(dialogs.Waiting);
        Assert.Equal(6, (int)Last()["rev"]!);
    }

    private static DialogDefinition Menu(string title) => Rules(Standard) with { Title = title, ExternalTitle = null };

    [Fact]
    public void TheMenusAreWhatThePauseScreenAndQuickActionsOpen()
    {
        dialogs.StoreRegistryDialog(5, "projects:rules", Menu("§6Kurallar"));
        dialogs.StoreRegistryDialog(6, "projects:shop", Menu("Market") with { ExternalTitle = "Market…" });
        dialogs.StoreRegistryName(7, "minecraft:custom_options");
        dialogs.StoreRegistryName(8, "minecraft:quick_actions");
        dialogs.StoreRegistryName(9, "minecraft:server_links");

        // While configuring there are none.
        dialogs.SetTags(new Dictionary<string, int[]> { [DialogManager.PauseTag] = [5], [DialogManager.QuickTag] = [5, 6] });
        Assert.Equal("{\"e\":\"menus\"}", lines[^1][Prefix.Length..]);
        Assert.Equal("menu", (string?)Act("{\"open\":\"pause\"}")["why"]);

        dialogs.SetPhase(DialogPhase.Play);
        JsonObject menus = Last();
        Assert.Equal("menus", (string?)menus["e"]);
        Assert.Equal("Kurallar", (string?)menus["pause"]!["label"]);
        Assert.NotNull(menus["pause"]!["labelSt"]);
        Assert.Equal("Quick Actions...", (string?)menus["quick"]!["label"]);

        // One dialog opens itself; more open the game's list of them, each labelled as the game labels it.
        JsonObject opened = Act("{\"open\":\"quick\"}");
        Assert.True((bool)opened["ok"]!);
        Assert.Equal("quick", (string?)opened["menu"]);
        JsonObject list = Last()["dialog"]!.AsObject();
        Assert.Equal("Quick Actions", (string?)list["title"]);
        Assert.Equal(new[] { "Kurallar", "Market…" }, list["buttons"]!.AsArray().Select(button => (string)button!["label"]!));
        Assert.Equal(new[] { "dialog", "dialog" }, list["buttons"]!.AsArray().Select(button => (string)button!["a"]!));
        Assert.Equal("Back", (string?)list["exit"]!["label"]);

        // A list's button opens its dialog.
        int rev = (int)Last()["rev"]!;
        Assert.True((bool)Press(rev, 2)["ok"]!);
        Assert.Equal("Market", (string?)Last()["dialog"]!["title"]);

        // An empty pause tag offers the server's links instead.
        dialogs.SetServerLinks([new DialogServerLink("Site", "https://projects.gg")]);
        dialogs.SetTags(new Dictionary<string, int[]>());
        menus = Last();
        Assert.Equal("Server Links...", (string?)menus["pause"]!["label"]);
        Assert.Null(menus["quick"]);
        Assert.True((bool)Act("{\"open\":\"pause\"}")["ok"]!);
        JsonObject links = Last()["dialog"]!.AsObject();
        Assert.Equal("open_url", (string?)links["buttons"]![0]!["a"]);
        Assert.Equal(310, (int)links["buttons"]![0]!["w"]!);
        Assert.Equal("menu", (string?)Act("{\"open\":\"quick\"}")["why"]);
        Assert.Equal("line", (string?)Act("{\"open\":\"inventory\"}")["why"]);
    }

    [Fact]
    public void ADialogListShowsItsDialogsAsTheGameDoes()
    {
        dialogs.StoreRegistryDialog(5, "projects:rules", Menu("Kurallar"));
        dialogs.SetTags(new Dictionary<string, int[]> { ["projects:help"] = [5] });
        DialogDefinition list = Rules(
                new DialogButton(1, "projects:rules", new DialogActionDefinition(DialogActionKind.ShowDialog, Value: "projects:rules", Type: "dialog_reference_name")),
                new DialogButton(2, "#projects:help", new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_tag")))
            with
            {
                Type = "minecraft:dialog_list", Inputs = [], ButtonWidth = 200,
            };
        dialogs.Show(list, DialogPhase.Play);
        JsonArray buttons = Last()["dialog"]!["buttons"]!.AsArray();
        Assert.Equal(new[] { "Kurallar", "Kurallar" }, buttons.Select(button => (string)button!["label"]!));
        Assert.Equal(new[] { 1, 2 }, buttons.Select(button => (int)button!["i"]!));
        Assert.All(buttons, button => Assert.Equal(200, (int)button!["w"]!));

        // A dialog the client does not know: the card does not show the list.
        dialogs.Show(list with { Actions = [new DialogButton(1, "projects:nope", new DialogActionDefinition(DialogActionKind.ShowDialog, Value: "projects:nope", Type: "dialog_reference_name"))] },
            DialogPhase.Play);
        Assert.True((bool)Last()["clear"]!);
    }

    [Fact]
    public void AnUntouchedSliderIsWhatTheGameWouldSend()
    {
        DialogInput free = new("v", DialogInputKind.NumberRange, "Ses", "3.3", Start: 0, End: 10, InitialNumber: 3.3F);
        dialogs.Show(Rules(Run(1, "Ayarla", "volume $(v)")) with { Inputs = [free] }, DialogPhase.Play);
        float shown = (float)Last()["dialog"]!["inputs"]![0]!["initial"]!;
        Assert.Equal(DialogValues.UntouchedSlider(0, 10, null, 3.3F), shown);
        string expected = "/volume " + DialogValues.SliderText(shown);
        Assert.True((bool)Press(1, 1, cmd: expected)["ok"]!);
        Assert.Equal(new[] { expected }, client.Sent);
    }

}
