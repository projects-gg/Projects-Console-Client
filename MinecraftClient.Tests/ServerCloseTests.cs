using MinecraftClient.Inventory;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.10: a server's Close Container is taken as the game takes it. The player's inventory (window 0) stays as it
/// is, its cursor too: Paper sends a close of 0 whenever a plugin calls closeInventory() with no window open, and the
/// server sends again only the slots its close changes. Production, kz1 (TradeSystem 2.6.3, which calls closeInventory()
/// around a trade's end): the trade went through, then the client's window 0 read empty and the agent's proof of the
/// sale (3 apples before, 2 after) failed.
///
/// These tests replay what McClient does with its windows: OnWindowItems and OnSetSlot (the content, then the mirror of a
/// window's player inventory part into window 0) and OnInventoryClose (<see cref="McClient.CloseServerWindow"/>).
/// </summary>
public sealed class ServerCloseTests
{
    private const int TradeId = 7;
    private const int Hotbar0 = 36; // window 0's first hotbar slot
    private const int TradeHotbar0 = 81; // the same slot in a 6-row window (54 slots, then the player's 27 and 9)

    private sealed class Windows
    {
        public readonly Dictionary<int, Container> All = new();
        public readonly HashSet<int> WithFullContents = new();

        public Container Player => All[0];

        public Windows()
        {
            All[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory", new Dictionary<int, Item>
            {
                [Hotbar0] = Apples(3),
                [9] = new(ItemType.Diamond, 5, null),
                [-1] = new(ItemType.Bread, 2, null), // the cursor
            });
            WithFullContents.Add(0);
        }

        /// <summary>OnInventoryOpen, then OnWindowItems with the window's content and the player's part as the server has it.</summary>
        public Container Open(int id, Dictionary<int, Item> content)
        {
            All[id] = new Container(id, ContainerType.Generic_9x6, "Takas");
            All[id].Items = content;
            WithFullContents.Add(id);
            McClient.SyncPlayerInventorySlotsFromWindow(All, WithFullContents, All[id]);
            return All[id];
        }

        /// <summary>OnSetSlot for a window's slot.</summary>
        public void SetSlot(int id, int slot, Item? item)
        {
            if (item is null)
                All[id].Items.Remove(slot);
            else
                All[id].Items[slot] = item;
            McClient.SyncPlayerInventorySlotsFromWindow(All, WithFullContents, All[id]);
        }

        public List<int> ServerClose(int id) => McClient.CloseServerWindow(All, WithFullContents, id);
    }

    private static Item Apples(int count) => new(ItemType.EnchantedGoldenApple, count, null);

    /// <summary>The trade window's content: a filler pane, and the player's part (main inventory, then hotbar).</summary>
    private static Dictionary<int, Item> TradeContent(int apples) => new()
    {
        [0] = new(ItemType.GrayStainedGlassPane, 1, null),
        [TradeHotbar0] = Apples(apples),
        [54] = new(ItemType.Diamond, 5, null), // window 0's slot 9
    };

    private static string[] Slots(Container window) => window.Items
        .Where(entry => !entry.Value.IsEmpty)
        .OrderBy(entry => entry.Key)
        .Select(entry => $"{entry.Key}:{entry.Value.Type}x{entry.Value.Count}")
        .ToArray();

    [Fact]
    public void AServerCloseOfThePlayersInventoryKeepsItsItemsAndCursor()
    {
        Windows windows = new();
        string[] before = Slots(windows.Player);

        Assert.Empty(windows.ServerClose(0)); // no window for the bots to close

        Assert.Equal(before, Slots(windows.Player));
        Assert.Equal(ItemType.Bread, windows.Player.Items[-1].Type);
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
    }

    [Fact]
    public void AServerCloseOfAWindowRemovesItAndKeepsThePlayersInventoryWithTheMirroredPart()
    {
        Windows windows = new();
        windows.Open(TradeId, TradeContent(apples: 3));
        windows.SetSlot(TradeId, TradeHotbar0, Apples(2)); // the server takes one apple into the trade

        Assert.Equal(2, windows.Player.Items[Hotbar0].Count); // mirrored into window 0

        Assert.Equal(new[] { TradeId }, windows.ServerClose(TradeId));

        Assert.False(windows.All.ContainsKey(TradeId));
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
        Assert.Equal(new[] { "-1:Breadx2", "9:Diamondx5", "36:EnchantedGoldenApplex2" }, Slots(windows.Player));
    }

    [Fact]
    public void TheSaleStaysInTheInventoryWhenTheServerClosesTheTradeAndThenThePlayersInventory()
    {
        // The live order: the trade window, the item taken, the server's close of the trade, closeInventory() again.
        Windows windows = new();
        windows.Open(TradeId, TradeContent(apples: 3));
        windows.SetSlot(TradeId, TradeHotbar0, Apples(2));
        windows.ServerClose(TradeId);

        Assert.Empty(windows.ServerClose(0));

        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(new[] { "-1:Breadx2", "9:Diamondx5", "36:EnchantedGoldenApplex2" }, Slots(windows.Player));

        // A window the server opens next mirrors as before.
        windows.Open(TradeId + 1, TradeContent(apples: 2));
        windows.SetSlot(TradeId + 1, TradeHotbar0, Apples(1));
        Assert.Equal(1, windows.Player.Items[Hotbar0].Count);
    }

    [Fact]
    public void AServerCloseOfThePlayersInventoryClosesAWindowStillListed()
    {
        // The server names the window it has open, 0 when none is: a window the client still lists is not open there
        // (Paper opens a plugin's window over another without a close), and the game's close goes back to the player's
        // inventory whatever the id.
        Windows windows = new();
        windows.Open(TradeId, TradeContent(apples: 3));
        windows.SetSlot(TradeId, TradeHotbar0, Apples(2));
        windows.Open(TradeId + 1, TradeContent(apples: 2));

        Assert.Equal(new[] { TradeId, TradeId + 1 }, windows.ServerClose(0));

        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
        Assert.Equal(new[] { "-1:Breadx2", "9:Diamondx5", "36:EnchantedGoldenApplex2" }, Slots(windows.Player));
    }

    [Fact]
    public void AServerCloseOfAnUnlistedWindowIsStillReported()
    {
        Windows windows = new();

        Assert.Equal(new[] { TradeId }, windows.ServerClose(TradeId));

        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(3, Slots(windows.Player).Length);
    }
}
