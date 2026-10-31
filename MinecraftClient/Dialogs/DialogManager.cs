using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Dialogs;

/// <summary>What a dialog needs of the client: its log, sending a command, a custom click.</summary>
internal interface IDialogClient
{
    void Info(string text);

    void SendText(string text);

    bool SendCustomClickAction(string id, Dictionary<string, object>? payload);
}

public sealed class DialogManager
{
    /// <summary>The dialog tag whose dialogs the game's pause screen offers.</summary>
    public const string PauseTag = "minecraft:pause_screen_additions";

    /// <summary>The dialog tag whose dialogs the game's quick actions key opens.</summary>
    public const string QuickTag = "minecraft:quick_actions";

    private readonly IDialogClient _client;
    private readonly Lock _lock = new();
    private readonly Dictionary<int, DialogDefinition> _registryById = new();
    private readonly Dictionary<string, DialogDefinition> _registryByName = new(StringComparer.Ordinal);
    private readonly List<DialogServerLink> _serverLinks = [];
    private Dictionary<string, int[]> _tags = new(StringComparer.Ordinal);
    private DialogPhase _phase = DialogPhase.Configuration;
    private DialogInstance? _current;
    private int? _waiting;
    private int _revision;

    public DialogManager(McClient client)
        : this(new ClientDialogs(client ?? throw new ArgumentNullException(nameof(client))))
    {
    }

    internal DialogManager(IDialogClient client)
    {
        _client = client;
    }

    private sealed class ClientDialogs(McClient client) : IDialogClient
    {
        public void Info(string text) => client.Log.Info(text);

        public void SendText(string text) => client.SendText(text);

        public bool SendCustomClickAction(string id, Dictionary<string, object>? payload) =>
            client.SendCustomClickAction(id, payload);
    }

    public event Action<DialogInstance>? DialogShown;
    public event Action<int>? DialogCleared;

    /// <summary>
    /// After a press on a dialog whose <c>after_action</c> is <c>wait_for_response</c>, the game shows its "waiting for
    /// response" screen in the dialog's place: this revision is that screen (client 4.2.5).
    /// </summary>
    public event Action<int>? DialogWaiting;

    /// <summary>What the pause screen and the quick actions key open may have changed (client 4.2.5).</summary>
    public event Action? MenusChanged;

    public DialogInstance? Current
    {
        get
        {
            lock (_lock)
                return _current;
        }
    }

    /// <summary>The revision of the "waiting for response" screen shown now, if any.</summary>
    public int? Waiting
    {
        get
        {
            lock (_lock)
                return _waiting;
        }
    }

    public void StoreRegistryDialog(int protocolId, string resourceId, DialogDefinition definition)
    {
        lock (_lock)
        {
            _registryById[protocolId] = definition;
            _registryByName[resourceId] = definition;
        }
    }

    /// <summary>
    /// A registry entry the server sent without its data (a pack both sides know: the game's own). The game's own
    /// dialogs (<c>minecraft:server_links</c>, <c>custom_options</c>, <c>quick_actions</c>) are known here as the game
    /// defines them.
    /// </summary>
    public void StoreRegistryName(int protocolId, string resourceId)
    {
        if (BuiltIn(resourceId) is not { } definition)
            return;
        StoreRegistryDialog(protocolId, resourceId, definition);
    }

    public void ClearRegistry()
    {
        lock (_lock)
        {
            _registryById.Clear();
            _registryByName.Clear();
        }
    }

    public void SetServerLinks(IEnumerable<DialogServerLink> links)
    {
        lock (_lock)
        {
            _serverLinks.Clear();
            _serverLinks.AddRange(links);
        }

        MenusChanged?.Invoke();
    }

    /// <summary>The dialog registry's tags (client 4.2.5): each tag's entries, by registry id, in the server's order.</summary>
    public void SetTags(IReadOnlyDictionary<string, int[]> tags)
    {
        lock (_lock)
            _tags = new Dictionary<string, int[]>(tags, StringComparer.Ordinal);
        MenusChanged?.Invoke();
    }

    /// <summary>The connection's phase: the pause screen and the quick actions key exist only in play.</summary>
    public void SetPhase(DialogPhase phase)
    {
        lock (_lock)
        {
            if (_phase == phase)
                return;
            _phase = phase;
        }

        MenusChanged?.Invoke();
    }

    public DialogInstance Show(DialogDefinition definition, DialogPhase phase)
    {
        DialogInstance instance;
        lock (_lock)
        {
            var expanded = ExpandDialogList(ExpandServerLinks(definition));
            var values = expanded.Inputs.ToDictionary(static input => input.Key, InitialValue, StringComparer.Ordinal);
            instance = new DialogInstance(++_revision, phase, expanded, values, DateTimeOffset.UtcNow);
            _current = instance;
            _waiting = null;
        }

        _client.Info("§e" + string.Format(Translations.dialog_received, instance.Definition.DisplayTitle()));
        DialogShown?.Invoke(instance);
        return instance;
    }

    public DialogInstance ShowRegistryReference(int protocolId, DialogPhase phase)
    {
        DialogDefinition? definition;
        lock (_lock)
            _registryById.TryGetValue(protocolId, out definition);

        if (definition is not null)
            return Show(definition, phase);

        var unresolved = new DialogDefinition(
            "minecraft:unresolved",
            string.Format(CultureInfo.InvariantCulture, Translations.dialog_unresolved_title, protocolId),
            null,
            CanCloseWithEscape: true,
            Pause: false,
            DialogAfterAction.Close,
            [new DialogBody(DialogBodyKind.Unknown, string.Format(CultureInfo.InvariantCulture, Translations.dialog_unresolved_body, protocolId))],
            [],
            [],
            null,
            IsResolved: false,
            UnresolvedReference: protocolId.ToString(CultureInfo.InvariantCulture));
        return Show(unresolved, phase);
    }

    /// <summary>
    /// The dialogs the game opens from its pause screen and with its quick actions key (client 4.2.5), as the game
    /// picks them: one dialog of the tag opens itself, more open the game's own list of them, and the pause screen
    /// offers the server's links when the tag is empty. None while configuring.
    /// </summary>
    public (DialogDefinition? Pause, DialogDefinition? Quick) Menus()
    {
        lock (_lock)
        {
            if (_phase != DialogPhase.Play)
                return (null, null);
            DialogDefinition? pause = Menu(PauseTag, "minecraft:custom_options")
                                      ?? (_serverLinks.Count > 0 ? Named("minecraft:server_links") : null);
            DialogDefinition? quick = Menu(QuickTag, "minecraft:quick_actions");
            return (pause is null ? null : ExpandDialogList(ExpandServerLinks(pause)),
                quick is null ? null : ExpandDialogList(ExpandServerLinks(quick)));
        }
    }

    /// <summary>Opens what the pause screen (<c>pause</c>) or the quick actions key (<c>quick</c>) opens, in play only.</summary>
    public DialogInstance? OpenMenu(string menu)
    {
        DialogDefinition? definition;
        lock (_lock)
        {
            if (_phase != DialogPhase.Play)
                return null;
            definition = menu switch
            {
                "pause" => Menu(PauseTag, "minecraft:custom_options")
                           ?? (_serverLinks.Count > 0 ? Named("minecraft:server_links") : null),
                "quick" => Menu(QuickTag, "minecraft:quick_actions"),
                _ => null,
            };
        }

        return definition is null ? null : Show(definition, DialogPhase.Play);
    }

    /// <summary>A tag's one dialog, or the game's list dialog when it has more; the caller holds the lock.</summary>
    private DialogDefinition? Menu(string tag, string list)
    {
        if (!_tags.TryGetValue(tag, out int[]? entries) || entries.Length == 0)
            return null;
        if (entries.Length > 1)
            return Named(list);
        return _registryById.TryGetValue(entries[0], out DialogDefinition? dialog) ? dialog : null;
    }

    private DialogDefinition? Named(string name) =>
        _registryByName.TryGetValue(name, out DialogDefinition? dialog) ? dialog : null;

    public void Clear()
    {
        int revision;
        lock (_lock)
        {
            // The game's clear closes a dialog, never its "waiting for response" screen.
            if (_current is null && _waiting is not null)
                return;
            revision = _current?.Revision ?? _revision;
            _current = null;
        }

        _client.Info(Translations.dialog_cleared);
        DialogCleared?.Invoke(revision);
    }

    public DialogActionResult Dismiss()
    {
        int revision;
        lock (_lock)
        {
            if (_current is not null)
            {
                revision = _current.Revision;
                _current = null;
            }
            else if (_waiting is { } waiting)
            {
                // The waiting screen's "Back".
                revision = waiting;
                _waiting = null;
            }
            else
            {
                return new DialogActionResult(false, Translations.dialog_none);
            }
        }

        DialogCleared?.Invoke(revision);
        return new DialogActionResult(true, Translations.dialog_dismissed);
    }

    public DialogActionResult SetInput(string key, string value)
    {
        lock (_lock)
        {
            if (_current is null)
                return new DialogActionResult(false, Translations.dialog_none);

            return SetCurrentInput(key, value);
        }
    }

    /// <summary>
    /// Sets an input of the dialog of <paramref name="revision"/> only (a card's press: the dialog the player saw, never a
    /// newer one).
    /// </summary>
    internal DialogActionResult SetInput(int revision, string key, string value)
    {
        lock (_lock)
        {
            if (_current is null || _current.Revision != revision)
                return new DialogActionResult(false, Translations.dialog_none);

            return SetCurrentInput(key, value);
        }
    }

    /// <summary>
    /// Presses button <paramref name="index"/> of the dialog of <paramref name="revision"/> only; a command button only
    /// when its command, filled in from the inputs, is exactly <paramref name="command"/> (the command the player
    /// confirmed). Nothing happens when the dialog changed, the button is unknown or the command differs.
    /// </summary>
    internal DialogActionResult Press(int revision, int index, string? command)
    {
        DialogInstance? instance;
        DialogButton? button;
        lock (_lock)
        {
            instance = _current;
            if (instance is null || instance.Revision != revision)
                return new DialogActionResult(false, Translations.dialog_none);

            button = instance.Definition.Actions.FirstOrDefault(action => action.Index == index);
        }

        if (button is null)
            return new DialogActionResult(false, string.Format(Translations.dialog_action_unknown, index));

        // The command is filled in from the same snapshot Execute sends from: what was compared is what is sent.
        if (button.Action is { Kind: DialogActionKind.RunCommand } run
            && !string.Equals(CommandOf(instance, run), command, StringComparison.Ordinal))
            return new DialogActionResult(false, Translations.dialog_action_invalid);

        return Execute(instance, button.Action, instance.Definition.AfterAction);
    }

    /// <summary>Cancels the dialog of <paramref name="revision"/> only, or leaves the waiting screen of that revision.</summary>
    internal DialogActionResult Cancel(int revision)
    {
        bool leftWaiting = false;
        lock (_lock)
        {
            if (_current is null && _waiting == revision)
            {
                // The waiting screen's "Back": the game sends nothing.
                _waiting = null;
                leftWaiting = true;
            }
            else if (_current is null || _current.Revision != revision)
            {
                return new DialogActionResult(false, Translations.dialog_none);
            }
        }

        if (!leftWaiting)
            return Cancel();
        DialogCleared?.Invoke(revision);
        return new DialogActionResult(true, Translations.dialog_dismissed);
    }

    /// <summary>
    /// The command a command button sends, starting with "/" (the game runs a dialog's command with or without one;
    /// without it the client would send it as chat): a <c>minecraft:run_command</c> as it is, a
    /// <c>minecraft:dynamic/run_command</c> filled in from the inputs as the game fills it. Null when the game would
    /// refuse the template.
    /// </summary>
    internal static string? CommandOf(DialogInstance instance, DialogActionDefinition action)
    {
        string? command = IsTemplate(action)
            ? DialogValues.FillTemplate(action.Value ?? string.Empty, BuildActionValues(instance).TemplateValues)
            : action.Value ?? string.Empty;
        if (command is null)
            return null;
        return command.StartsWith('/') ? command : "/" + command;
    }

    /// <summary>Whether a command button's command is a template the inputs fill in (<c>minecraft:dynamic/run_command</c>).</summary>
    internal static bool IsTemplate(DialogActionDefinition action) =>
        action.Kind == DialogActionKind.RunCommand && action.Type == "minecraft:dynamic/run_command";

    /// <summary>Sets an input of the open dialog; the caller holds the lock and has checked there is one.</summary>
    private DialogActionResult SetCurrentInput(string key, string value)
    {
        var current = _current!;
        var input = current.Definition.Inputs.FirstOrDefault(input => input.Key.Equals(key, StringComparison.Ordinal));
        if (input is null)
            return new DialogActionResult(false, string.Format(Translations.dialog_input_unknown, key));

        var normalized = NormalizeInputValue(input, value, out var error);
        if (error is not null)
            return new DialogActionResult(false, error);

        var values = current.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        values[key] = normalized;
        _current = current with { Values = values };
        return new DialogActionResult(true, string.Format(Translations.dialog_input_set, key, normalized));
    }

    public DialogActionResult Click(int index)
    {
        DialogButton? button;
        DialogInstance? instance;
        lock (_lock)
        {
            instance = _current;
            button = instance?.Definition.Actions.FirstOrDefault(action => action.Index == index);
        }

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        if (button is null)
            return new DialogActionResult(false, string.Format(Translations.dialog_action_unknown, index));

        return Execute(instance, button.Action, instance.Definition.AfterAction);
    }

    public DialogActionResult ClickLabel(string label)
    {
        DialogButton[] matches;
        DialogInstance? instance;
        lock (_lock)
        {
            instance = _current;
            matches = instance?.Definition.Actions
                .Where(action => action.Label.Equals(label, StringComparison.OrdinalIgnoreCase))
                .ToArray() ?? [];
        }

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        return matches.Length switch
        {
            0 => new DialogActionResult(false, string.Format(Translations.dialog_action_label_unknown, label)),
            > 1 => new DialogActionResult(false, string.Format(Translations.dialog_action_label_ambiguous, label)),
            _ => Execute(instance, matches[0].Action, instance.Definition.AfterAction)
        };
    }

    public DialogActionResult Cancel()
    {
        DialogInstance? instance;
        lock (_lock)
            instance = _current;

        if (instance is null)
            return new DialogActionResult(false, Translations.dialog_none);

        if (!instance.Definition.CanCloseWithEscape && instance.Definition.CancelAction is null)
            return new DialogActionResult(false, Translations.dialog_cannot_cancel);

        // The game closes a dialog with its exit action and then always closes it.
        return Execute(instance, instance.Definition.CancelAction, DialogAfterAction.Close);
    }

    /// <summary>
    /// Does <paramref name="action"/> of <paramref name="instance"/> as the game does, then what the game does after
    /// it: close the dialog, keep it, or show its "waiting for response" screen. A dialog the action opens takes the
    /// place of both.
    /// </summary>
    private DialogActionResult Execute(DialogInstance instance, DialogActionDefinition? action, DialogAfterAction after)
    {
        if (!instance.Definition.IsResolved)
            return new DialogActionResult(false, Translations.dialog_unresolved_action_disabled);

        DialogActionResult result;
        switch (action?.Kind ?? DialogActionKind.None)
        {
            case DialogActionKind.None:
                result = new DialogActionResult(true, Translations.dialog_action_closed);
                break;

            case DialogActionKind.RunCommand:
                if (instance.Phase != DialogPhase.Play)
                    return new DialogActionResult(false, Translations.dialog_action_command_not_in_play);

                if (CommandOf(instance, action!) is not { } command)
                    return new DialogActionResult(false, Translations.dialog_action_invalid);
                _client.SendText(command);
                result = new DialogActionResult(true, string.Format(Translations.dialog_action_command_sent, command));
                break;

            case DialogActionKind.CustomClick:
                if (action!.Id is null)
                    return new DialogActionResult(false, Translations.dialog_action_invalid);

                // minecraft:custom sends its payload as it is; minecraft:dynamic/custom its additions with every
                // input's value (an input's value over an addition of the same name), always as a compound.
                var payload = action.Type == "minecraft:custom"
                    ? action.Payload
                    : MergePayload(action.Payload, BuildActionValues(instance).TagValues);
                if (!_client.SendCustomClickAction(action.Id, payload))
                    return new DialogActionResult(false, Translations.dialog_action_custom_failed);

                result = new DialogActionResult(true, string.Format(Translations.dialog_action_custom_sent, action.Id));
                break;

            case DialogActionKind.ShowDialog:
                if (action!.NestedDialog is not null)
                {
                    Show(action.NestedDialog, instance.Phase);
                    return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                }

                if (action.DialogReferenceId is int referenceId)
                {
                    ShowRegistryReference(referenceId, instance.Phase);
                    return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                }

                if (action.Value is not null)
                {
                    DialogDefinition? referencedDialog;
                    lock (_lock)
                        _registryByName.TryGetValue(Identifier(action.Value), out referencedDialog);

                    if (referencedDialog is not null)
                    {
                        Show(referencedDialog, instance.Phase);
                        return new DialogActionResult(true, Translations.dialog_action_nested_opened);
                    }
                }

                return new DialogActionResult(false, Translations.dialog_action_invalid);

            case DialogActionKind.OpenUrl:
                result = new DialogActionResult(true, string.Format(Translations.dialog_action_open_url, action!.Value ?? string.Empty));
                break;

            case DialogActionKind.SuggestCommand:
                result = new DialogActionResult(true, string.Format(Translations.dialog_action_suggest_command, action!.Value ?? string.Empty));
                break;

            case DialogActionKind.CopyToClipboard:
                result = new DialogActionResult(true, string.Format(Translations.dialog_action_copy, action!.Value ?? string.Empty));
                break;

            default:
                return new DialogActionResult(false, string.Format(Translations.dialog_action_unsupported, action!.Type ?? action.Kind.ToString()));
        }

        After(instance.Revision, after);
        return result;
    }

    /// <summary>What the game does after a press on the dialog of <paramref name="revision"/>, if it is still open.</summary>
    private void After(int revision, DialogAfterAction after)
    {
        if (after == DialogAfterAction.None)
            return;
        int waiting;
        lock (_lock)
        {
            if (_current?.Revision != revision)
                return;
            _current = null;
            if (after == DialogAfterAction.Close)
            {
                waiting = -1;
            }
            else
            {
                waiting = ++_revision;
                _waiting = waiting;
            }
        }

        if (waiting < 0)
        {
            DialogCleared?.Invoke(revision);
            return;
        }

        _client.Info(Translations.dialog_waiting);
        DialogWaiting?.Invoke(waiting);
    }

    private DialogDefinition ExpandServerLinks(DialogDefinition definition)
    {
        if (!definition.Type.Equals("minecraft:server_links", StringComparison.Ordinal))
            return definition;

        // The game's server links dialog: one button a link, as wide as the dialog says, then nothing else.
        var linkActions = _serverLinks
            .Select((link, index) => new DialogButton(
                index + 1,
                link.Label,
                new DialogActionDefinition(DialogActionKind.OpenUrl, Value: link.Url, Type: "minecraft:open_url"),
                Width: definition.ButtonWidth))
            .ToList();

        return definition with { Actions = linkActions };
    }

    /// <summary>
    /// A dialog list as the game shows it: one button a dialog (a tag's in the server's order), labelled with the
    /// dialog's external title (else its title) and as wide as the list says. A dialog the client does not know stays
    /// unresolved (the card does not show the list).
    /// </summary>
    private DialogDefinition ExpandDialogList(DialogDefinition definition)
    {
        if (!definition.Type.Equals("minecraft:dialog_list", StringComparison.Ordinal))
            return definition;

        List<DialogButton> buttons = [];
        foreach (DialogButton button in definition.Actions)
        {
            DialogActionDefinition? action = button.Action;
            if (action is { Type: "dialog_tag" })
            {
                string tag = Identifier(button.Label.TrimStart('#'));
                foreach (int id in _tags.GetValueOrDefault(tag) ?? [])
                {
                    buttons.Add(_registryById.TryGetValue(id, out DialogDefinition? listed)
                        ? new DialogButton(0, listed.DisplayTitle(),
                            new DialogActionDefinition(DialogActionKind.ShowDialog, DialogReferenceId: id, Type: "minecraft:show_dialog"),
                            Width: definition.ButtonWidth)
                        : new DialogButton(0, button.Label, new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_reference_id")));
                }

                continue;
            }

            if (action is { Type: "dialog_reference_name", Value: { } name })
            {
                buttons.Add(_registryByName.TryGetValue(Identifier(name), out DialogDefinition? named)
                    ? button with { Label = named.DisplayTitle(), Width = definition.ButtonWidth }
                    : button with { Action = new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_reference_name") });
                continue;
            }

            buttons.Add(button with { Width = button.Width ?? definition.ButtonWidth });
        }

        return definition with { Actions = buttons.Select((button, i) => button with { Index = i + 1 }).ToList() };
    }

    /// <summary>A resource name with its namespace (<c>minecraft:</c> when it has none).</summary>
    private static string Identifier(string name) => name.Contains(':') ? name : "minecraft:" + name;

    /// <summary>
    /// The game's own dialogs (its <c>Dialogs</c> bootstrap), for a server that sends their registry entries without
    /// data: its links, its "custom options" list of the pause screen's dialogs, its quick actions list.
    /// </summary>
    internal static DialogDefinition? BuiltIn(string name)
    {
        DialogButton back = new(0, Translate("gui.back", "Back"), null, Width: 200);
        DialogDefinition ListOf(string title, string external, string tag) => new(
            "minecraft:dialog_list",
            Translate(title + ".title", external.TrimEnd('.')),
            Translate(title, external),
            CanCloseWithEscape: true,
            Pause: true,
            DialogAfterAction.Close,
            [],
            [],
            [new DialogButton(1, "#" + tag, new DialogActionDefinition(DialogActionKind.Unknown, Type: "dialog_tag"))],
            null,
            Columns: 1,
            ButtonWidth: 310,
            ExitButton: back);
        return name switch
        {
            "minecraft:server_links" => new DialogDefinition(
                "minecraft:server_links",
                Translate("menu.server_links.title", "Server Links"),
                Translate("menu.server_links", "Server Links..."),
                CanCloseWithEscape: true,
                Pause: true,
                DialogAfterAction.Close,
                [],
                [],
                [],
                null,
                Columns: 1,
                ButtonWidth: 310,
                ExitButton: back),
            "minecraft:custom_options" => ListOf("menu.custom_options", "Custom Options...", PauseTag),
            "minecraft:quick_actions" => ListOf("menu.quick_actions", "Quick Actions...", QuickTag),
            _ => null,
        };
    }

    private static string Translate(string key, string fallback) =>
        ChatParser.TranslateString(key) is { Length: > 0 } text && text != key ? text : fallback;

    private static DialogActionValues BuildActionValues(DialogInstance instance)
    {
        Dictionary<string, string> templateValues = new(StringComparer.Ordinal);
        Dictionary<string, object> tagValues = new(StringComparer.Ordinal);

        foreach (var input in instance.Definition.Inputs)
        {
            instance.Values.TryGetValue(input.Key, out var value);
            value ??= InitialValue(input);
            templateValues[input.Key] = ToTemplateValue(input, value);
            tagValues[input.Key] = ToNbtValue(input, value);
        }

        return new DialogActionValues(templateValues, tagValues);
    }

    private static Dictionary<string, object> MergePayload(Dictionary<string, object>? basePayload, Dictionary<string, object> inputTags)
    {
        Dictionary<string, object> payload = basePayload is null
            ? new(StringComparer.Ordinal)
            : new(basePayload, StringComparer.Ordinal);

        foreach (var (key, value) in inputTags)
            payload[key] = value;

        return payload;
    }

    /// <summary>
    /// An input's value before the player changes it: a slider's is the value the game's slider shows at its initial
    /// place (without a step, the initial value through the slider's own float sums).
    /// </summary>
    internal static string InitialValue(DialogInput input)
    {
        if (input.Kind != DialogInputKind.NumberRange)
            return input.InitialValue;
        float initial = input.InitialNumber ?? (input.Start + input.End) / 2F;
        return NumberToString(DialogValues.UntouchedSlider(input.Start, input.End, input.Step, initial));
    }

    private static string NormalizeInputValue(DialogInput input, string value, out string? error)
    {
        error = null;
        switch (input.Kind)
        {
            case DialogInputKind.Text:
                if (value.Length > input.MaxLength)
                {
                    error = string.Format(Translations.dialog_input_too_long, input.Key, input.MaxLength);
                    return input.InitialValue;
                }

                // The game's text fields take no section sign, control character or DEL; a line break only when
                // multiline.
                if (!value.All(c => DialogValues.AllowedInText(c, input.Multiline)))
                {
                    error = string.Format(Translations.dialog_input_text_invalid, input.Key);
                    return input.InitialValue;
                }

                return value;

            case DialogInputKind.Boolean:
                if (bool.TryParse(value, out var boolValue))
                    return boolValue ? "true" : "false";

                if (value.Equals(input.OnTrue, StringComparison.OrdinalIgnoreCase))
                    return "true";

                if (value.Equals(input.OnFalse, StringComparison.OrdinalIgnoreCase))
                    return "false";

                error = string.Format(Translations.dialog_input_boolean_invalid, input.Key);
                return input.InitialValue;

            case DialogInputKind.SingleOption:
                if (input.Options?.Any(option => option.Id.Equals(value, StringComparison.Ordinal)) == true)
                    return value;

                error = string.Format(Translations.dialog_input_option_invalid, input.Key);
                return input.InitialValue;

            case DialogInputKind.NumberRange:
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !float.IsFinite(number))
                {
                    error = string.Format(Translations.dialog_input_number_invalid, input.Key);
                    return InitialValue(input);
                }

                var min = Math.Min(input.Start, input.End);
                var max = Math.Max(input.Start, input.End);
                float initial = input.InitialNumber ?? (input.Start + input.End) / 2F;
                if (!DialogValues.SliderTakes(number, input.Start, input.End, null, initial))
                {
                    error = string.Format(CultureInfo.InvariantCulture, Translations.dialog_input_number_range_invalid, input.Key, min, max);
                    return InitialValue(input);
                }

                // A slider with a step takes only its steps, counted from its initial value as the game counts them.
                if (!DialogValues.SliderTakes(number, input.Start, input.End, input.Step, initial))
                {
                    error = string.Format(Translations.dialog_input_number_step_invalid, input.Key);
                    return InitialValue(input);
                }

                return NumberToString(number);

            default:
                return value;
        }
    }

    /// <summary>
    /// What a command template gets for an input's value, as the game writes it: a text escaped
    /// (<see cref="DialogValues.EscapeText"/>), a switch's <c>on_true</c> or <c>on_false</c>, a slider's number in
    /// Java's text (<see cref="DialogValues.SliderText"/>), an option's id.
    /// </summary>
    private static string ToTemplateValue(DialogInput input, string value)
    {
        return input.Kind switch
        {
            DialogInputKind.Boolean => value.Equals("true", StringComparison.OrdinalIgnoreCase) ? input.OnTrue : input.OnFalse,
            DialogInputKind.Text => DialogValues.EscapeText(value),
            DialogInputKind.NumberRange when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                => DialogValues.SliderText(number),
            _ => value
        };
    }

    private static object ToNbtValue(DialogInput input, string value)
    {
        return input.Kind switch
        {
            DialogInputKind.Boolean => (byte)(value.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0),
            DialogInputKind.NumberRange when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => value
        };
    }

    /// <summary>
    /// A number as an input's value is kept and passed: a whole number as an integer, else the shortest decimal that
    /// reads back as the same float (no exponent).
    /// </summary>
    internal static string NumberToString(float value)
    {
        var integer = (int)value;
        return integer == value ? integer.ToString(CultureInfo.InvariantCulture) : DialogValues.Plain(value);
    }

    private sealed record DialogActionValues(
        IReadOnlyDictionary<string, string> TemplateValues,
        Dictionary<string, object> TagValues);
}
