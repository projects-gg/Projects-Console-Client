namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Whether the game would show a screen now (client 4.2.9): a window the server opened and has not closed, or the
    /// player's own inventory while the client clicks it. The game clicks a window only on its screen, opens the
    /// player's inventory to click it, and stands with every key up while a screen is open; the client keeps the own
    /// inventory open <see cref="OwnInventoryTicks"/> ticks after its last click (without a close packet, as before: a
    /// close would hand back what lies in its crafting grid).
    /// </summary>
    internal sealed class ScreenTracker
    {
        /// <summary>How long the player's own inventory stays open after a click on it (half a second).</summary>
        public const int OwnInventoryTicks = 10;

        private int? windowId; // the window the server opened
        private int ownInventoryTicks;

        /// <summary>A screen is open.</summary>
        public bool Open => windowId is not null || ownInventoryTicks > 0;

        /// <summary>The server opened a window: its screen replaces any other.</summary>
        public void ServerOpened(int id)
        {
            windowId = id;
            ownInventoryTicks = 0;
        }

        /// <summary>The server closed a window: the game closes the screen it shows, whatever its id.</summary>
        public void ServerClosed() => Clear();

        /// <summary>
        /// The client clicks or closes a window: it needs a screen. Without the server's window open, the player's own
        /// inventory opens for it (also for a window the client no longer shows).
        /// </summary>
        public void Using()
        {
            if (windowId is null)
                ownInventoryTicks = OwnInventoryTicks;
        }

        /// <summary>The client closed a window: its screen closes (closing another one leaves the open screen open).</summary>
        public void Closed(int id)
        {
            if (windowId == id)
                windowId = null;
            if (windowId is null)
                ownInventoryTicks = 0;
        }

        /// <summary>A client tick passed.</summary>
        public void Tick()
        {
            if (ownInventoryTicks > 0)
                ownInventoryTicks--;
        }

        /// <summary>No window is left (a join, a respawn, a transfer): no screen.</summary>
        public void Clear()
        {
            windowId = null;
            ownInventoryTicks = 0;
        }
    }
}
