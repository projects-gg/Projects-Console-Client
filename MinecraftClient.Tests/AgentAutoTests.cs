using System.Text;
using System.Text.Json.Nodes;
using MinecraftClient.ChatBots;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// The agent bridge's version 4 (docs/agent-bridge.md, "Version 4"): the operations, only with a mode's variable; the
/// cloud's mining blocks; the item rules; both modes' state machines against a fake client, and the order and timing
/// rules their packets keep (the game's, as the Projects anticheat checks them).
/// </summary>
public sealed class AgentAutoTests
{
    private const AgentFeatures Both = AgentFeatures.Fish | AgentFeatures.Mine;

    private static AgentOperation Parse(string json, AgentFeatures features = Both)
    {
        Assert.True(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 7, features, out AgentOperation? operation,
            out _, out _, out string? reason), reason);
        return operation!;
    }

    private static string Refuse(string json, AgentFeatures features = Both)
    {
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes(json), 1, features, out _, out _, out _,
            out string? reason));
        return reason!;
    }

    // --- operations ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("""{"op":"fish","do":"start"}""", 0)] // none
    [InlineData("""{"op":"fish","do":"start"}""", 1)] // movement
    [InlineData("""{"op":"fish","do":"start"}""", 4)] // mining
    [InlineData("""{"op":"mine","do":"start"}""", 2)] // fishing
    [InlineData("""{"op":"mine","do":"stop"}""", 1)]
    [InlineData("""{"op":"aim","yaw":0,"pitch":0}""", 1)]
    [InlineData("""{"op":"aim","yaw":0,"pitch":0}""", 0)]
    public void ModesAreUnknownWithoutTheirVariable(string json, int features)
    {
        Assert.Equal("unknown_op", Refuse(json, (AgentFeatures)features));
    }

    [Fact]
    public void TheTestProcessAsksForNoMode()
    {
        // Only the cloud machine sets the variables: no configuration key, no console command turns a mode on.
        Assert.Equal(AgentFeatures.None, AgentAuto.Features & Both);
        Assert.False(AgentOperation.TryParse(Encoding.UTF8.GetBytes("""{"op":"fish","do":"start"}"""), 1, out _, out _,
            out _, out string? reason));
        Assert.Equal("unknown_op", reason);
        Assert.Equal(AgentFeatures.Fish | AgentFeatures.Move, AgentAuto.FeaturesOf("1", "1", "0"));
        Assert.Equal(AgentFeatures.Mine, AgentAuto.FeaturesOf(null, "true", "1"));
        Assert.Equal(["move", "fish", "mine"], AgentAuto.FeatureNames(AgentFeatures.Move | Both));
    }

    [Fact]
    public void VersionFourAddsItsEventsButNeverToAll()
    {
        Assert.Equal(0, (int)(AgentBridge.EventGroups.All & AgentBridge.EventGroups.Auto));
        Assert.Equal(AgentBridge.EventGroups.All, AgentBridge.ReportedGroups(null, movement: false, auto: false));
        Assert.Equal(AgentBridge.EventGroups.All | AgentBridge.EventGroups.Auto,
            AgentBridge.ReportedGroups(null, movement: false, auto: true));
        Assert.Equal(AgentBridge.EventGroups.Auto, AgentBridge.ReportedGroups("", movement: false, auto: true));
        Assert.Equal(4, AgentAuto.ProtocolVersion);
        Assert.Equal(2, AgentBridge.ProtocolVersion);
        Assert.Equal(3, AgentBridge.MovementProtocolVersion);
    }

    [Fact]
    public void AStartTakesTheCloudsDefaults()
    {
        AgentOperation fish = Parse("""{"op":"fish","id":"f1","do":"start"}""");
        Assert.Equal(("fish", "start", true), (fish.Op, fish.Do, fish.IsAuto));
        Assert.Equal((true, 5, false, false), (fish.Auto!.Switch, fish.Auto.Keep, fish.Auto.KeepWhenFull, fish.Auto.Look));

        AgentOperation mine = Parse("""{"op":"mine","do":"start","blocks":null}""");
        Assert.Equal(AgentAuto.MiningBlocks, mine.Auto!.Blocks);
        Assert.False(mine.Auto.AnyIdle);
        Assert.Equal((60, 180), (mine.Auto.IdleMin, mine.Auto.IdleMax));
    }

    [Fact]
    public void AStartsSettingsAreRead()
    {
        AgentOperation fish = Parse("""{"op":"fish","do":"start","rod":{"switch":false,"keep":100},"full":"keep","look":true}""");
        Assert.Equal((false, 100, true, true), (fish.Auto!.Switch, fish.Auto.Keep, fish.Auto.KeepWhenFull, fish.Auto.Look));

        AgentOperation mine = Parse("""
            {"op":"mine","do":"start","blocks":["minecraft:diamond_ore","minecraft:cobblestone"],
             "tool":{"switch":true,"keep":1},"full":"wait",
             "idle":{"look":true,"sneak":false,"jump":true,"step":true,"min":20,"max":3600}}
            """.Replace("\n", "").Replace("\r", ""));
        Assert.Equal(["minecraft:cobblestone", "minecraft:diamond_ore"], mine.Auto!.Blocks); // the list's order
        Assert.Equal((true, 1, false), (mine.Auto.Switch, mine.Auto.Keep, mine.Auto.KeepWhenFull));
        Assert.Equal((true, false, true, true, 20, 3600),
            (mine.Auto.IdleLook, mine.Auto.IdleSneak, mine.Auto.IdleJump, mine.Auto.IdleStep, mine.Auto.IdleMin,
                mine.Auto.IdleMax));

        Assert.Equal("player", Parse("""{"op":"mine","do":"pause"}""").Why);
        Assert.Equal("trade", Parse("""{"op":"fish","do":"pause","why":"trade"}""").Why);
        Assert.Null(Parse("""{"op":"fish","do":"resume"}""").Auto);
        Assert.Equal("stop", Parse("""{"op":"mine","do":"stop"}""").Do);
    }

    [Theory]
    [InlineData("""{"op":"fish"}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"go"}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"pause","why":"bored"}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"start","rod":{"keep":0}}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"start","rod":{"keep":101}}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"start","rod":{"switch":"yes"}}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"start","full":"command"}""", "bad_request")]
    [InlineData("""{"op":"fish","do":"start","look":1}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","blocks":[]}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","blocks":["minecraft:chest"]}""", "bad_block")]
    [InlineData("""{"op":"mine","do":"start","blocks":["cobblestone"]}""", "bad_block")]
    [InlineData("""{"op":"mine","do":"start","blocks":["minecraft:stone","minecraft:stone"]}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","blocks":"minecraft:stone"}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","idle":{"min":19}}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","idle":{"min":200,"max":100}}""", "bad_request")]
    [InlineData("""{"op":"mine","do":"start","idle":{"max":3601}}""", "bad_request")]
    [InlineData("""{"op":"aim","yaw":181,"pitch":0}""", "bad_request")]
    [InlineData("""{"op":"aim","yaw":0,"pitch":-90.5}""", "bad_request")]
    [InlineData("""{"op":"aim","yaw":0}""", "bad_request")]
    [InlineData("""{"op":"aim","yaw":"0","pitch":0}""", "bad_request")]
    public void MalformedModeOperationsAreRefused(string json, string reason)
    {
        Assert.Equal(reason, Refuse(json));
    }

    [Fact]
    public void AnAimIsRoundedToATenth()
    {
        AgentOperation aim = Parse("""{"op":"aim","id":"a1","yaw":-179.96,"pitch":30.04}""", AgentFeatures.Fish);
        Assert.Equal((-180f, 30f), (aim.Yaw, aim.Pitch));
        Assert.Equal((12.4f, -0.1f), (Parse("""{"op":"aim","yaw":12.35,"pitch":-0.05}""", AgentFeatures.Mine).Yaw,
            Parse("""{"op":"aim","yaw":12.35,"pitch":-0.05}""", AgentFeatures.Mine).Pitch));
        Assert.Equal(180f, Parse("""{"op":"aim","yaw":180,"pitch":90}""").Yaw);
    }

    [Fact]
    public void TheMiningBlocksAreTheClouds()
    {
        JsonNode settings = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors",
            "feat-settings-v1.json")))!;
        string[] cloud = settings["fixed"]!["miningBlocks"]!.AsArray().Select(node => (string)node!).ToArray();
        Assert.Equal(cloud, AgentAuto.MiningBlocks);
        Assert.Equal(33, AgentAuto.MiningBlocks.Length);
        foreach (string id in AgentAuto.MiningBlocks)
        {
            Material? material = AgentAutoItems.MaterialOf(id);
            Assert.True(material is not null, id);
            Assert.Equal(id, AgentAutoItems.BlockId(material!.Value));
        }

        Assert.Null(AgentAutoItems.MaterialOf("minecraft:not_a_block"));
        Assert.Null(AgentAutoItems.MaterialOf("cobblestone"));
    }

    // --- the address reached --------------------------------------------------------------------------------------

    [Fact]
    public void JoinedSaysTheAddressTheConnectionReached()
    {
        using System.Net.Sockets.TcpListener listener = new(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        using System.Net.Sockets.TcpClient tcp = new();
        tcp.Connect(System.Net.IPAddress.Loopback, port);
        using System.Net.Sockets.Socket accepted = listener.AcceptSocket();
        Assert.Equal(("127.0.0.1", port), McClient.EndPointOf(tcp.Client));
        Assert.Null(McClient.EndPointOf(null));
        Assert.Null(McClient.EndPointOf(new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream,
            System.Net.Sockets.ProtocolType.Tcp))); // not connected
        Assert.Equal("10.1.2.3", McClient.AddressText(System.Net.IPAddress.Parse("::ffff:10.1.2.3")));
        Assert.Equal("2001:db8::1", McClient.AddressText(System.Net.IPAddress.Parse("2001:db8::1")));
        Assert.Equal("45.94.169.25", McClient.AddressText(System.Net.IPAddress.Parse("45.94.169.25")));
    }

    // --- items -----------------------------------------------------------------------------------------------------

    private static Item Rod(int damage = 0, bool enchanted = false, ItemType type = ItemType.FishingRod)
    {
        Dictionary<string, object> nbt = new() { ["Damage"] = damage };
        if (enchanted)
            nbt["Enchantments"] = new object[] { new Dictionary<string, object> { ["id"] = "minecraft:lure", ["lvl"] = (short)3 } };
        return new Item(type, 1, nbt);
    }

    [Fact]
    public void DurabilityIsWhatIsLeftOfTheMaximum()
    {
        Assert.Equal((4, 64), AgentAutoItems.Durability(Rod(60)));
        Assert.Equal((1561, 1561), AgentAutoItems.Durability(Rod(0, type: ItemType.DiamondPickaxe)));
        Assert.Equal((59 - 58, 59), AgentAutoItems.Durability(Rod(58, type: ItemType.WoodenPickaxe)));
        Item unbreakable = new(ItemType.IronPickaxe, 1, new Dictionary<string, object> { ["Damage"] = 200, ["Unbreakable"] = (byte)1 });
        Assert.Equal((250, 250), AgentAutoItems.Durability(unbreakable));
        Assert.Null(AgentAutoItems.Durability(new Item(ItemType.Cod, 1, null)));
        Assert.Equal(new AutoTool("minecraft:fishing_rod", 4, 64), AgentAutoItems.Tool(Rod(60)));
    }

    [Theory]
    [InlineData("minecraft:cod", false, "fish")]
    [InlineData("minecraft:pufferfish", false, "fish")]
    [InlineData("minecraft:bow", true, "treasure")]
    [InlineData("minecraft:enchanted_book", true, "treasure")]
    [InlineData("minecraft:fishing_rod", true, "treasure")]
    [InlineData("minecraft:fishing_rod", false, "junk")]
    [InlineData("minecraft:name_tag", false, "treasure")]
    [InlineData("minecraft:nautilus_shell", false, "treasure")]
    [InlineData("minecraft:saddle", false, "treasure")]
    [InlineData("minecraft:lily_pad", false, "junk")]
    [InlineData("minecraft:leather_boots", false, "junk")]
    public void CatchesFallIntoTheGamesLootCategories(string id, bool enchanted, string category)
    {
        Assert.Equal(category, AgentAutoItems.Category(id, enchanted));
    }

    [Fact]
    public void ACatchIsWhatCameInNotWhatMovedOrWore()
    {
        var before = new Dictionary<int, (string, int, bool)>
        {
            [36] = ("minecraft:fishing_rod", 1, false), [9] = ("minecraft:cod", 3, false), [10] = ("minecraft:dirt", 5, false),
        };
        var after = new Dictionary<int, (string, int, bool)>
        {
            [36] = ("minecraft:fishing_rod", 1, false), // the rod wore: no catch
            [9] = ("minecraft:cod", 4, false),          // one more cod
            [11] = ("minecraft:dirt", 5, false),        // the dirt moved: no catch
            [12] = ("minecraft:enchanted_book", 1, true),
        };
        Assert.Equal([("minecraft:cod", 1, false), ("minecraft:enchanted_book", 1, true)], AgentAutoItems.Added(before, after));
    }

    [Theory]
    [InlineData(Protocol18Handler.MC_1_15_Version, null)]
    [InlineData(Protocol18Handler.MC_1_16_Version, 8)]
    [InlineData(Protocol18Handler.MC_1_16_5_Version, 8)]
    [InlineData(Protocol18Handler.MC_1_17_Version, 9)]
    [InlineData(Protocol18Handler.MC_1_20_4_Version, 9)]
    [InlineData(Protocol18Handler.MC_1_21_4_Version, 9)]
    [InlineData(Protocol18Handler.MC_1_21_11_Version, 9)]
    [InlineData(Protocol18Handler.MC_26_1_Version, 9)]
    [InlineData(Protocol18Handler.MC_26_3_Version, 9)]
    public void TheBitingFlagIsAfterTheEntitysOwnEntries(int protocol, int? index)
    {
        Assert.Equal(index, AgentFishing.BitingIndex(protocol));
        Assert.Equal(protocol >= Protocol18Handler.MC_1_17_Version ? 8 : 7, AgentFishing.HookedIndex(protocol));
    }

    [Fact]
    public void MinedBlocksDropWhatTheGamesLootSays()
    {
        Assert.Equal(ItemType.Cobblestone, AgentMining.Drop(Material.Stone, false, Protocol18Handler.MC_26_3_Version));
        Assert.Equal(ItemType.Stone, AgentMining.Drop(Material.Stone, true, Protocol18Handler.MC_26_3_Version));
        Assert.Equal(ItemType.RawIron, AgentMining.Drop(Material.IronOre, false, Protocol18Handler.MC_1_17_Version));
        Assert.Equal(ItemType.IronOre, AgentMining.Drop(Material.IronOre, false, Protocol18Handler.MC_1_16_5_Version));
        Assert.Equal(ItemType.Cobblestone, AgentMining.Drop(Material.Cobblestone, false, Protocol18Handler.MC_26_3_Version));
        Assert.Equal(ItemType.Quartz, AgentMining.Drop(Material.NetherQuartzOre, false, Protocol18Handler.MC_26_3_Version));
    }

    // --- the look ray ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(180f, 0f, 0, 65, -1, Direction.South)]  // north at eye level: the block's south face
    [InlineData(0f, 0f, 0, 65, 1, Direction.North)]     // south
    [InlineData(90f, 0f, -1, 65, 0, Direction.East)]    // west
    [InlineData(270f, 0f, 1, 65, 0, Direction.West)]    // east
    [InlineData(180f, 50f, 0, 64, -1, Direction.Up)]    // down onto the block in front of the feet: over its edge, its top
    [InlineData(180f, 60f, 0, 64, -1, Direction.South)] // steeper: below its edge, its side
    [InlineData(0f, 90f, 0, 63, 0, Direction.Up)]       // straight down
    [InlineData(0f, -90f, 0, 67, 0, Direction.Down)]    // straight up
    public void TheLookHitsTheFirstBlockByTheFaceItEnters(float yaw, float pitch, int x, int y, int z, Direction face)
    {
        Material At(int bx, int by, int bz) => (bx, by, bz) == (x, y, z) ? Material.Cobblestone : Material.Air;
        var hit = AgentMining.LookRay(At, 0.5, 64 + 1.62, 0.5, yaw, pitch, AgentMining.Reach);
        Assert.NotNull(hit);
        Assert.Equal((x, y, z, face), (hit!.Value.X, hit.Value.Y, hit.Value.Z, hit.Value.Face));
    }

    [Fact]
    public void TheLookReachesFourAndAHalfBlocksAndStopsAtAnythingButAirAndFluids()
    {
        Material Far(int x, int y, int z) => z == -6 && y == 65 ? Material.Cobblestone : Material.Air;
        Assert.Null(AgentMining.LookRay(Far, 0.5, 65.62, 0.5, 180, 0, AgentMining.Reach)); // its face 5.5 blocks away
        Material Near(int x, int y, int z) => z == -5 && y == 65 ? Material.Cobblestone : Material.Air;
        Assert.NotNull(AgentMining.LookRay(Near, 0.5, 65.62, 0.5, 180, 0, AgentMining.Reach)); // 4.5 away
        Material Watery(int x, int y, int z) => z == -2 ? Material.Stone : z == -1 ? Material.Water : Material.Air;
        Assert.Equal(-2, AgentMining.LookRay(Watery, 0.5, 65.62, 0.5, 180, 0, 4.5)!.Value.Z);
        Material Torch(int x, int y, int z) => z == -2 ? Material.Stone : z == -1 ? Material.Torch : Material.Air;
        Assert.Equal(Material.Torch, AgentMining.LookRay(Torch, 0.5, 65.62, 0.5, 180, 0, 4.5)!.Value.Block);
    }

    // --- a fake client ---------------------------------------------------------------------------------------------

    private sealed class Client : IMineHost
    {
        public readonly List<(long Tick, string Action)> Actions = new();
        public readonly Dictionary<(int, int, int), Material> Blocks = new();
        public readonly Dictionary<int, Item> Inv = new();
        public long Now;
        public (float Yaw, float Pitch)? PendingLook;
        public int DigTime = 8;

        public int Protocol { get; set; } = Protocol18Handler.MC_26_3_Version;
        public bool Terrain { get; set; } = true;
        public bool Entities { get; set; } = true;
        public bool Inventory { get; set; } = true;
        public bool PositionKnown { get; set; } = true;
        public Location Position { get; set; } = new(0.5, 64, 0.5);
        public float Yaw { get; set; } = 180;
        public float Pitch { get; set; }
        public bool ScreenOpen { get; set; }
        public bool WindowOpen { get; set; }
        public int HeldSlot { get; set; }
        public int PlayerEntityId => 42;
        public IReadOnlyDictionary<int, Item>? Items => Inventory ? Inv : null;
        public bool OnGround { get; set; } = true;
        public bool Sneaking { get; set; }
        public bool Walking { get; set; }

        private void Did(string action) => Actions.Add((Now, action));

        public bool Use()
        {
            Did("use");
            return true;
        }

        public bool Hold(int slot)
        {
            Did("hold " + slot);
            HeldSlot = slot;
            return true;
        }

        public bool Swap(int slot, int hotbar)
        {
            Did($"swap {slot} {hotbar}");
            Inv.TryGetValue(slot, out Item? moving);
            Inv.TryGetValue(36 + hotbar, out Item? held);
            if (held is null) Inv.Remove(slot); else Inv[slot] = held;
            if (moving is null) Inv.Remove(36 + hotbar); else Inv[36 + hotbar] = moving;
            return true;
        }

        public void Look(float yaw, float pitch)
        {
            Did($"look {yaw:0.#} {pitch:0.#}");
            PendingLook = (yaw, pitch);
        }

        public Material BlockAt(int x, int y, int z) => Blocks.TryGetValue((x, y, z), out Material m) ? m : Material.Air;
        public int DigTicks(Material block, Item? tool) => tool is not null && AgentAutoItems.IsPickaxe(tool.Type) ? DigTime : 100;

        public bool Dig(int status, Location block, Direction face)
        {
            Did($"dig {status} {(int)block.X},{(int)block.Y},{(int)block.Z} {face}");
            return true;
        }

        public bool Swing()
        {
            Did("swing");
            return true;
        }

        public void ReleaseSneak() => Did("release sneak");
        public void Jump() => Did("jump");
        public void Sneak(bool on) => Did("sneak " + on);
        public void StepKey(int key) => Did("step " + key);

        /// <summary>The server's word on a block after a finish (a generator refills it at once); null: none comes.</summary>
        public Action<Location, Material>? Refill;

        /// <summary>One client tick: the mode acts, then the tick's move carries the look.</summary>
        public void Run(Action tick, int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                Now++;
                tick();
                if (PendingLook is { } look)
                {
                    (Yaw, Pitch) = look;
                    PendingLook = null;
                }

                // A finish breaks the block (a start alone does, when it breaks at once): the server's word follows.
                foreach ((long _, string action) in Actions.Where(a => a.Tick == Now
                             && (a.Action.StartsWith("dig 2") || DigTime == 0 && a.Action.StartsWith("dig 0"))).ToList())
                {
                    string[] at = action.Split(' ')[2].Split(',');
                    Refill?.Invoke(new Location(int.Parse(at[0]), int.Parse(at[1]), int.Parse(at[2])), Material.Cobblestone);
                }
            }
        }

        /// <summary>Run until the first action of that kind, and that many ticks more.</summary>
        public void RunUntil(Action tick, string prefix, int more = 0)
        {
            for (int i = 0; i < 2000 && !TicksOf(prefix).Any(); i++)
                Run(tick);
            Run(tick, more);
        }

        public IEnumerable<long> TicksOf(string prefix) => Actions.Where(a => a.Action.StartsWith(prefix)).Select(a => a.Tick);
        public bool In(long tick, string prefix) => Actions.Any(a => a.Tick == tick && a.Action.StartsWith(prefix));
    }

    // --- fishing ---------------------------------------------------------------------------------------------------

    private static (AgentFishing Fishing, Client Client, List<AutoState> States, List<string> Catches) Fishing(
        AutoSettings? settings = null, bool water = true)
    {
        Client client = new() { Pitch = 30 };
        if (water)
        {
            for (int x = -3; x <= 3; x++)
                for (int z = -8; z <= -2; z++)
                    client.Blocks[(x, 62, z)] = Material.Water;
        }

        client.Inv[36] = Rod();
        List<AutoState> states = new();
        List<string> catches = new();
        AgentFishing fishing = new(client, states.Add, (item, n, cat, ench) => catches.Add($"{item} {n} {cat} {ench}"),
            new Random(7));
        Assert.Null(fishing.Start(settings ?? new AutoSettings()));
        return (fishing, client, states, catches);
    }

    /// <summary>Run until the next use; the bobber of a cast appears, lands and comes to rest in the water.</summary>
    private static long CastAndLand(AgentFishing fishing, Client client, int bobberId = 500)
    {
        int uses = client.TicksOf("use").Count();
        for (int i = 0; i < 400 && client.TicksOf("use").Count() == uses; i++)
            client.Run(fishing.Tick);
        long cast = client.TicksOf("use").Last();
        fishing.OnEntitySpawn(bobberId, EntityType.FishingBobber, client.PlayerEntityId, new Location(0.5, 64, -1));
        client.Run(fishing.Tick, 3);
        fishing.OnEntityMove(bobberId, new Location(0.5, 62.9, -5));
        client.Run(fishing.Tick, 12);
        return cast;
    }

    [Fact]
    public void ItCastsWaitsForABiteReelsInAndCountsTheCatch()
    {
        var (fishing, client, states, catches) = Fishing();
        Assert.Equal("on", states[^1].State);
        Assert.Equal(new AutoTool("minecraft:fishing_rod", 64, 64), states[^1].Tool);

        long cast = CastAndLand(fishing, client);
        Assert.Contains(states, s => s.Doing == "cast");
        Assert.Equal("wait", states[^1].Doing);

        fishing.OnEntityMetadata(500, new Dictionary<int, object?> { [9] = true });
        long bite = client.Now;
        client.Run(fishing.Tick, 12);
        long reel = client.TicksOf("use").Last();
        Assert.NotEqual(cast, reel);
        Assert.InRange(reel - bite, AgentFishing.ReelMinTicks, AgentFishing.ReelMaxTicks + 1);
        Assert.Equal("reel", states[^1].Doing);

        client.Inv[10] = new Item(ItemType.Dirt, 64, null); // a server's gift in the same moment: not a catch
        fishing.OnInventoryChanged();
        client.Inv[9] = new Item(ItemType.Cod, 1, null);
        fishing.OnInventoryChanged();
        client.Inv[11] = new Item(ItemType.Salmon, 1, null); // a second stack: a reel brings one
        fishing.OnInventoryChanged();
        Assert.Equal(["minecraft:cod 1 fish False"], catches);

        fishing.OnEntityDespawn(500);
        client.Run(fishing.Tick, AgentFishing.RecastMaxTicks + 2);
        long recast = client.TicksOf("use").Last();
        Assert.InRange(recast - reel, AgentFishing.RecastMinTicks, AgentFishing.RecastMaxTicks + 1);
    }

    [Fact]
    public void ABobberOfAnotherPlayerOrABiteBefore116IsNotOurs()
    {
        var (fishing, client, states, _) = Fishing();
        client.Run(fishing.Tick, 40);
        fishing.OnEntitySpawn(77, EntityType.FishingBobber, 9, new Location(0.5, 64, -1)); // someone else's
        fishing.OnEntityMetadata(77, new Dictionary<int, object?> { [9] = true });
        int uses = client.TicksOf("use").Count();
        client.Run(fishing.Tick, 10);
        Assert.Equal(uses, client.TicksOf("use").Count());
    }

    [Fact]
    public void AWornRodIsPutAwayForTheBestOtherOneHotbarFirstAndNeverInAUsesTick()
    {
        var (fishing, client, states, _) = Fishing(new AutoSettings { Keep = 5 });
        client.Inv[36] = Rod(59); // 5 left: at keep
        client.Inv[12] = Rod(0, enchanted: true); // main inventory
        client.Inv[40] = Rod(10); // hotbar
        client.Run(fishing.Tick, 80);
        Assert.Equal(4, client.HeldSlot);
        long hold = client.TicksOf("hold").First();
        long use = client.TicksOf("use").First();
        Assert.True(use - hold >= 3, $"hold {hold}, use {use}");
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("swap"));
    }

    [Fact]
    public void ARodInTheMainInventoryIsSwappedIntoTheHand()
    {
        var (fishing, client, _, _) = Fishing();
        client.Inv[36] = new Item(ItemType.Stick, 1, null);
        client.Inv[20] = Rod(3); // 61 left
        client.Run(fishing.Tick, 80);
        Assert.Contains(client.Actions, a => a.Action == "swap 20 0");
        Assert.Equal(ItemType.FishingRod, client.Inv[36].Type);
        Assert.True(client.TicksOf("use").First() - client.TicksOf("swap").First() >= 3);
    }

    [Fact]
    public void WithoutAUsableRodItWaitsAndSaysWhy()
    {
        var (fishing, client, states, _) = Fishing(new AutoSettings { Keep = 1 });
        client.Inv[36] = Rod(62); // 2 left: a reel from the ground would break it
        client.Run(fishing.Tick, 80);
        Assert.Equal(("waiting", "rod_low"), (states[^1].State, states[^1].Reason));
        Assert.Empty(client.TicksOf("use"));

        client.Inv.Clear();
        fishing.OnInventoryChanged();
        client.Run(fishing.Tick, 5);
        Assert.Equal(("waiting", "no_rod"), (states[^1].State, states[^1].Reason));

        client.Inv[36] = Rod();
        fishing.OnInventoryChanged();
        client.Run(fishing.Tick, 40);
        Assert.Equal("on", states[^1].State);
        Assert.NotEmpty(client.TicksOf("use"));
    }

    [Fact]
    public void AFullInventoryWaitsUnlessItKeepsGoing()
    {
        var (fishing, client, states, _) = Fishing();
        for (int slot = 9; slot <= 44; slot++)
            if (slot != 36)
                client.Inv[slot] = new Item(ItemType.Dirt, 64, null);
        client.Run(fishing.Tick, 80);
        Assert.Equal(("waiting", "full"), (states[^1].State, states[^1].Reason));

        var (keeps, keepClient, keepStates, _) = Fishing(new AutoSettings { KeepWhenFull = true });
        for (int slot = 9; slot <= 44; slot++)
            if (slot != 36)
                keepClient.Inv[slot] = new Item(ItemType.Dirt, 64, null);
        keepClient.Run(keeps.Tick, 80);
        Assert.NotEmpty(keepClient.TicksOf("use"));
        Assert.Equal("on", keepStates[^1].State);
    }

    [Fact]
    public void FiveFailedCastsMeanNoWaterAndItTriesAgainEveryThirtySeconds()
    {
        var (fishing, client, states, _) = Fishing(water: false);
        for (int i = 0; i < 2000 && states[^1].Reason != "no_water"; i++)
            client.Run(fishing.Tick); // no bobber ever comes
        Assert.Equal(("waiting", "no_water"), (states[^1].State, states[^1].Reason));
        int uses = client.TicksOf("use").Count();
        Assert.Equal(AgentFishing.FailedCasts, uses);
        client.Run(fishing.Tick, AgentFishing.RetryTicks + 10);
        Assert.Equal(uses + 1, client.TicksOf("use").Count());
        Assert.Equal("waiting", states[^1].State);
    }

    [Fact]
    public void NoBiteInAMinuteReelsInAsAFailedCast()
    {
        var (fishing, client, _, _) = Fishing();
        long cast = CastAndLand(fishing, client);
        client.Run(fishing.Tick, AgentFishing.BiteTicks + 5);
        Assert.Equal(2, client.TicksOf("use").Count(t => t > cast - 1));
    }

    [Fact]
    public void AMenuPausesItAndItGoesOnByItself()
    {
        var (fishing, client, states, _) = Fishing();
        client.Run(fishing.Tick, 5);
        client.WindowOpen = client.ScreenOpen = true;
        client.Run(fishing.Tick, 100);
        Assert.Equal(("paused", "window"), (states[^1].State, states[^1].Reason));
        Assert.Empty(client.TicksOf("use"));
        client.WindowOpen = client.ScreenOpen = false;
        client.Run(fishing.Tick, 60);
        Assert.Equal("on", states[^1].State);
        Assert.NotEmpty(client.TicksOf("use"));
    }

    [Fact]
    public void BeingMovedPausesItUntilThePlayerResumesWhichAnchorsTheNewSpot()
    {
        var (fishing, client, states, _) = Fishing();
        client.Run(fishing.Tick, 5);
        client.Position = new Location(10.5, 64, 0.5); // a teleport
        client.Run(fishing.Tick, 2);
        Assert.Equal(("paused", "moved"), (states[^1].State, states[^1].Reason));
        client.Run(fishing.Tick, 100);
        Assert.Empty(client.TicksOf("use"));
        Assert.Null(fishing.Resume());
        client.Run(fishing.Tick, 60);
        Assert.Equal("on", states[^1].State);
        Assert.NotEmpty(client.TicksOf("use"));
    }

    [Fact]
    public void AnAimTurnsBeforeTheNextCastNeverInItsTick()
    {
        var (fishing, client, _, _) = Fishing();
        CastAndLand(fishing, client);
        fishing.Aim(90, 20);
        client.Run(fishing.Tick, 80);
        long look = client.TicksOf("look").First();
        long recast = client.TicksOf("use").Last();
        Assert.True(recast - look >= 2, $"look {look}, cast {recast}");
        Assert.Equal((90f, 20f), (client.Yaw, client.Pitch));
        foreach (long use in client.TicksOf("use"))
            Assert.False(client.In(use, "look") || client.In(use, "hold") || client.In(use, "swap"), $"tick {use}");
    }

    [Fact]
    public void TheSmallLookBetweenCastsComesBackBeforeTheCast()
    {
        var (fishing, client, _, _) = Fishing(new AutoSettings { Look = true });
        for (int i = 0; i < 6; i++)
        {
            CastAndLand(fishing, client, 600 + i);
            fishing.OnEntityMetadata(600 + i, new Dictionary<int, object?> { [9] = true });
            client.Run(fishing.Tick, 12);
            fishing.OnEntityDespawn(600 + i);
        }

        Assert.NotEmpty(client.TicksOf("look"));
        foreach (long use in client.TicksOf("use"))
            Assert.False(client.In(use, "look"), $"tick {use}");
        Assert.Equal((180f, 30f), (client.Yaw, client.Pitch)); // the anchor's look
    }

    [Fact]
    public void AStopOrAPlayersPauseReelsTheBobberIn()
    {
        var (fishing, client, states, _) = Fishing();
        CastAndLand(fishing, client);
        int uses = client.TicksOf("use").Count();
        Assert.Null(fishing.Pause("player"));
        client.Run(fishing.Tick, 2);
        Assert.Equal(uses + 1, client.TicksOf("use").Count());
        Assert.Equal(("paused", "player"), (states[^1].State, states[^1].Reason));
        Assert.Equal("mode_off", new AgentFishing(client, _ => { }, (_, _, _, _) => { }).Pause("player"));
    }

    [Fact]
    public void AntiAfkIsSilentOnlyWhileAModeIsAtWork()
    {
        Assert.False(AgentAuto.ModeWorking);
        Assert.False(AntiAFK.Silenced(screenOpen: false));
        Assert.True(AntiAFK.Silenced(screenOpen: true));
        try
        {
            AgentAuto.ModeWorking = true;
            Assert.True(AntiAFK.Silenced(screenOpen: false));
        }
        finally
        {
            AgentAuto.ModeWorking = false;
        }

        Assert.False(AntiAFK.Silenced(screenOpen: false));
    }

    [Fact]
    public void FishingHandsOverFromAntiAfkBeforeItsFirstActionAndBackWhileItWaits()
    {
        var (fishing, client, states, _) = Fishing();
        Assert.True(fishing.Working); // on: AntiAFK stops with the next tick; the hand-over comes first
        client.Run(fishing.Tick, 60);
        Assert.Equal(1, client.TicksOf("release sneak").First()); // the tick after the start, not the start's own
        long release = client.TicksOf("release sneak").First();
        long cast = client.TicksOf("use").First();
        Assert.True(cast - release >= 3, $"release {release}, cast {cast}");
        Assert.True(fishing.Working);

        // A wait: AntiAFK may run again; the end of the wait hands over again before the next cast.
        for (int slot = 9; slot <= 44; slot++)
            if (slot != 36)
                client.Inv[slot] = new Item(ItemType.Dirt, 64, null);
        long full = client.Now;
        fishing.OnEntityDespawn(500);
        client.Run(fishing.Tick, 120);
        Assert.Equal(("waiting", "full"), (states[^1].State, states[^1].Reason));
        Assert.False(fishing.Working);
        for (int slot = 9; slot <= 44; slot++)
            if (slot != 36)
                client.Inv.Remove(slot);
        fishing.OnInventoryChanged();
        client.Run(fishing.Tick, 80);
        Assert.Equal("on", states[^1].State);
        long again = client.TicksOf("release sneak").Last();
        Assert.True(again > full);
        Assert.True(client.TicksOf("use").Last() - again >= 3);

        Assert.Null(fishing.Pause("player"));
        client.Run(fishing.Tick, 3);
        Assert.False(fishing.Working);
    }

    [Fact]
    public void AMenuClosingWithTheBobberOutPutsItBackToWorkAtOnce()
    {
        var (fishing, client, states, _) = Fishing();
        CastAndLand(fishing, client);
        client.WindowOpen = client.ScreenOpen = true;
        client.Run(fishing.Tick, 10);
        Assert.False(fishing.Working); // paused by the menu: AntiAFK may run
        client.WindowOpen = client.ScreenOpen = false;
        int releases = client.TicksOf("release sneak").Count();
        client.Run(fishing.Tick, 1);
        Assert.Equal("on", states[^1].State);
        Assert.True(fishing.Working);
        fishing.OnEntityMetadata(500, new Dictionary<int, object?> { [9] = true });
        client.Run(fishing.Tick, 15);
        Assert.Equal(releases + 1, client.TicksOf("release sneak").Count());
        long release = client.TicksOf("release sneak").Last();
        Assert.True(client.TicksOf("use").Last() - release >= 3);
    }

    [Fact]
    public void AnAimRightAfterAFinishStillWaitsForTheServersWord()
    {
        var (mining, client, _, _) = Mining();
        client.Refill = null;
        client.RunUntil(mining.Tick, "dig 2");
        mining.Aim(180, 0);
        client.Run(mining.Tick, 60);
        Assert.Single(client.TicksOf("dig 0"));
        mining.OnBlockChange(new Location(0, 65, -1), Material.Cobblestone);
        client.Run(mining.Tick, 5);
        Assert.Equal(2, client.TicksOf("dig 0").Count());
    }

    [Fact]
    public void MiningHandsOverFromAntiAfkBeforeItsFirstStart()
    {
        var (mining, client, states, _) = Mining();
        client.RunUntil(mining.Tick, "dig 0");
        long release = client.TicksOf("release sneak").First();
        Assert.True(client.TicksOf("dig 0").First() - release >= 3);
        Assert.True(mining.Working);
        Assert.Null(mining.Pause("trade"));
        Assert.False(mining.Working);
        Assert.Null(mining.Resume());
        client.Run(mining.Tick, 30);
        Assert.Equal(2, client.TicksOf("release sneak").Count());
        Assert.True(mining.Working);
    }

    [Fact]
    public void FishingNeedsEntitiesAndInventory()
    {
        Client client = new() { Entities = false };
        Assert.Equal("entities_disabled", new AgentFishing(client, _ => { }, (_, _, _, _) => { }).Start(new AutoSettings()));
        client.Entities = true;
        client.Inventory = false;
        Assert.Equal("inventory_disabled", new AgentFishing(client, _ => { }, (_, _, _, _) => { }).Start(new AutoSettings()));
        client.Inventory = true;
        client.Protocol = Protocol18Handler.MC_1_8_Version;
        List<AutoState> states = new();
        Assert.Null(new AgentFishing(client, states.Add, (_, _, _, _) => { }).Start(new AutoSettings()));
        Assert.Equal(("off", "version"), (states[^1].State, states[^1].Reason));
    }

    // --- mining ----------------------------------------------------------------------------------------------------

    private static (AgentMining Mining, Client Client, List<AutoState> States, List<string> Dug) Mining(
        AutoSettings? settings = null)
    {
        Client client = new();
        client.Blocks[(0, 65, -1)] = Material.Cobblestone; // in front, at eye level, looking north
        for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                client.Blocks[(x, 63, z)] = Material.Stone; // the floor
        client.Inv[36] = Rod(0, type: ItemType.DiamondPickaxe);
        List<AutoState> states = new();
        List<string> dug = new();
        AgentMining mining = new(client, states.Add, (block, n) => dug.Add($"{block} {n}"), new Random(3));
        client.Refill = mining.OnBlockChange;
        Assert.Null(mining.Start(settings ?? new AutoSettings { Blocks = ["minecraft:cobblestone"] }));
        return (mining, client, states, dug);
    }

    [Fact]
    public void ItBreaksTheBlockInFrontInTheGamesTime()
    {
        var (mining, client, states, dug) = Mining();
        client.Run(mining.Tick, 200);

        List<long> starts = client.Actions.Where(a => a.Action.StartsWith("dig 0")).Select(a => a.Tick).ToList();
        List<long> finishes = client.Actions.Where(a => a.Action.StartsWith("dig 2")).Select(a => a.Tick).ToList();
        Assert.True(starts.Count >= 5, $"{starts.Count} starts");
        Assert.All(client.Actions.Where(a => a.Action.StartsWith("dig")), a => Assert.EndsWith("0,65,-1 South", a.Action));
        for (int i = 0; i < finishes.Count; i++)
        {
            // The finish: the game's time (n - 1 ticks after the start) and a tick more.
            Assert.Equal(client.DigTime, finishes[i] - starts[i]);
            // A swing with the start, every tick between, and with the finish.
            for (long t = starts[i]; t <= finishes[i]; t++)
                Assert.True(client.In(t, "swing"), $"no swing at {t}");
            // The next start: the game's 5 ticks of delay after the finish, and up to 3 more.
            if (i + 1 < starts.Count)
                Assert.InRange(starts[i + 1] - finishes[i], AgentMining.DestroyDelayTicks,
                    AgentMining.DestroyDelayTicks + AgentMining.RandomDelayTicks);
        }

        Assert.Equal(finishes.Count, dug.Count);
        Assert.All(dug, d => Assert.Equal("minecraft:cobblestone 1", d));
        Assert.Equal(new AutoTarget("minecraft:cobblestone", 0, 65, -1), states.Last(s => s.Target is not null).Target);
        Assert.Equal("on", states[^1].State);
        Assert.Null(states[^1].Doing);
    }

    [Fact]
    public void AfterAFinishTheNextStartWaitsForTheServersWordOnThatBlock()
    {
        var (mining, client, _, _) = Mining();
        client.Refill = null; // the server is slow: no word yet
        client.RunUntil(mining.Tick, "dig 2");
        long finish = client.TicksOf("dig 2").First();
        client.Run(mining.Tick, 100);
        Assert.Single(client.TicksOf("dig 0"));
        mining.OnBlockChange(new Location(0, 65, -1), Material.Cobblestone);
        client.Run(mining.Tick, 2);
        Assert.Equal(2, client.TicksOf("dig 0").Count());
        Assert.True(client.TicksOf("dig 0").Last() - finish > 100);
    }

    [Fact]
    public void OnlyBlocksOfTheListAreBrokenAndNothingInSightMeansNoTarget()
    {
        var (mining, client, states, _) = Mining(new AutoSettings { Blocks = ["minecraft:diamond_ore"] });
        client.Run(mining.Tick, AgentMining.NoTargetTicks + 20);
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("dig"));
        Assert.Equal(("waiting", "no_target"), (states[^1].State, states[^1].Reason));

        // Checked again every 30 s: a block of the list appears.
        client.Blocks[(0, 65, -1)] = Material.DiamondOre;
        client.Run(mining.Tick, 100);
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("dig"));
        client.Run(mining.Tick, AgentMining.RetryTicks);
        Assert.Contains(client.Actions, a => a.Action.StartsWith("dig 0"));
        Assert.Equal("on", states[^1].State);
    }

    [Fact]
    public void ABlockThatChangesMidDigIsAbortedFacingDownAndTheNextWaits()
    {
        var (mining, client, _, _) = Mining();
        client.RunUntil(mining.Tick, "dig 0", 1);
        long start = client.TicksOf("dig 0").First();
        client.Blocks[(0, 65, -1)] = Material.Air; // someone else broke it
        client.Run(mining.Tick, 1);
        Assert.Contains(client.Actions, a => a.Action == "dig 1 0,65,-1 Down");
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("dig 2"));
        Assert.True(client.TicksOf("dig 1").First() > start);
    }

    [Fact]
    public void AMenuDuringADigAbortsItOnceTheMenuIsClosed()
    {
        var (mining, client, states, _) = Mining();
        client.RunUntil(mining.Tick, "dig 0", 2);
        client.WindowOpen = client.ScreenOpen = true;
        client.Run(mining.Tick, 20);
        Assert.Equal(("paused", "window"), (states[^1].State, states[^1].Reason));
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("dig 1"));
        Assert.DoesNotContain(client.Actions, a => a.Action.StartsWith("dig 2"));
        Assert.DoesNotContain(client.Actions, a => a.Tick > client.Now - 19 && a.Action == "swing");
        client.WindowOpen = client.ScreenOpen = false;
        client.Run(mining.Tick, 1);
        Assert.Contains(client.Actions, a => a.Action == "dig 1 0,65,-1 Down");
        client.Run(mining.Tick, 40);
        Assert.Equal("on", states[^1].State);
    }

    [Fact]
    public void AToolIsTakenOnlyBetweenBlocksTheFastestOfTheHotbarFirst()
    {
        var (mining, client, states, _) = Mining(new AutoSettings { Blocks = ["minecraft:cobblestone"], Keep = 5 });
        client.Inv[36] = Rod(1556, type: ItemType.DiamondPickaxe); // 5 left: at keep
        client.Inv[38] = Rod(0, type: ItemType.StonePickaxe);
        client.Inv[20] = Rod(0, type: ItemType.NetheritePickaxe); // main inventory: the hotbar comes first
        client.Run(mining.Tick, 60);
        Assert.Equal(2, client.HeldSlot);
        long hold = client.TicksOf("hold").First();
        long start = client.TicksOf("dig 0").First();
        Assert.True(start - hold >= 3, $"hold {hold}, start {start}");

        // Never a slot change between a start and its finish.
        List<long> starts = client.TicksOf("dig 0").ToList(), finishes = client.TicksOf("dig 2").ToList();
        foreach (long change in client.TicksOf("hold").Concat(client.TicksOf("swap")))
            for (int i = 0; i < finishes.Count; i++)
                Assert.False(change >= starts[i] && change <= finishes[i], $"change at {change}");

        client.Inv.Clear();
        mining.OnInventoryChanged();
        client.Run(mining.Tick, 30);
        Assert.Equal(("waiting", "no_tool"), (states[^1].State, states[^1].Reason));
    }

    [Fact]
    public void AnInstantBreakIsAStartAloneAndTheDelayStillHolds()
    {
        var (mining, client, _, dug) = Mining();
        client.DigTime = 0;
        client.Run(mining.Tick, 100);
        Assert.Empty(client.TicksOf("dig 2"));
        List<long> starts = client.TicksOf("dig 0").ToList();
        Assert.True(starts.Count >= 5);
        for (int i = 1; i < starts.Count; i++)
            Assert.True(starts[i] - starts[i - 1] >= AgentMining.DestroyDelayTicks);
        Assert.Equal(starts.Count, dug.Count);
    }

    [Fact]
    public void IdleActionsHappenBetweenBlocksAndTheLookComesBackBeforeAStart()
    {
        var (mining, client, _, _) = Mining(new AutoSettings
        {
            Blocks = ["minecraft:cobblestone"], IdleLook = true, IdleSneak = true, IdleJump = true, IdleMin = 20, IdleMax = 20,
        });
        client.Run(mining.Tick, 20 * 20 * 6);
        List<long> starts = client.TicksOf("dig 0").ToList(), finishes = client.TicksOf("dig 2").ToList();
        long[] idle = client.TicksOf("look").Concat(client.TicksOf("sneak")).Concat(client.TicksOf("jump")).ToArray();
        Assert.NotEmpty(idle);
        foreach (long t in idle)
            for (int i = 0; i < finishes.Count; i++)
                Assert.False(t >= starts[i] && t <= finishes[i], $"idle at {t} during a dig");
        foreach (long look in client.TicksOf("look"))
            Assert.DoesNotContain(starts, s => s >= look && s - look < 3);
        Assert.Equal((180f, 0f), (client.Yaw, client.Pitch)); // the anchor's look
    }

    [Fact]
    public void AStepGoesBackAndReturnsOnlyOverASafeFloor()
    {
        var (mining, client, _, _) = Mining(new AutoSettings
        {
            Blocks = ["minecraft:cobblestone"], IdleStep = true, IdleMin = 20, IdleMax = 20,
        });
        client.Blocks.Remove((0, 63, 1)); // a hole behind: no step
        client.Run(mining.Tick, 20 * 25);
        Assert.Empty(client.TicksOf("step"));
    }

    [Fact]
    public void MiningNeedsTerrainAndOneModeRunsAtATime()
    {
        Client client = new() { Terrain = false };
        Assert.Equal("terrain_disabled", new AgentMining(client, _ => { }, (_, _) => { }).Start(new AutoSettings()));
    }
}
