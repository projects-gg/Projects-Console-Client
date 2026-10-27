using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;

namespace MinecraftClient.Tests;

/// <summary>
/// A click a server menu refuses (FEAT-E B3, a local Canvas 26.3, 28 Oct 2026): the host's view kept the clicked slot
/// empty, and the cursor empty, until the menu opened again. The client did take the server's correction; the bridge
/// did not report it. The Worker asks for a snapshot after every move, and the bridge answered it at the next tick,
/// before the server's answer, with the client's guess, but kept finding later changes against the content it had
/// reported before the click. The correction was no change against that, so it was never reported.
///
/// These tests replay what the client's window goes through, in the order the run saw it: the content, the client's
/// guess for the click (McClient.DoWindowAction), the snapshot, then what a 26.3 server sends for a click a plugin
/// cancels (it sends nothing of its own; broadcastChanges finds the slot and the cursor differ from what the click
/// said: ContainerSetSlot with the slot's item, then SetCursorItem empty), and the report after the settle time.
/// </summary>
public sealed class AgentBridgeRefusedClickTests
{
    private const int WindowId = 2;
    private const int Total = 63; // 3 rows and the player inventory section

    /// <summary>The host's view of the window, built from the bridge's win_items and win_slot events.</summary>
    private sealed class HostView
    {
        private readonly Dictionary<int, string> slots = new();

        public string[] Slots => Sorted(slots);

        /// <summary>A report (AgentBridge.SyncWindow): the whole content, or the slots that changed.</summary>
        public List<int>? Report(AgentBridge.OpenWindow open, Container window)
        {
            List<int>? changed = open.Changes(window, AgentBridge.MaxSlotEventsPerUpdate);
            if (changed is null)
            {
                Whole(open, window);
                return null;
            }

            foreach (int slot in changed)
            {
                if (window.Items.TryGetValue(slot, out Item? item) && !item.IsEmpty)
                    slots[slot] = AgentBridgeItems.Fingerprint(item);
                else
                    slots.Remove(slot);
            }

            return changed;
        }

        /// <summary>win_items: a report's whole content, or the answer to a <c>snapshot</c>.</summary>
        public void Whole(AgentBridge.OpenWindow open, Container window)
        {
            slots.Clear();
            foreach ((int slot, Item item) in open.Whole(window))
                slots[slot] = AgentBridgeItems.Fingerprint(item);
        }
    }

    private static Container Kitler() => new(WindowId, ContainerType.Generic_9x3, "Kitler", new Dictionary<int, Item>
    {
        [11] = new(ItemType.Diamond, 1, null),
        [13] = new(ItemType.IronSword, 1, null),
        [15] = new(ItemType.Bread, 3, null),
    });

    private static AgentBridge.OpenWindow Opened() => new(WindowId, ContainerType.Generic_9x3, openSeq: 57, openArrival: 1, Total);

    private static string[] Sorted(Dictionary<int, string> slots) =>
        slots.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value}").ToArray();

    private static string[] Server(Container window) => Sorted(window.Items
        .Where(entry => !entry.Value.IsEmpty)
        .ToDictionary(entry => entry.Key, entry => AgentBridgeItems.Fingerprint(entry.Value)));

    /// <summary>
    /// McClient.DoWindowAction, a left click with an empty cursor: the slot's item goes on the cursor (the player
    /// inventory's slot -1, always reported whole in <c>inv</c>), the slot is empty.
    /// </summary>
    private static void Pickup(Container window, int slot) => window.Items.Remove(slot);

    /// <summary>What the server says for the refused click: ContainerSetSlot with the slot's item (McClient.OnSetSlot).</summary>
    private static void Refused(Container window, int slot, Item serverItem) => window.Items[slot] = serverItem;

    [Fact]
    public void ASnapshotBetweenTheClickAndTheServersAnswerDoesNotHideTheAnswer()
    {
        Container window = Kitler();
        string[] server = Server(window);
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();

        Assert.Null(host.Report(open, window)); // the content, as win_items
        Assert.Equal(server, host.Slots);

        Pickup(window, 11);
        host.Whole(open, window); // the next tick: the Worker's snapshot shows the client's guess
        Assert.DoesNotContain(host.Slots, slot => slot.StartsWith("11:", StringComparison.Ordinal));

        Refused(window, 11, new Item(ItemType.Diamond, 1, null));

        Assert.Equal(new[] { 11 }, host.Report(open, window)); // the settled report
        Assert.Equal(server, host.Slots);
    }

    [Fact]
    public void AnAnswerBeforeTheSnapshotIsInTheSnapshot()
    {
        Container window = Kitler();
        string[] server = Server(window);
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();
        host.Report(open, window);

        Pickup(window, 11);
        Refused(window, 11, new Item(ItemType.Diamond, 1, null));
        host.Whole(open, window);

        Assert.Empty(host.Report(open, window)!);
        Assert.Equal(server, host.Slots);
    }

    [Fact]
    public void AnAnswerAfterTheSettledReportFollowsAsAChange()
    {
        Container window = Kitler();
        string[] server = Server(window);
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();
        host.Report(open, window);

        Pickup(window, 11);
        host.Whole(open, window);
        Assert.Empty(host.Report(open, window)!); // a slow server: nothing yet, the guess stands

        Refused(window, 11, new Item(ItemType.Diamond, 1, null));

        Assert.Equal(new[] { 11 }, host.Report(open, window));
        Assert.Equal(server, host.Slots);
    }

    [Fact]
    public void WithoutASnapshotTheAnswerIsNoChangeAndTheViewStays()
    {
        Container window = Kitler();
        string[] server = Server(window);
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();
        host.Report(open, window);

        Pickup(window, 11);
        Refused(window, 11, new Item(ItemType.Diamond, 1, null));

        Assert.Empty(host.Report(open, window)!);
        Assert.Equal(server, host.Slots);
    }

    [Fact]
    public void AClickTheServerTakesInSilenceIsReportedAsGuessed()
    {
        Container window = Kitler();
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();
        host.Report(open, window);

        Pickup(window, 11);
        Assert.Equal(new[] { 11 }, host.Report(open, window));

        Assert.Equal(Server(window), host.Slots);
        Assert.DoesNotContain(host.Slots, slot => slot.StartsWith("11:", StringComparison.Ordinal));
    }

    [Fact]
    public void ASnapshotBeforeTheContentDoesNotCountAsTheContent()
    {
        Container window = new(WindowId, ContainerType.Generic_9x3, "Kitler");
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();

        host.Whole(open, window); // a snapshot before the server filled the window
        Assert.False(open.ContentSent); // clicks still wait for the content (not_ready)

        foreach ((int slot, Item item) in Kitler().Items)
            window.Items[slot] = item;

        Assert.Null(host.Report(open, window)); // the content goes out whole
        Assert.True(open.ContentSent);
        Assert.Equal(Server(window), host.Slots);
    }

    [Fact]
    public void MoreThanTwelveChangesGoOutWhole()
    {
        Container window = Kitler();
        AgentBridge.OpenWindow open = Opened();
        HostView host = new();
        host.Report(open, window);

        for (int slot = 30; slot < 43; slot++)
            window.Items[slot] = new Item(ItemType.Dirt, 64, null);

        Assert.Null(host.Report(open, window));
        Assert.Equal(Server(window), host.Slots);
        Assert.Empty(host.Report(open, window)!);
    }
}
