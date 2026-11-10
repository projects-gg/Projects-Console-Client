using MinecraftClient.Inventory;

namespace MinecraftClient.Tests;

/// <summary>
/// McClient's windows as the tests replay them: OnInventoryOpen and OnWindowItems (the content, the window's cursor, then
/// the mirror of a window's player inventory part into window 0), OnSetSlot, OnInventoryClose and CloseInventory.
/// </summary>
internal sealed class TestWindows
{
    public const int TradeId = 7;
    public const int Hotbar0 = 36; // window 0's first hotbar slot
    public const int TradeHotbar0 = 81; // the same slot in a 6-row window (54 slots, then the player's 27 and 9)
    public const int TradeMain0 = 54; // window 0's slot 9 in that window

    public readonly Dictionary<int, Container> All = new();
    public readonly HashSet<int> WithFullContents = new();

    public Container Player => All[0];

    public TestWindows()
    {
        All[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory", new Dictionary<int, Item>
        {
            [Hotbar0] = Apples(3),
            [9] = new(ItemType.Diamond, 5, null),
            [-1] = new(ItemType.Bread, 2, null), // the cursor
        });
        WithFullContents.Add(0);
    }

    public static Item Apples(int count) => new(ItemType.EnchantedGoldenApple, count, null);

    /// <summary>The trade window's content: a filler pane, and the player's part (main inventory, then hotbar).</summary>
    public static Dictionary<int, Item> TradeContent(int apples) => new()
    {
        [0] = new(ItemType.GrayStainedGlassPane, 1, null),
        [TradeHotbar0] = Apples(apples),
        [TradeMain0] = new(ItemType.Diamond, 5, null),
    };

    public static string[] Slots(Container window) => window.Items
        .Where(entry => !entry.Value.IsEmpty)
        .OrderBy(entry => entry.Key)
        .Select(entry => $"{entry.Key}:{entry.Value.Type}x{entry.Value.Count}")
        .ToArray();

    /// <summary>
    /// OnInventoryOpen, then OnWindowItems with the window's content and the player's part as the server has it, and
    /// its cursor (from 1.17.1 the carried item of the window, empty for a new one).
    /// </summary>
    public Container Open(int id, Dictionary<int, Item> content, Item? cursor = null)
    {
        All[id] = new Container(id, ContainerType.Generic_9x6, "Takas") { Items = content };
        WithFullContents.Add(id);
        McClient.SyncPlayerInventorySlotsFromWindow(All, WithFullContents, All[id]);
        Cursor(cursor);
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

    /// <summary>OnSetSlot for the cursor (window -1, slot -1; from 1.21.2 Set Cursor Item).</summary>
    public void Cursor(Item? item)
    {
        if (item is null)
            Player.Items.Remove(-1);
        else
            Player.Items[-1] = item;
    }

    public List<int> ServerClose(int id) => McClient.CloseServerWindow(All, WithFullContents, id, out _);

    public List<int> ServerClose(int id, out bool cursorDropped) =>
        McClient.CloseServerWindow(All, WithFullContents, id, out cursorDropped);

    public bool ClientClose(int id) => McClient.CloseClientWindow(All, WithFullContents, id);
}

/// <summary>
/// Client 4.2.10: a server's Close Container is taken as the game takes it. The player's inventory (window 0) stays as it
/// is: Paper sends a close of 0 whenever a plugin calls closeInventory() with no window open, and the server sends again
/// only the slots its close changes. Production, kz1 (TradeSystem 2.6.3, which calls closeInventory() around a trade's
/// end): the trade went through, then the client's window 0 read empty and the agent's proof of the sale (3 apples
/// before, 2 after) failed. The cursor is the open window's: it goes with a window that closes, and stays otherwise.
/// </summary>
public sealed class ServerCloseTests
{
    private const int TradeId = TestWindows.TradeId;

    [Fact]
    public void AServerCloseOfThePlayersInventoryKeepsItsItemsAndCursor()
    {
        TestWindows windows = new();
        string[] before = TestWindows.Slots(windows.Player);

        Assert.Empty(windows.ServerClose(0, out bool cursorDropped)); // no window for the bots to close

        Assert.False(cursorDropped);
        Assert.Equal(before, TestWindows.Slots(windows.Player));
        Assert.Equal(ItemType.Bread, windows.Player.Items[-1].Type);
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
    }

    [Fact]
    public void AServerCloseOfAWindowRemovesItAndKeepsThePlayersInventoryWithTheMirroredPart()
    {
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));
        windows.SetSlot(TradeId, TestWindows.TradeHotbar0, TestWindows.Apples(2)); // one apple goes into the trade

        Assert.Equal(2, windows.Player.Items[TestWindows.Hotbar0].Count); // mirrored into window 0

        Assert.Equal(new[] { TradeId }, windows.ServerClose(TradeId));

        Assert.False(windows.All.ContainsKey(TradeId));
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
        Assert.Equal(new[] { "9:Diamondx5", "36:EnchantedGoldenApplex2" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void TheSaleStaysInTheInventoryWhenTheServerClosesTheTradeAndThenThePlayersInventory()
    {
        // The live order: the trade window, the item taken, the server's close of the trade, closeInventory() again.
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));
        windows.SetSlot(TradeId, TestWindows.TradeHotbar0, TestWindows.Apples(2));
        windows.ServerClose(TradeId);

        Assert.Empty(windows.ServerClose(0));

        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(new[] { "9:Diamondx5", "36:EnchantedGoldenApplex2" }, TestWindows.Slots(windows.Player));

        // A window the server opens next mirrors as before.
        windows.Open(TradeId + 1, TestWindows.TradeContent(apples: 2));
        windows.SetSlot(TradeId + 1, TestWindows.TradeHotbar0, TestWindows.Apples(1));
        Assert.Equal(1, windows.Player.Items[TestWindows.Hotbar0].Count);
    }

    [Fact]
    public void AServerCloseOfThePlayersInventoryClosesAWindowStillListedWithItsCursor()
    {
        // The server names the window it has open, 0 when none is: a window the client still lists is not open there
        // (Paper opens a plugin's window over another without a close), and the game's close goes back to the player's
        // inventory whatever the id.
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));
        windows.SetSlot(TradeId, TestWindows.TradeHotbar0, TestWindows.Apples(2));
        windows.Open(TradeId + 1, TestWindows.TradeContent(apples: 2), cursor: TestWindows.Apples(1));

        Assert.Equal(new[] { TradeId, TradeId + 1 }, windows.ServerClose(0, out bool cursorDropped));

        Assert.True(cursorDropped);
        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
        Assert.Equal(new[] { "9:Diamondx5", "36:EnchantedGoldenApplex2" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void AServerCloseOfAWindowDropsTheCursorItHeld()
    {
        // The cursor was the window's; the server puts its item back into the inventory (Set Player Inventory) and says
        // nothing of the cursor.
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));
        windows.Cursor(TestWindows.Apples(1)); // the bot picked up one apple
        windows.SetSlot(TradeId, TestWindows.TradeHotbar0, TestWindows.Apples(2));

        windows.ServerClose(TradeId, out bool cursorDropped);

        Assert.True(cursorDropped);
        Assert.False(windows.Player.Items.ContainsKey(-1));
        Assert.Equal(new[] { "9:Diamondx5", "36:EnchantedGoldenApplex2" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void AServerCloseOfAnUnlistedWindowIsStillReportedAndKeepsTheCursor()
    {
        TestWindows windows = new();

        Assert.Equal(new[] { TradeId }, windows.ServerClose(TradeId, out bool cursorDropped));

        Assert.False(cursorDropped);
        Assert.Equal(new[] { 0 }, windows.All.Keys.Order());
        Assert.Equal(new[] { "-1:Breadx2", "9:Diamondx5", "36:EnchantedGoldenApplex3" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void TheClientsCloseOfAWindowDropsItsCursorAndKeepsThePlayersInventory()
    {
        // The game drops the window's menu with its cursor (LocalPlayer.closeContainer); the server puts the item back.
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3), cursor: TestWindows.Apples(1));

        Assert.True(windows.ClientClose(TradeId));

        Assert.False(windows.All.ContainsKey(TradeId));
        Assert.Equal(new[] { 0 }, windows.WithFullContents.Order());
        Assert.Equal(new[] { "9:Diamondx5", "36:EnchantedGoldenApplex3" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void TheClientsCloseOfThePlayersInventoryKeepsItsCursor()
    {
        // The game keeps its own inventory's cursor (only a server's player puts it back); the server sends it again.
        TestWindows windows = new();
        string[] before = TestWindows.Slots(windows.Player);

        Assert.False(windows.ClientClose(0));

        Assert.Equal(before, TestWindows.Slots(windows.Player));
        Assert.Equal(ItemType.Bread, windows.Player.Items[-1].Type);
    }
}
