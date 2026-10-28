using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Out-of-band bridge for the AFK+ agent host (contract: docs/agent-bridge.md). Reports chat lines with the click
    /// actions of their components, windows and their items, the player inventory, the held slot, the position and
    /// the player list; takes window clicks, an anvil name, window close, hotbar slot and snapshot requests, and
    /// (version 2) drops, using the held item, and clicks and drops on the player inventory while no window is open.
    /// The host may ask for only some event groups (<see cref="EventsEnvVar"/>).
    ///
    /// It never takes a text command: chat and commands keep going through stdin, built by the host from fixed
    /// templates. It only runs when the host passes both descriptors (MCC_AGENT_FD_OUT, MCC_AGENT_FD_IN), so the
    /// desktop launcher and every other user of the client are unaffected.
    /// </summary>
    public sealed class AgentBridge : ChatBot
    {
        /// <summary>Contract version, sent in "hello".</summary>
        public const int ProtocolVersion = 2;

        /// <summary>The version "hello" says to a host that asked for movement (<see cref="AgentMovement.MoveEnvVar"/>).</summary>
        public const int MovementProtocolVersion = 3;

        /// <summary>
        /// Comma-separated event groups the host wants (<c>chat</c>, <c>players</c>, <c>inv</c>, <c>win</c>,
        /// <c>held</c>, <c>pos</c>); the others are not even turned into JSON. Unset: every group.
        /// </summary>
        public const string EventsEnvVar = "MCC_AGENT_EVENTS";

        /// <summary>Groups of events a host can ask for; hello, joined, left, ack and err are always written.</summary>
        [Flags]
        internal enum EventGroups
        {
            None = 0,
            Chat = 1,
            Players = 2,
            Inventory = 4,
            Windows = 8,
            Held = 16,
            Position = 32,
            All = Chat | Players | Inventory | Windows | Held | Position,

            /// <summary>Version 3's <c>move</c> events: only with <see cref="AgentMovement.MoveEnvVar"/>, never in <c>All</c>.</summary>
            Movement = 64,
        }

        /// <summary>The groups <paramref name="value"/> names (unknown names count for nothing); null: all of them.</summary>
        internal static EventGroups ParseEventGroups(string? value)
        {
            if (value is null)
                return EventGroups.All;

            EventGroups groups = EventGroups.None;
            foreach (string name in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                groups |= name switch
                {
                    "chat" => EventGroups.Chat,
                    "players" => EventGroups.Players,
                    "inv" => EventGroups.Inventory,
                    "win" => EventGroups.Windows,
                    "held" => EventGroups.Held,
                    "pos" => EventGroups.Position,
                    _ => EventGroups.None,
                };
            }

            return groups;
        }

        /// <summary>The groups named, plus <c>pos</c> and <c>move</c> for a host that asked for movement.</summary>
        internal static EventGroups ReportedGroups(string? events, bool movement) =>
            ParseEventGroups(events) | (movement ? EventGroups.Position | EventGroups.Movement : EventGroups.None);

        private static readonly EventGroups reportedGroups =
            ReportedGroups(Environment.GetEnvironmentVariable(EventsEnvVar), AgentMovement.Enabled);

        /// <summary>
        /// A host that names event groups speaks version 2: window content is reported once a tick, and after the
        /// bot's own click or drop the client's view is reported even when the server sends nothing. Without it the
        /// timing is version 1's (an event for every packet).
        /// </summary>
        private static readonly bool versionTwoViews = Environment.GetEnvironmentVariable(EventsEnvVar) is not null;

        /// <summary>Ticks (50 ms) the view waits after the bot's own move for the server's answer.</summary>
        private const int SettleTicks = 3;

        private const int MaxOperationsPerSecond = 10;
        private const int MaxUsesPerSecond = 1;
        private const int MaxOperationsPerTick = 4;
        private const int FirstPlayerInventorySlot = 5; // armor; 0-4 are the crafting grid and its result
        private const int LastPlayerInventorySlot = 45; // off hand
        private const int PlayersIntervalTicks = 20;
        private const int PositionIntervalTicks = 20;
        private const int InventoryDebounceTicks = 5;
        private const int MaxPlayers = 200;
        private const int MaxChatLength = 2048;
        internal const int MaxSlotEventsPerUpdate = 12;
        private const int PlayerInventorySection = 36;

        private static bool helloSent;

        /// <summary>The host passed both descriptors.</summary>
        public static bool Enabled => AgentBridgeChannel.Configured;

        private readonly AgentBridgeChannel channel = AgentBridgeChannel.Instance;
        private readonly OperationBudget budget = new(MaxOperationsPerSecond);
        private readonly OperationBudget useBudget = new(MaxUsesPerSecond);
        private readonly EventGroups reported = reportedGroups;
        private bool active;
        private bool joined;
        private long staleBefore;
        private OpenWindow? window;
        private int closingWindowId = -1;
        private bool inventoryDirty;
        private int inventoryCooldown;

        // The first "inv" of the session: player inventory operations need it, and arrive after it.
        private bool inventoryReported;
        private long inventoryReportedArrival;

        // Version 2 views: the open window changed since the last report; no report before this tick.
        private readonly bool coalesceViews = versionTwoViews;
        private bool windowDirty;
        private int settleUntilTick;
        private int ticks;
        private string[] lastPlayers = [];
        private (double X, double Y, double Z, string Dim)? lastPosition;

        // Version 3: movement, for a host that asked for it.
        private AgentMovement? movement;
        private long handledStop;
        private string? lastFacing;

        /// <summary>The one window operations may act on: the last one the server opened.</summary>
        internal sealed class OpenWindow(int id, ContainerType type, long openSeq, long openArrival, int total)
        {
            public int Id { get; } = id;
            public ContainerType Type { get; } = type;
            public long OpenSeq { get; set; } = openSeq;
            public long OpenArrival { get; } = openArrival;
            public int Total { get; } = total;

            /// <summary>The window's content was reported (<c>win_items</c> of a report, not of a snapshot).</summary>
            public bool ContentSent { get; private set; }

            /// <summary>What the host was last told, slot by slot: the base the next changes are found against.</summary>
            private Dictionary<int, string> told = new();

            /// <summary>
            /// The slots a report tells the host about now, or null for the whole content: the first report, and a burst
            /// of more than <paramref name="maxSlots"/> changes (a full resync). Empty when nothing changed since the host
            /// was last told. The slots returned count as told.
            /// </summary>
            public List<int>? Changes(Container container, int maxSlots)
            {
                if (!ContentSent)
                {
                    ContentSent = true;
                    return null;
                }

                Dictionary<int, string> current = Fingerprints(Content(container));
                List<int> changed = current.Keys.Union(told.Keys)
                    .Where(slot => !current.TryGetValue(slot, out string? now)
                                   || !told.TryGetValue(slot, out string? before)
                                   || now != before)
                    .Order()
                    .ToList();
                if (changed.Count > maxSlots)
                    return null;

                told = current;
                return changed;
            }

            /// <summary>
            /// The whole content as <c>win_items</c> carries it (the first report, a resync, a <c>snapshot</c>): the
            /// slots in order, empty ones left out. It counts as told, a snapshot's too: one taken between the bot's
            /// click and the server's answer shows the client's guess, and the server's correction must then be a
            /// change against it (before 4.2.1 a refused click's resync went unreported and the slot looked empty).
            /// </summary>
            public List<KeyValuePair<int, Item>> Whole(Container container)
            {
                List<KeyValuePair<int, Item>> content = Content(container);
                told = Fingerprints(content);
                return content;
            }

            /// <summary>The window's slots the host is told about (its own and the player section), empty ones left out.</summary>
            private List<KeyValuePair<int, Item>> Content(Container container) => container.Items
                .Where(entry => entry.Key >= 0 && entry.Key < Total && !entry.Value.IsEmpty)
                .OrderBy(entry => entry.Key)
                .ToList();

            private static Dictionary<int, string> Fingerprints(List<KeyValuePair<int, Item>> content) =>
                content.ToDictionary(entry => entry.Key, entry => AgentBridgeItems.Fingerprint(entry.Value));
        }

        public override void Initialize()
        {
            if (!channel.TryStart(out string? error))
            {
                LogToConsole(string.Format(Translations.bot_agentBridge_unavailable, error));
                UnloadBot();
                return;
            }

            active = true;
            // Click actions are only collected for the chat events that carry them.
            ChatClicks.Collecting = Reports(EventGroups.Chat);
            if (AgentMovement.Enabled)
                movement = new AgentMovement(new MoveHost(Handler), EmitMove);

            if (!helloSent)
            {
                helloSent = true;
                int version = movement is null ? ProtocolVersion : MovementProtocolVersion;
                channel.Emit("hello", writer =>
                {
                    writer.WriteNumber("v", version);
                    writer.WriteNumber("pid", Environment.ProcessId);
                });
            }

            if (!GetInventoryEnabled())
            {
                LogToConsole(Translations.bot_agentBridge_inventory_disabled);
                channel.Refuse(null, "init", "inventory_disabled");
            }
        }

        public override void AfterGameJoined()
        {
            if (!active)
                return;

            DiscardQueuedOperations();
            window = null;
            closingWindowId = -1;
            lastPlayers = [];
            lastPosition = null;
            inventoryDirty = true;
            inventoryReported = false;
            windowDirty = false;
            settleUntilTick = 0;
            joined = true;

            channel.Emit("joined", writer =>
            {
                writer.WriteString("user", GetUsername());
                writer.WriteNumber("protocol", GetProtocolVersion());
            });
            lastFacing = null;
            handledStop = channel.LatestStopArrival;
            movement?.Joined();
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (!active)
                return false;

            joined = false;
            window = null;
            inventoryReported = false;
            movement?.Left();
            DiscardQueuedOperations();
            channel.Emit("left", writer => writer.WriteString("reason", reason.ToString()));
            return false; // reporting only: AutoRelog and the host keep owning reconnects
        }

        public override void OnRespawn()
        {
            // A respawn or another world: what moved stops ("reset"), sneaking ends.
            if (active && joined)
                movement?.Moved();
        }

        /// <summary>The host asked for this group of events (<see cref="EventsEnvVar"/>).</summary>
        private bool Reports(EventGroups group) => (reported & group) != 0;

        public override void OnChatReceived(string text, ChatMessage message)
        {
            if (!active || !Reports(EventGroups.Chat))
                return;

            string type = ChatType(message);
            string plain = AgentBridgeItems.Plain(text, MaxChatLength);
            List<ChatClick>? clicks = message.clicks;

            channel.Emit("chat", writer =>
            {
                writer.WriteString("type", type);
                writer.WriteString("text", plain);
                writer.WriteStartArray("clicks");
                if (clicks is not null)
                {
                    foreach (ChatClick click in clicks)
                    {
                        writer.WriteStartArray();
                        writer.WriteStringValue(click.Action);
                        writer.WriteStringValue(AgentBridgeItems.WellFormed(ConsoleIO.KeepOnOneLine(click.Value)));
                        writer.WriteEndArray();
                    }
                }

                writer.WriteEndArray();
            });
        }

        /// <summary>system: a server message; player: a player's chat line (signed or not); actionbar: above the hotbar.</summary>
        internal static string ChatType(ChatMessage message)
        {
            if (message.isOverlay)
                return "actionbar";
            if (message.isSignedChat)
                return message.isSystemChat && !message.isProfileless ? "system" : "player";

            // Before 1.19: the chat position byte (0 chat, 1 system, 2 game info).
            return message.chatTypeId switch
            {
                1 => "system",
                2 => "actionbar",
                _ => "player",
            };
        }

        public override void OnInventoryOpen(int inventoryId)
        {
            if (!active || inventoryId == 0)
                return;

            Container? container = Handler.GetInventory(inventoryId);
            if (container is null)
                return;

            if (window is not null && window.Id != inventoryId)
                EmitWindowClose(window.Id, "replaced");

            int total = container.Type.SlotCount();
            int size = container.Type == ContainerType.PlayerInventory ? total : Math.Max(0, total - PlayerInventorySection);
            string type = WindowTypeName(container.Type);
            string title = AgentBridgeItems.Plain(container.Title);
            long arrival = channel.LastArrival;

            // Without window events the window is still tracked (player inventory operations wait for it to close),
            // but no operation can name it: its "ws" stays 0.
            long seq = 0;
            if (Reports(EventGroups.Windows))
            {
                // The title component as the server sent it: custom menus draw their background with font glyphs.
                byte[]? titleRaw = AgentBridgeItems.RawComponent(container.TitleComponent);
                seq = channel.Emit("win_open", writer =>
                {
                    writer.WriteNumber("id", inventoryId);
                    writer.WriteString("type", type);
                    writer.WriteString("title", title);
                    writer.WriteNumber("size", size);
                    writer.WriteNumber("total", total);
                    if (titleRaw is not null)
                    {
                        writer.WritePropertyName("titleRaw");
                        writer.WriteRawValue(titleRaw, skipInputValidation: true);
                    }
                });
            }

            window = new OpenWindow(inventoryId, container.Type, seq, arrival, total);
            closingWindowId = -1;

            // Some servers send the content before the window: report what is already known.
            if (coalesceViews)
                windowDirty = container.Items.Count > 0;
            else if (container.Items.Count > 0)
                SyncWindow(window, container);
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            if (!active)
                return;

            if (inventoryId == 0)
                inventoryDirty = true;

            if (window is null || inventoryId != window.Id)
                return;

            // A version 2 host gets a burst of slot packets as one change, once a tick (Update).
            if (coalesceViews)
                windowDirty = true;
            else if (Handler.GetInventory(inventoryId) is { } container)
                SyncWindow(window, container);
        }

        public override void OnInventoryClose(int inventoryId)
        {
            if (!active || inventoryId == 0)
                return;

            bool ours = inventoryId == closingWindowId;
            closingWindowId = -1;
            if (window is not null && window.Id == inventoryId)
            {
                window = null;
                EmitWindowClose(inventoryId, ours ? "client" : "server");
            }
        }

        public override void OnHeldItemChange(byte slot)
        {
            if (active && Reports(EventGroups.Held))
                channel.Emit("held", writer => writer.WriteNumber("slot", slot));
        }

        public override void Update()
        {
            if (!active)
                return;

            ticks++;
            // After the bot's own move, wait a moment for the server's answer (if any) before reporting the view.
            bool settled = !coalesceViews || ticks >= settleUntilTick;
            if (settled)
                FlushWindow(); // before the operations: they see the content the host was told about

            // A stop takes effect at once, ahead of the operations waiting before it (version 3).
            if (movement is not null && channel.LatestStopArrival > handledStop)
            {
                handledStop = channel.LatestStopArrival;
                movement.Stop();
            }

            if (joined)
                ExecuteOperations();
            else
                DiscardQueuedOperations();

            if (joined)
                movement?.Tick();

            if (inventoryCooldown > 0)
                inventoryCooldown--;
            if (joined && inventoryDirty && inventoryCooldown == 0 && settled)
            {
                inventoryDirty = false;
                inventoryCooldown = InventoryDebounceTicks;
                EmitInventory();
            }

            if (joined && ticks % PlayersIntervalTicks == 0 && Reports(EventGroups.Players))
                CheckPlayers();
            if (joined && ticks % PositionIntervalTicks == PositionIntervalTicks / 2 && Reports(EventGroups.Position))
                CheckPosition();
        }

        private void ExecuteOperations()
        {
            for (int i = 0; i < MaxOperationsPerTick && channel.TryTakeOperation(out AgentOperation? operation); i++)
            {
                string? error = Execute(operation);
                if (error is null)
                {
                    channel.Emit("ack", writer =>
                    {
                        if (operation.Id is not null)
                            writer.WriteString("id", operation.Id);
                        writer.WriteString("op", operation.Op);
                    });
                    if (coalesceViews && operation.Op is AgentOperation.Click or AgentOperation.Drop)
                        ReportAfterMove(operation);
                }
                else
                {
                    channel.Refuse(operation.Id, operation.Op, error);
                }
            }
        }

        /// <summary>
        /// A version 2 host sees the result of the bot's click or drop: the window and the player inventory are
        /// reported once the server's answer had <see cref="SettleTicks"/> to arrive, also when it sends none (a
        /// vanilla server answers a correctly predicted click with silence; the client's view is then the result).
        /// </summary>
        private void ReportAfterMove(AgentOperation operation)
        {
            if (!operation.OnPlayerInventory)
                windowDirty = true;
            inventoryDirty = true;
            settleUntilTick = ticks + SettleTicks;
        }

        /// <summary>Report the open window's changes made since the last report (version 2 hosts).</summary>
        private void FlushWindow()
        {
            if (!windowDirty)
                return;

            windowDirty = false;
            if (window is not null && Handler.GetInventory(window.Id) is { } container)
                SyncWindow(window, container);
        }

        /// <summary>Carry out one operation; null on success, else the reason it was refused.</summary>
        private string? Execute(AgentOperation operation)
        {
            // A stop is never refused: it stopped movement when it arrived (Update), or there is nothing to stop.
            if (operation.Op == AgentOperation.Stop)
            {
                movement?.Stop();
                return null;
            }

            if (operation.Arrival <= staleBefore)
                return "stale";
            if (operation.Moves && operation.Arrival < channel.LatestStopArrival)
                return "stale"; // a stop came after it
            if (!budget.TryTake())
                return "rate_limited";

            if (operation.Moves)
                return movement is null ? "unknown_op" : movement.Start(operation);

            switch (operation.Op)
            {
                case AgentOperation.Click:
                {
                    if (CheckSlot(operation, out int windowId) is { } slotError)
                        return slotError;

                    WindowActionType? action = (operation.Mode, operation.Button) switch
                    {
                        ("pickup", 0) => WindowActionType.LeftClick,
                        ("pickup", 1) => WindowActionType.RightClick,
                        ("quick_move", 0) => WindowActionType.ShiftClick,
                        ("quick_move", 1) => WindowActionType.ShiftRightClick,
                        _ => null,
                    };
                    if (action is null)
                        return operation.Mode is "pickup" or "quick_move" ? "bad_button" : "bad_mode";

                    return Handler.DoWindowAction(windowId, operation.Slot, action.Value) ? null : "send_failed";
                }

                case AgentOperation.Drop:
                {
                    if (CheckSlot(operation, out int windowId) is { } slotError)
                        return slotError;
                    // An item on the cursor first goes back (the host does that); dropping now could lose track of it.
                    if (Holds(0, -1))
                        return "bad_state";
                    if (!Holds(windowId, operation.Slot))
                        return "empty_slot";

                    // A window click in mode 4: button 0 throws one item, 1 the stack. Never slot -999.
                    WindowActionType drop = operation.Stack ? WindowActionType.DropItemStack : WindowActionType.DropItem;
                    return Handler.DoWindowAction(windowId, operation.Slot, drop) ? null : "send_failed";
                }

                case AgentOperation.Use:
                {
                    if (window is not null)
                        return "window_open";
                    if (!useBudget.TryTake())
                        return "rate_limited";

                    return Handler.UseItemOnHand() ? null : "send_failed";
                }

                case AgentOperation.Rename:
                {
                    if (CheckWindow(operation) is { } windowError)
                        return windowError;
                    if (window!.Type != ContainerType.Anvil)
                        return "not_anvil";
                    if (!AgentOperation.IsValidRenameText(operation.Text))
                        return "bad_text";

                    return Handler.SendRenameItemInWindow(window.Id, operation.Text!) ? null : "send_failed";
                }

                case AgentOperation.Close:
                {
                    if (CheckWindow(operation) is { } windowError)
                        return windowError;

                    closingWindowId = window!.Id;
                    return Handler.CloseInventory(window.Id) ? null : "send_failed";
                }

                case AgentOperation.Hold:
                {
                    if (!Handler.ChangeSlot((short)operation.Slot))
                        return "send_failed";
                    int slot = operation.Slot;
                    if (Reports(EventGroups.Held))
                        channel.Emit("held", writer => writer.WriteNumber("slot", slot));
                    return null;
                }

                case AgentOperation.Snapshot:
                {
                    if (operation.What == "inv")
                    {
                        if (!Reports(EventGroups.Inventory))
                            return "masked";
                        EmitInventory();
                        return null;
                    }

                    if (!Reports(EventGroups.Windows))
                        return "masked";
                    if (window is null || Handler.GetInventory(window.Id) is not { } container)
                        return "no_window";

                    EmitWindowItems(window, container);
                    return null;
                }
            }

            return "unknown_op";
        }

        /// <summary>
        /// The window and slot a click or drop acts on: the open window's (as in version 1), or the player inventory's
        /// while no window is open (<c>win: 0, ws: 0</c>, slots 5-45). Null when the slot may be used.
        /// </summary>
        private string? CheckSlot(AgentOperation operation, out int windowId)
        {
            windowId = 0;
            if (operation.OnPlayerInventory)
                return PlayerInventoryRefusal(operation, window is not null, GetInventoryEnabled(), inventoryReported,
                    inventoryReportedArrival);

            if (CheckWindow(operation) is { } windowError)
                return windowError;
            if (operation.Slot >= window!.Total)
                return "bad_slot";

            windowId = window.Id;
            return null;
        }

        /// <summary>
        /// Why a click or drop on the player inventory (<c>win: 0, ws: 0</c>) may not run now, or null: no window may
        /// be open, the session's first <c>inv</c> must have been written and the operation sent after it, and only the
        /// armor, main, hotbar and off hand slots (5-45) are taken.
        /// </summary>
        internal static string? PlayerInventoryRefusal(AgentOperation operation, bool windowOpen, bool inventoryEnabled,
            bool inventoryReported, long inventoryReportedArrival)
        {
            if (windowOpen)
                return "wrong_window";
            if (!inventoryEnabled)
                return "inventory_disabled";
            if (!inventoryReported)
                return "not_ready";
            if (operation.Arrival <= inventoryReportedArrival)
                return "stale";
            if (operation.Slot is < FirstPlayerInventorySlot or > LastPlayerInventorySlot)
                return "bad_slot";
            return null;
        }

        /// <summary>The client sees an item in that slot of that window (slot -1 of window 0: the cursor).</summary>
        private bool Holds(int windowId, int slot)
        {
            return Handler.GetInventory(windowId) is { } container
                   && container.Items.TryGetValue(slot, out Item? item)
                   && item is { IsEmpty: false };
        }

        /// <summary>The operation names the open window: right id, right "ws", sent after the window opened.</summary>
        private string? CheckWindow(AgentOperation operation)
        {
            if (window is null)
                return "no_window";
            return WindowRefusal(operation, window.Id, window.OpenSeq, window.OpenArrival, window.ContentSent);
        }

        /// <summary>
        /// Why an operation on the open window may not run now, or null: <c>win</c> and <c>ws</c> must be the window's
        /// and the operation sent after the window opened. Clicks and names also wait for the window's content to be
        /// reported; <c>close</c> does not, so a window the server sent but never filled (a plugin that failed while
        /// opening it, as Folia's thread checks make one do) can still be closed.
        /// </summary>
        internal static string? WindowRefusal(AgentOperation operation, int windowId, long openSeq, long openArrival,
            bool contentSent)
        {
            if (operation.Window != windowId)
                return "wrong_window";
            if (operation.WindowSeq is null)
                return "bad_request";
            if (operation.WindowSeq != openSeq)
                return "wrong_window";
            if (operation.Arrival <= openArrival)
                return "stale";
            if (!contentSent && operation.Op != AgentOperation.Close)
                return "not_ready";
            return null;
        }

        private void DiscardQueuedOperations()
        {
            staleBefore = channel.LastArrival;
            channel.DiscardOperations(operation =>
            {
                // A stop is never refused (version 3): out of the game nothing moves anyway.
                if (operation.Op == AgentOperation.Stop)
                    Acknowledge(operation);
                else
                    channel.Refuse(operation.Id, operation.Op, "stale");
            });
        }

        private void Acknowledge(AgentOperation operation)
        {
            channel.Emit("ack", writer =>
            {
                if (operation.Id is not null)
                    writer.WriteString("id", operation.Id);
                writer.WriteString("op", operation.Op);
            });
        }

        private void SyncWindow(OpenWindow open, Container container)
        {
            if (!Reports(EventGroups.Windows))
                return;

            List<int>? changed = open.Changes(container, MaxSlotEventsPerUpdate);
            if (changed is null)
            {
                EmitWindowItems(open, container);
                return;
            }

            foreach (int slot in changed)
            {
                container.Items.TryGetValue(slot, out Item? item);
                Item? present = item is { IsEmpty: false } ? item : null;
                int stateId = container.StateID;
                channel.Emit("win_slot", writer =>
                {
                    writer.WriteNumber("id", open.Id);
                    writer.WriteNumber("ws", open.OpenSeq);
                    writer.WriteNumber("state", stateId);
                    writer.WriteNumber("slot", slot);
                    writer.WritePropertyName("item");
                    if (present is null)
                        writer.WriteNullValue();
                    else
                        AgentBridgeItems.Write(writer, slot, present);
                });
            }
        }

        /// <summary>The whole content (a report or a snapshot); the <c>win_slot</c> changes that follow are against it.</summary>
        private void EmitWindowItems(OpenWindow open, Container container)
        {
            List<KeyValuePair<int, Item>> items = open.Whole(container);
            int stateId = container.StateID;

            channel.Emit("win_items", writer =>
            {
                writer.WriteNumber("id", open.Id);
                writer.WriteNumber("ws", open.OpenSeq);
                writer.WriteNumber("state", stateId);
                writer.WriteStartArray("items");
                foreach ((int slot, Item item) in items)
                    AgentBridgeItems.Write(writer, slot, item);
                writer.WriteEndArray();
            });
        }

        private void EmitWindowClose(int id, string by)
        {
            if (!Reports(EventGroups.Windows))
                return;

            channel.Emit("win_close", writer =>
            {
                writer.WriteNumber("id", id);
                writer.WriteString("by", by);
            });
        }

        private void EmitInventory()
        {
            if (!Reports(EventGroups.Inventory) || !GetInventoryEnabled() || Handler.GetInventory(0) is not { } inventory)
                return;

            List<KeyValuePair<int, Item>> items = inventory.Items
                .Where(entry => entry.Key >= 0 && !entry.Value.IsEmpty)
                .OrderBy(entry => entry.Key)
                .ToList();
            inventory.Items.TryGetValue(-1, out Item? cursor);
            Item? carried = cursor is { IsEmpty: false } ? cursor : null;
            byte held = GetCurrentSlot();

            long arrival = channel.LastArrival;
            long seq = channel.Emit("inv", writer =>
            {
                writer.WriteNumber("held", held);
                writer.WriteStartArray("items");
                foreach ((int slot, Item item) in items)
                    AgentBridgeItems.Write(writer, slot, item);
                writer.WriteEndArray();
                writer.WritePropertyName("cursor");
                if (carried is null)
                    writer.WriteNullValue();
                else
                    AgentBridgeItems.Write(writer, null, carried);
            });

            // Player inventory operations wait for the session's first view of it and must have been sent after it.
            if (seq > 0 && !inventoryReported)
            {
                inventoryReported = true;
                inventoryReportedArrival = arrival;
            }
        }

        private void CheckPlayers()
        {
            string[] players = GetOnlinePlayers()
                .Where(name => !string.IsNullOrEmpty(name))
                .Order(StringComparer.Ordinal)
                .Take(MaxPlayers)
                .ToArray();
            if (players.SequenceEqual(lastPlayers))
                return;

            lastPlayers = players;
            channel.Emit("players", writer =>
            {
                writer.WriteStartArray("list");
                foreach (string name in players)
                    writer.WriteStringValue(AgentBridgeItems.Plain(name, 64));
                writer.WriteEndArray();
            });
        }

        private void CheckPosition()
        {
            Location location = GetCurrentLocation();
            string dimension = World.GetDimension()?.Name ?? string.Empty;
            // Version 3: where the direction buttons point is part of the position, a turn is a change, and so is every
            // new block (a one-block step may move the feet less than a block: the walk stops near the block's middle).
            string? facing = movement?.Facing;
            if (lastPosition is { } last && last.Dim == dimension && facing == lastFacing
                && (movement is null
                    ? Math.Abs(last.X - location.X) < 1 && Math.Abs(last.Y - location.Y) < 1
                                                         && Math.Abs(last.Z - location.Z) < 1
                    : SameBlock(last.X, location.X) && SameBlock(last.Y, location.Y) && SameBlock(last.Z, location.Z)))
                return;

            lastPosition = (location.X, location.Y, location.Z, dimension);
            lastFacing = facing;
            channel.Emit("pos", writer =>
            {
                writer.WriteNumber("x", Math.Round(location.X, 2));
                writer.WriteNumber("y", Math.Round(location.Y, 2));
                writer.WriteNumber("z", Math.Round(location.Z, 2));
                writer.WriteString("dim", dimension);
                if (facing is not null)
                    writer.WriteString("facing", facing);
            });
        }

        /// <summary>Both coordinates are in the same block (version 3's <c>pos</c>: every new block is reported).</summary>
        internal static bool SameBlock(double before, double now) => Math.Floor(before) == Math.Floor(now);

        /// <summary>Version 3's <c>move</c> event: what the account does, and whether it sneaks.</summary>
        private void EmitMove(MoveState state)
        {
            if (!Reports(EventGroups.Movement))
                return;

            channel.Emit("move", writer =>
            {
                writer.WriteString("doing", state.Doing);
                if (state.Id is not null)
                    writer.WriteString("id", state.Id);
                if (state.To is { } to)
                {
                    writer.WriteStartObject("to");
                    writer.WriteNumber("x", to.X);
                    writer.WriteNumber("y", to.Y);
                    writer.WriteNumber("z", to.Z);
                    writer.WriteEndObject();
                }

                if (state.Name is not null)
                    writer.WriteString("name", state.Name);
                if (state.UntilMs is { } until)
                    writer.WriteNumber("until", until);
                if (state.Ended is not null)
                    writer.WriteString("ended", state.Ended);
                writer.WriteBoolean("sneak", state.Sneak);
            });
        }

        /// <summary>The client as the movement controller sees it (<see cref="IMoveHost"/>).</summary>
        private sealed class MoveHost(McClient client) : IMoveHost
        {
            public bool CanMove => client.GetTerrainEnabled() && client.GetEntityHandlingEnabled();
            public bool PositionKnown => client.LocationKnown;
            public Location Position => client.GetCurrentLocation();
            public float Yaw => client.GetYaw();
            public bool Walking => client.WalkingPath;
            public string SelfName => client.GetUsername();
            public void Walk(IReadOnlyList<Location> waypoints) => client.WalkWaypoints(waypoints);
            public void Halt() => client.HaltPath();
            public void Look(float yaw) => client.LookTowards(yaw);
            public void Jump() => client.BridgeJump();
            public void Sneak(bool on) => client.SetBridgeSneak(on);

            public bool TryFindPlayer(string name, out Location location)
            {
                location = default;
                Entity[] entities;
                try
                {
                    entities = client.GetEntities().Values.ToArray();
                }
                catch (InvalidOperationException)
                {
                    return false; // changed while copied: the next tick looks again
                }

                foreach (Entity entity in entities)
                {
                    if (entity.Type == EntityType.Player
                        && string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        location = entity.Location;
                        return true;
                    }
                }

                return false;
            }

            public IEnumerable<MovePlanner.Cell> Neighbors(MovePlanner.Cell cell)
            {
                World world = client.GetWorld();
                foreach (Location next in Movement.GetAvailableMoves(world, cell.Center, allowUnsafe: false))
                {
                    MovePlanner.Cell moved = MovePlanner.Cell.Of(next);
                    if (Loaded(world, moved))
                        yield return moved;
                }
            }

            public bool Stands(MovePlanner.Cell cell)
            {
                World world = client.GetWorld();
                Location center = cell.Center;
                return Loaded(world, cell)
                       && (Movement.IsOnGround(world, center) || world.GetBlock(center).Type.IsLiquid());
            }

            /// <summary>Only the terrain the server sent: an unknown chunk is never walked into.</summary>
            private static bool Loaded(World world, MovePlanner.Cell cell)
            {
                ChunkColumn? column = world.GetChunkColumn(cell.Center);
                return column is not null && column.FullyLoaded != false;
            }
        }

        /// <summary>The window type as the game's menu registry names it ("generic_9x6", "anvil", ...).</summary>
        internal static string WindowTypeName(ContainerType type) => type switch
        {
            ContainerType.Generic_9x1 => "generic_9x1",
            ContainerType.Generic_9x2 => "generic_9x2",
            ContainerType.Generic_9x3 => "generic_9x3",
            ContainerType.Generic_9x4 => "generic_9x4",
            ContainerType.Generic_9x5 => "generic_9x5",
            ContainerType.Generic_9x6 => "generic_9x6",
            ContainerType.Generic_3x3 => "generic_3x3",
            ContainerType.Crafter => "crafter_3x3",
            ContainerType.Anvil => "anvil",
            ContainerType.Beacon => "beacon",
            ContainerType.BlastFurnace => "blast_furnace",
            ContainerType.BrewingStand => "brewing_stand",
            ContainerType.Crafting => "crafting",
            ContainerType.Enchantment => "enchantment",
            ContainerType.Furnace => "furnace",
            ContainerType.Grindstone => "grindstone",
            ContainerType.Hopper => "hopper",
            ContainerType.Lectern => "lectern",
            ContainerType.Loom => "loom",
            ContainerType.Merchant => "merchant",
            ContainerType.ShulkerBox => "shulker_box",
            ContainerType.SmightingTable => "smithing",
            ContainerType.Smoker => "smoker",
            ContainerType.Cartography => "cartography_table",
            ContainerType.Stonecutter => "stonecutter",
            ContainerType.PlayerInventory => "player",
            _ => "unknown",
        };

        /// <summary>At most <c>rate</c> operations in any one second (a token bucket refilled continuously).</summary>
        internal sealed class OperationBudget(int rate)
        {
            private readonly Stopwatch clock = Stopwatch.StartNew();
            private double tokens = rate;
            private double lastSeconds;

            public bool TryTake()
            {
                double now = clock.Elapsed.TotalSeconds;
                tokens = Math.Min(rate, tokens + (now - lastSeconds) * rate);
                lastSeconds = now;
                if (tokens < 1)
                    return false;

                tokens -= 1;
                return true;
            }
        }
    }
}
