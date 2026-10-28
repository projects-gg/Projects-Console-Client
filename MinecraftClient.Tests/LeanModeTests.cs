using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>Lean mode (MCC_LEAN, docs/agent-bridge.md "Lean mode"): its parts and the waits and palettes they change.</summary>
public sealed class LeanModeTests
{
    [Theory]
    [InlineData(null, (int)LeanMode.Parts.None)]
    [InlineData("", (int)LeanMode.Parts.None)]
    [InlineData("0", (int)LeanMode.Parts.None)]
    [InlineData("1", (int)LeanMode.Parts.All)]
    [InlineData(" 1 ", (int)LeanMode.Parts.All)]
    [InlineData("palettes", (int)LeanMode.Parts.Palettes)]
    [InlineData("palettes,socket", (int)(LeanMode.Parts.Palettes | LeanMode.Parts.Socket))]
    [InlineData(" book , palettes ", (int)(LeanMode.Parts.Book | LeanMode.Parts.Palettes))]
    [InlineData("socket,unknown", (int)LeanMode.Parts.Socket)]
    [InlineData("yes", (int)LeanMode.Parts.None)]
    public void ParsesTheParts(string? value, int expected)
    {
        Assert.Equal((LeanMode.Parts)expected, LeanMode.Parse(value));
    }

    [Fact]
    public void ALazyPaletteIsBuiltOnItsFirstUseOnly()
    {
        int built = 0;
        Lazy<object> palette = LeanMode.Palette(lazy: true, "test", () => { built++; return new object(); });

        Assert.Equal(0, built);
        object first = palette.Value;
        Assert.Same(first, palette.Value);
        Assert.Equal(1, built);
    }

    [Fact]
    public void AnEagerPaletteIsBuiltAtOnceAsBefore()
    {
        int built = 0;
        Lazy<object> palette = LeanMode.Palette(lazy: false, "test", () => { built++; return new object(); });

        Assert.Equal(1, built);
        Assert.True(palette.IsValueCreated);
        _ = palette.Value;
        Assert.Equal(1, built);
    }

    [Fact]
    public void AnEagerPaletteThrowsWhereItAlwaysDid()
    {
        Assert.Throws<NotImplementedException>(() =>
            LeanMode.Palette<object>(lazy: false, "test", () => throw new NotImplementedException()));
    }

    [Fact]
    public async Task WaitForDataReturnsWhenDataComes()
    {
        using var pair = await SocketPair.OpenAsync();

        var clock = Stopwatch.StartNew();
        Task send = Task.Run(async () =>
        {
            await Task.Delay(150);
            await pair.Peer.GetStream().WriteAsync(new byte[] { 1 });
        });
        pair.Wrapper.WaitForData(10_000);
        clock.Stop();
        await send;

        Assert.True(pair.Wrapper.HasDataAvailable());
        Assert.True(clock.ElapsedMilliseconds < 5_000, $"waited {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task WaitForDataReturnsAtOnceWhenDataIsThere()
    {
        using var pair = await SocketPair.OpenAsync();
        await pair.Peer.GetStream().WriteAsync(new byte[] { 1, 2 });
        Assert.True(SpinWait.SpinUntil(pair.Wrapper.HasDataAvailable, TimeSpan.FromSeconds(5)));

        var clock = Stopwatch.StartNew();
        pair.Wrapper.WaitForData(10_000);

        Assert.True(clock.ElapsedMilliseconds < 5_000, $"waited {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task WaitForDataEndsWithoutData()
    {
        using var pair = await SocketPair.OpenAsync();

        var clock = Stopwatch.StartNew();
        pair.Wrapper.WaitForData(LeanMode.SocketWaitMilliseconds);
        clock.Stop();

        Assert.False(pair.Wrapper.HasDataAvailable());
        Assert.True(clock.ElapsedMilliseconds >= LeanMode.SocketWaitMilliseconds / 2, $"waited {clock.ElapsedMilliseconds} ms");
        Assert.True(clock.ElapsedMilliseconds < 5_000, $"waited {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task WaitForDataReturnsWhenThePeerCloses()
    {
        using var pair = await SocketPair.OpenAsync();
        pair.Peer.Client.Shutdown(SocketShutdown.Both);
        pair.Peer.Close();

        var clock = Stopwatch.StartNew();
        pair.Wrapper.WaitForData(10_000);

        Assert.True(clock.ElapsedMilliseconds < 5_000, $"waited {clock.ElapsedMilliseconds} ms");
        Assert.True(pair.Wrapper.HasDataAvailable());
    }

    [Fact]
    public async Task WaitForDataOnAClosedClientDoesNotThrow()
    {
        using var pair = await SocketPair.OpenAsync();
        pair.Client.Close();

        pair.Wrapper.WaitForData(50);
    }

    private sealed class SocketPair : IDisposable
    {
        private readonly TcpListener listener;

        private SocketPair(TcpListener listener, TcpClient client, TcpClient peer)
        {
            this.listener = listener;
            Client = client;
            Peer = peer;
            Wrapper = new SocketWrapper(client);
        }

        public TcpClient Client { get; }

        public TcpClient Peer { get; }

        public SocketWrapper Wrapper { get; }

        public static async Task<SocketPair> OpenAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var client = new TcpClient();
            Task<TcpClient> accept = listener.AcceptTcpClientAsync();
            await client.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
            return new SocketPair(listener, client, await accept);
        }

        public void Dispose()
        {
            Client.Dispose();
            Peer.Dispose();
            listener.Stop();
        }
    }
}

/// <summary>The global block palette left for later (lean mode) and set as before.</summary>
[Collection(BlockPaletteCollection.Name)]
public sealed class LeanBlockPaletteTests
{
    [Fact]
    public void AFactoryRunsOnTheFirstReadOnly()
    {
        BlockPalette previous = Block.Palette;
        try
        {
            int built = 0;
            Block.SetPaletteFactory(() => { built++; return new Palette263(); });
            Assert.Equal(0, built);

            BlockPalette first = Block.Palette;
            Assert.IsType<Palette263>(first);
            Assert.Same(first, Block.Palette);
            Assert.Equal(1, built);
        }
        finally
        {
            Block.Palette = previous;
        }
    }

    [Fact]
    public void ASetPaletteReplacesAFactoryThatNeverRan()
    {
        BlockPalette previous = Block.Palette;
        try
        {
            int built = 0;
            Block.SetPaletteFactory(() => { built++; return new Palette263(); });
            var set = new Palette112();
            Block.Palette = set;

            Assert.Same(set, Block.Palette);
            Assert.Equal(0, built);
        }
        finally
        {
            Block.Palette = previous;
        }
    }
}

/// <summary>Tests that change the global block palette run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BlockPaletteCollection
{
    public const string Name = "BlockPalette";
}
