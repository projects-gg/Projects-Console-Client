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
    /// the player list; takes window clicks, an anvil name, window close, hotbar slot and snapshot requests.
    ///
    /// It never takes a text command: chat and commands keep going through stdin, built by the host from fixed
    /// templates. It only runs when the host passes both descriptors (MCC_AGENT_FD_OUT, MCC_AGENT_FD_IN), so the
    /// desktop launcher and every other user of the client are unaffected.
    /// </summary>
    public sealed class AgentBridge : ChatBot
    {
        /// <summary>Contract version, sent in "hello".</summary>
        public const int ProtocolVersion = 1;

        private const int MaxOperationsPerSecond = 10;
        private const int MaxOperationsPerTick = 4;
        private const int PlayersIntervalTicks = 20;
        private const int PositionIntervalTicks = 20;
        private const int InventoryDebounceTicks = 5;
        private const int MaxPlayers = 200;
        private const int MaxChatLength = 2048;
        private const int MaxSlotEventsPerUpdate = 12;
        private const int PlayerInventorySection = 36;

        private static bool helloSent;

        /// <summary>The host passed both descriptors.</summary>
        public static bool Enabled => AgentBridgeChannel.Configured;

        private readonly AgentBridgeChannel channel = AgentBridgeChannel.Instance;
        private readonly OperationBudget budget = new(MaxOperationsPerSecond);
        private bool active;
        private bool joined;
        private long staleBefore;
        private OpenWindow? window;
        private int closingWindowId = -1;
        private bool inventoryDirty;
        private int inventoryCooldown;
        private int ticks;
        private string[] lastPlayers = [];
        private (double X, double Y, double Z, string Dim)? lastPosition;

        /// <summary>The one window operations may act on: the last one the server opened.</summary>
        private sealed class OpenWindow(int id, ContainerType type, long openSeq, long openArrival, int total)
        {
            public int Id { get; } = id;
            public ContainerType Type { get; } = type;
            public long OpenSeq { get; set; } = openSeq;
            public long OpenArrival { get; } = openArrival;
            public int Total { get; } = total;
            public bool ContentSent { get; set; }
            public Dictionary<int, string> Fingerprints { get; set; } = new();
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
            ChatClicks.Collecting = true;

            if (!helloSent)
            {
                helloSent = true;
                channel.Emit("hello", writer =>
                {
                    writer.WriteNumber("v", ProtocolVersion);
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
            joined = true;

            channel.Emit("joined", writer =>
            {
                writer.WriteString("user", GetUsername());
                writer.WriteNumber("protocol", GetProtocolVersion());
            });
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (!active)
                return false;

            joined = false;
            window = null;
            DiscardQueuedOperations();
            channel.Emit("left", writer => writer.WriteString("reason", reason.ToString()));
            return false; // reporting only: AutoRelog and the host keep owning reconnects
        }

        public override void OnChatReceived(string text, ChatMessage message)
        {
            if (!active)
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
                        writer.WriteStringValue(ConsoleIO.KeepOnOneLine(click.Value));
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

            long seq = channel.Emit("win_open", writer =>
            {
                writer.WriteNumber("id", inventoryId);
                writer.WriteString("type", type);
                writer.WriteString("title", title);
                writer.WriteNumber("size", size);
                writer.WriteNumber("total", total);
            });

            window = new OpenWindow(inventoryId, container.Type, seq, arrival, total);
            closingWindowId = -1;

            // Some servers send the content before the window: report what is already known.
            if (container.Items.Count > 0)
                SyncWindow(window, container);
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            if (!active)
                return;

            if (inventoryId == 0)
                inventoryDirty = true;

            if (window is not null && inventoryId == window.Id && Handler.GetInventory(inventoryId) is { } container)
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
            if (active)
                channel.Emit("held", writer => writer.WriteNumber("slot", slot));
        }

        public override void Update()
        {
            if (!active)
                return;

            ticks++;
            if (joined)
                ExecuteOperations();
            else
                DiscardQueuedOperations();

            if (inventoryCooldown > 0)
                inventoryCooldown--;
            if (joined && inventoryDirty && inventoryCooldown == 0)
            {
                inventoryDirty = false;
                inventoryCooldown = InventoryDebounceTicks;
                EmitInventory();
            }

            if (joined && ticks % PlayersIntervalTicks == 0)
                CheckPlayers();
            if (joined && ticks % PositionIntervalTicks == PositionIntervalTicks / 2)
                CheckPosition();
        }

        private void ExecuteOperations()
        {
            for (int i = 0; i < MaxOperationsPerTick && channel.TryTakeOperation(out AgentOperation operation); i++)
            {
                string? error = Execute(operation);
                if (error is null)
                    channel.Emit("ack", writer =>
                    {
                        if (operation.Id is not null)
                            writer.WriteString("id", operation.Id);
                        writer.WriteString("op", operation.Op);
                    });
                else
                    channel.Refuse(operation.Id, operation.Op, error);
            }
        }

        /// <summary>Carry out one operation; null on success, else the reason it was refused.</summary>
        private string? Execute(AgentOperation operation)
        {
            if (operation.Arrival <= staleBefore)
                return "stale";
            if (!budget.TryTake())
                return "rate_limited";

            switch (operation.Op)
            {
                case AgentOperation.Click:
                {
                    if (CheckWindow(operation) is { } windowError)
                        return windowError;
                    if (operation.Slot >= window!.Total)
                        return "bad_slot";

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

                    return Handler.DoWindowAction(window.Id, operation.Slot, action.Value) ? null : "send_failed";
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
                    channel.Emit("held", writer => writer.WriteNumber("slot", slot));
                    return null;
                }

                case AgentOperation.Snapshot:
                {
                    if (operation.What == "inv")
                    {
                        EmitInventory();
                        return null;
                    }

                    if (window is null || Handler.GetInventory(window.Id) is not { } container)
                        return "no_window";

                    EmitWindowItems(window, container);
                    return null;
                }
            }

            return "unknown_op";
        }

        /// <summary>The operation names the open window: right id, right "ws", sent after the window opened.</summary>
        private string? CheckWindow(AgentOperation operation)
        {
            if (window is null)
                return "no_window";
            if (operation.Window != window.Id)
                return "wrong_window";
            if (operation.WindowSeq is null)
                return "bad_request";
            if (operation.WindowSeq != window.OpenSeq)
                return "wrong_window";
            if (operation.Arrival <= window.OpenArrival)
                return "stale";
            if (!window.ContentSent)
                return "not_ready";
            return null;
        }

        private void DiscardQueuedOperations()
        {
            staleBefore = channel.LastArrival;
            channel.DiscardOperations(operation => channel.Refuse(operation.Id, operation.Op, "stale"));
        }

        private void SyncWindow(OpenWindow open, Container container)
        {
            Dictionary<int, string> current = new();
            foreach ((int slot, Item item) in container.Items)
            {
                if (slot >= 0 && slot < open.Total && !item.IsEmpty)
                    current[slot] = AgentBridgeItems.Fingerprint(item);
            }

            if (!open.ContentSent)
            {
                open.ContentSent = true;
                open.Fingerprints = current;
                EmitWindowItems(open, container);
                return;
            }

            List<int> changed = current.Keys.Union(open.Fingerprints.Keys)
                .Where(slot => !current.TryGetValue(slot, out string? now)
                               || !open.Fingerprints.TryGetValue(slot, out string? before)
                               || now != before)
                .Order()
                .ToList();
            open.Fingerprints = current;
            if (changed.Count == 0)
                return;

            if (changed.Count > MaxSlotEventsPerUpdate)
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

        private void EmitWindowItems(OpenWindow open, Container container)
        {
            List<KeyValuePair<int, Item>> items = container.Items
                .Where(entry => entry.Key >= 0 && entry.Key < open.Total && !entry.Value.IsEmpty)
                .OrderBy(entry => entry.Key)
                .ToList();
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
            channel.Emit("win_close", writer =>
            {
                writer.WriteNumber("id", id);
                writer.WriteString("by", by);
            });
        }

        private void EmitInventory()
        {
            if (!GetInventoryEnabled() || Handler.GetInventory(0) is not { } inventory)
                return;

            List<KeyValuePair<int, Item>> items = inventory.Items
                .Where(entry => entry.Key >= 0 && !entry.Value.IsEmpty)
                .OrderBy(entry => entry.Key)
                .ToList();
            inventory.Items.TryGetValue(-1, out Item? cursor);
            Item? carried = cursor is { IsEmpty: false } ? cursor : null;
            byte held = GetCurrentSlot();

            channel.Emit("inv", writer =>
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
            if (lastPosition is { } last && last.Dim == dimension
                                         && Math.Abs(last.X - location.X) < 1
                                         && Math.Abs(last.Y - location.Y) < 1
                                         && Math.Abs(last.Z - location.Z) < 1)
                return;

            lastPosition = (location.X, location.Y, location.Z, dimension);
            channel.Emit("pos", writer =>
            {
                writer.WriteNumber("x", Math.Round(location.X, 2));
                writer.WriteNumber("y", Math.Round(location.Y, 2));
                writer.WriteNumber("z", Math.Round(location.Z, 2));
                writer.WriteString("dim", dimension);
            });
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
