using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.9: while a screen is open (a window the server opened, the player's own inventory while the client clicks
/// it) every key is up for the server, as the game releases them when a screen opens, and a window click or close goes
/// only once the server knows. Grim MultiActionsC cancels a click made with a movement, sneak or sprint key down (and
/// MultiActionsD flags such a close); production: an AFK account's trade clicks, one a second for 33 seconds, while
/// AntiAFK held its sneak key ("MultiActionsC x15, sneaking=true").
/// </summary>
public sealed class ScreenKeysTests
{
    private const int Entity = 7;

    /// <summary>The packets the keys sent, in order, as short names.</summary>
    private sealed class Wire
    {
        public readonly List<string> Sent = new();
        public bool Play = true;

        public PlayerKeys Keys(int version) => new(version, Send);

        private bool Send(PacketTypesOut packet, byte[] data)
        {
            if (!Play)
                return false;

            Sent.Add(packet switch
            {
                PacketTypesOut.SteerVehicle => $"input {data[0]:x2}",
                PacketTypesOut.EntityAction => $"action {data[1]}", // entity id, command, jump boost: one byte each here
                PacketTypesOut.ClickWindow => "click",
                PacketTypesOut.CloseWindow => "close",
                _ => packet.ToString(),
            });
            return true;
        }
    }

    private static readonly byte Forward = Protocol18Handler.PlayerInputForward;
    private static readonly byte[] Click = [1, 0, 0, 0];

    private static void Sneak(PlayerKeys keys, bool on) =>
        keys.EntityAction(Entity, (int)(on ? EntityActionType.StartSneaking : EntityActionType.StopSneaking));

    private static void Sprint(PlayerKeys keys, bool on) =>
        keys.EntityAction(Entity, (int)(on ? EntityActionType.StartSprinting : EntityActionType.StopSprinting));

    [Fact]
    public void OpeningAScreenReleasesEveryKeyAtOnce()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        Sneak(keys, true);
        keys.SetMovementKeys(Forward); // the tick's move
        keys.EndTick();

        keys.SetScreenOpen(true); // the server's Open Screen, between two ticks

        Assert.Equal(["input 21", "input 00"], wire.Sent);
    }

    [Fact]
    public void AClickAfterAWindowOpenedWhileSneakingFollowsAReleasedInput()
    {
        // The production case: no terrain (no moves), AntiAFK's sneak key down, the trade window opens, a click a second
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        Sneak(keys, true);
        keys.Flush();
        keys.EndTick();

        keys.SetScreenOpen(true);
        keys.WindowPacket(PacketTypesOut.ClickWindow, Click);
        for (int tick = 0; tick < 20; tick++)
        {
            keys.StartTick();
            Sneak(keys, tick % 2 == 0); // a sneak toggle meanwhile (a slab overhead, a command) stays up
            keys.Flush();
            keys.EndTick();
        }

        keys.WindowPacket(PacketTypesOut.ClickWindow, Click);

        Assert.Equal(["input 20", "input 00", "click", "click"], wire.Sent);
    }

    [Fact]
    public void AClickInTheTickTheKeysWentWaitsForTheNextTicksReleasedInput()
    {
        // A walk sent its forward key this tick; a click on the player's own inventory comes after the move
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        keys.SetMovementKeys(Forward);
        keys.SetScreenOpen(true);
        Assert.True(keys.WindowPacket(PacketTypesOut.ClickWindow, Click));
        keys.Flush();
        Assert.Equal(["input 01"], wire.Sent); // no second Player Input in a tick (Grim BadPacketsZ)

        keys.EndTick();
        keys.StartTick();
        keys.SetMovementKeys(0); // the move while the screen is open has no key down

        Assert.Equal(["input 01", "input 00", "click"], wire.Sent);
    }

    [Fact]
    public void KeysStayUpUntilTheScreenClosesThenGoDownWithTheNextTick()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        Sneak(keys, true);
        keys.SetMovementKeys(Forward);
        keys.EndTick();
        keys.SetScreenOpen(true);
        keys.SetMovementKeys(Forward);
        keys.Flush();
        keys.EndTick();
        keys.SetMovementKeys(Forward);
        keys.EndTick();

        keys.SetScreenOpen(false);
        Assert.Equal(["input 21", "input 00"], wire.Sent); // nothing between: the next tick reads the keys again

        keys.SetMovementKeys(Forward);
        Assert.Equal(["input 21", "input 00", "input 21"], wire.Sent);
    }

    [Fact]
    public void ACloseAlsoFollowsAReleasedInputAndTheKeysGoDownAfter()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        keys.SetMovementKeys(Forward);
        keys.EndTick();

        keys.WindowPacket(PacketTypesOut.CloseWindow, [0]);
        keys.Flush();
        keys.EndTick();
        keys.SetMovementKeys(Forward);

        Assert.Equal(["input 01", "input 00", "close", "input 01"], wire.Sent);
    }

    [Fact]
    public void AClickWithEveryKeyUpGoesAtOnce()
    {
        foreach (int version in new[] { Protocol18Handler.MC_1_12_2_Version, Protocol18Handler.MC_26_1_Version })
        {
            Wire wire = new();
            PlayerKeys keys = wire.Keys(version);
            keys.SetMovementKeys(0);
            keys.WindowPacket(PacketTypesOut.ClickWindow, Click);
            Assert.Equal(["click"], wire.Sent);
        }
    }

    [Fact]
    public void BeforeOneTwentyOneSixSneakingStopsBeforeTheClickAndStartsAgainAfter()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_1_12_2_Version);
        Sneak(keys, true);
        keys.SetScreenOpen(true);
        keys.WindowPacket(PacketTypesOut.ClickWindow, Click);
        keys.WindowPacket(PacketTypesOut.CloseWindow, [1]);
        keys.SetScreenOpen(false);
        keys.Flush();

        Assert.Equal(["action 0", "action 1", "click", "close", "action 0"], wire.Sent);
    }

    [Fact]
    public void From1212To1215BothTheInputAndTheCommandAreReleased()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_1_21_4_Version);
        Sneak(keys, true);
        keys.Flush();
        keys.EndTick();
        keys.SetScreenOpen(true);
        keys.WindowPacket(PacketTypesOut.ClickWindow, Click);

        Assert.Equal(["action 0", "input 20", "input 00", "action 1", "click"], wire.Sent);
    }

    [Fact]
    public void SprintingStopsBeforeAClickAndStartsAgainAfter()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_1_20_4_Version);
        Sprint(keys, true);
        keys.WindowPacket(PacketTypesOut.ClickWindow, Click); // a click without a screen the client shows
        keys.Flush();
        Assert.Equal(["action 3", "action 4", "click", "action 3"], wire.Sent);

        // From 1.21.6 the sprint commands moved down by two
        Wire modern = new();
        PlayerKeys keys26 = modern.Keys(Protocol18Handler.MC_26_1_Version);
        Sprint(keys26, true);
        keys26.SetScreenOpen(true);
        Assert.Equal(["action 1", "action 2"], modern.Sent);
    }

    [Fact]
    public void ASneakCommandGoesOnlyWhenItChangesWhatTheServerKnows()
    {
        // Grim BadPacketsG (before 1.21.2): a second "start sneaking" or a "stop" while not sneaking
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_1_12_2_Version);
        Sneak(keys, false);
        Sneak(keys, true);
        Sneak(keys, true);
        Sneak(keys, false);

        Assert.Equal(["action 0", "action 1"], wire.Sent);
    }

    [Fact]
    public void OtherActionsGoAtOnceAlsoWhileAScreenIsOpen()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_1_12_2_Version);
        keys.SetScreenOpen(true);
        keys.EntityAction(Entity, (int)EntityActionType.LeaveBed);

        Assert.Equal(["action 2"], wire.Sent);
    }

    [Fact]
    public void ANewPlayerSendsItsKeysAgainAndForgetsTheClicksOfAGoneWindow()
    {
        Wire wire = new();
        PlayerKeys keys = wire.Keys(Protocol18Handler.MC_26_1_Version);
        keys.SetMovementKeys(Forward);
        keys.EndTick();

        wire.Play = false; // a proxy's server switch: the connection configures the client
        keys.WindowPacket(PacketTypesOut.ClickWindow, Click);
        keys.Flush();
        keys.EndTick();
        keys.NewPlayer(); // Join Game on the next server
        wire.Play = true;
        keys.SetMovementKeys(Forward);
        keys.Flush();

        Assert.Equal(["input 01", "input 01"], wire.Sent);
    }
}

/// <summary>Client 4.2.9: when the game would show a screen, which keeps every key up (<see cref="ScreenTracker"/>).</summary>
public sealed class ScreenTrackerTests
{
    [Fact]
    public void AServerWindowIsAScreenUntilTheServerClosesIt()
    {
        ScreenTracker screen = new();
        Assert.False(screen.Open);

        screen.ServerOpened(3);
        for (int tick = 0; tick < 1000; tick++)
            screen.Tick();
        Assert.True(screen.Open);

        screen.ServerClosed();
        Assert.False(screen.Open);
    }

    [Fact]
    public void TheOwnInventoryOpensForAClickAndClosesHalfASecondAfterTheLastOne()
    {
        ScreenTracker screen = new();
        screen.Using(); // a click on window 0
        for (int tick = 0; tick < ScreenTracker.OwnInventoryTicks - 1; tick++)
            screen.Tick();
        Assert.True(screen.Open);

        screen.Using(); // another click: half a second again
        for (int tick = 0; tick < ScreenTracker.OwnInventoryTicks - 1; tick++)
            screen.Tick();
        Assert.True(screen.Open);
        screen.Tick();
        Assert.False(screen.Open);
    }

    [Fact]
    public void AClickOnTheOpenWindowKeepsItsScreenUntilItCloses()
    {
        ScreenTracker screen = new();
        screen.ServerOpened(2);
        screen.Using();
        screen.Closed(5); // another, older window
        Assert.True(screen.Open);

        screen.Closed(2);
        Assert.False(screen.Open);
    }

    [Fact]
    public void ClosingTheOwnInventoryClosesItsScreenAndAJoinClosesEveryScreen()
    {
        ScreenTracker screen = new();
        screen.Using();
        screen.Closed(0);
        Assert.False(screen.Open);

        screen.ServerOpened(1);
        screen.Clear();
        Assert.False(screen.Open);
    }
}

/// <summary>Client 4.2.9: AntiAFK neither sneaks nor walks while a screen is open; its clock goes on after.</summary>
public sealed class AntiAfkScreenTests
{
    [Fact]
    public void ItsClockStopsWhileAScreenIsOpenAndGoesOnAfter()
    {
        AntiAFK bot = new();
        int first = Settings.DoubleToTick(5.0); // its first turn
        for (int tick = 1; tick < first; tick++)
            Assert.False(bot.Due(screenOpen: false));

        for (int tick = 0; tick < 2000; tick++)
            Assert.False(bot.Due(screenOpen: true));

        Assert.True(bot.Due(screenOpen: false));
    }
}
