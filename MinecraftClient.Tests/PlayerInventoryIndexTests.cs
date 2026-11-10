using MinecraftClient.Inventory;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.10: Set Player Inventory (from 1.21.2) and window -2's Set Slot (before) name a player inventory index, which
/// the game sets in its inventory (Inventory.setItem): window 0 shows it where InventoryMenu lays it out, and an open
/// window's player part too. The server sends it when it puts an item back into the inventory (a closed window's cursor
/// or crafting grid, Inventory.placeItemBackInInventory) or picks a block. Before, the client wrote the index as a window 0
/// slot: an item put back in the hotbar showed in the crafting grid or an armor slot.
/// </summary>
public sealed class PlayerInventoryIndexTests
{
    private const int TradeId = TestWindows.TradeId;

    [Theory]
    [InlineData(0, 36)] // hotbar
    [InlineData(8, 44)]
    [InlineData(9, 9)] // main inventory
    [InlineData(35, 35)]
    [InlineData(36, 8)] // feet
    [InlineData(37, 7)] // legs
    [InlineData(38, 6)] // chest
    [InlineData(39, 5)] // head
    [InlineData(40, 45)] // off hand
    public void AnIndexLandsOnTheGamesWindowSlot(int index, int windowSlot)
    {
        Assert.True(McClient.TryGetPlayerInventoryWindowSlot(index, out int slot));
        Assert.Equal(windowSlot, slot);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(41)] // body armor
    [InlineData(42)] // saddle
    [InlineData(100)]
    public void AnIndexOutsideWindow0IsIgnored(int index)
    {
        TestWindows windows = new();
        string[] before = TestWindows.Slots(windows.Player);

        Assert.False(McClient.TryGetPlayerInventoryWindowSlot(index, out _));
        Assert.Empty(McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, index, TestWindows.Apples(1)));
        Assert.Equal(before, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void AnItemPutBackInTheHotbarShowsInTheHotbarNotTheCraftingGrid()
    {
        TestWindows windows = new();

        Assert.Empty(McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 1, TestWindows.Apples(1)));

        Assert.Equal(new[] { "-1:Breadx2", "9:Diamondx5", "36:EnchantedGoldenApplex3", "37:EnchantedGoldenApplex1" },
            TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void ArmorAndTheOffHandShowOnlyInWindow0()
    {
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));
        string[] trade = TestWindows.Slots(windows.All[TradeId]);

        Assert.Empty(McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 39, new Item(ItemType.IronHelmet, 1, null)));
        Assert.Empty(McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 40, new Item(ItemType.Shield, 1, null)));

        Assert.Equal(ItemType.IronHelmet, windows.Player.Items[5].Type);
        Assert.Equal(ItemType.Shield, windows.Player.Items[45].Type);
        Assert.Equal(trade, TestWindows.Slots(windows.All[TradeId]));
    }

    [Fact]
    public void AnOpenWindowsPlayerPartShowsItTooAndItsNextSlotDoesNotUndoIt()
    {
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 2));

        Assert.Equal(new[] { TradeId },
            McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 0, TestWindows.Apples(3)));
        Assert.Equal(new[] { TradeId },
            McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 10, new Item(ItemType.Emerald, 4, null)));

        Assert.Equal(3, windows.All[TradeId].Items[TestWindows.TradeHotbar0].Count);
        Assert.Equal(ItemType.Emerald, windows.All[TradeId].Items[TestWindows.TradeMain0 + 1].Type);
        Assert.NotSame(windows.Player.Items[TestWindows.Hotbar0], windows.All[TradeId].Items[TestWindows.TradeHotbar0]);

        // The window's next slot mirrors its player part into window 0: the item stays.
        windows.SetSlot(TradeId, 0, null);
        Assert.Equal(new[] { "9:Diamondx5", "10:Emeraldx4", "36:EnchantedGoldenApplex3" }, TestWindows.Slots(windows.Player));
    }

    [Fact]
    public void AnEmptyItemEmptiesTheSlotInWindow0AndTheWindow()
    {
        TestWindows windows = new();
        windows.Open(TradeId, TestWindows.TradeContent(apples: 3));

        Assert.Equal(new[] { TradeId }, McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 0, null));

        Assert.False(windows.Player.Items.ContainsKey(TestWindows.Hotbar0));
        Assert.False(windows.All[TradeId].Items.ContainsKey(TestWindows.TradeHotbar0));
    }

    [Fact]
    public void AWindowWhoseContentHasNotComeIsLeftToIt()
    {
        TestWindows windows = new();
        windows.All[TradeId] = new Container(TradeId, ContainerType.Generic_9x6, "Takas"); // opened, no content yet

        Assert.Empty(McClient.SetPlayerInventoryIndex(windows.All, windows.WithFullContents, 0, TestWindows.Apples(4)));

        Assert.Empty(windows.All[TradeId].Items);
        Assert.Equal(4, windows.Player.Items[TestWindows.Hotbar0].Count);
    }
}
