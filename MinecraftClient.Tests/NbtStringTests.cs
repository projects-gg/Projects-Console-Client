using System.Text;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// NBT strings through <see cref="DataTypes"/>, as Java servers write them (modified UTF-8): text components from
/// 1.20.3 (chat, titles, team prefixes, item names), tag names, and item NBT before 1.20.5, read and written back.
/// </summary>
public sealed class NbtStringTests
{
    private const int Mc263 = Protocol18Handler.MC_26_3_Version;
    private const int Mc1122 = Protocol18Handler.MC_1_12_2_Version;
    private const string Axe = "\U0001FA93";
    private const string Camera = "\U0001F3A5";

    private static readonly byte[] AxeBytes = [0xED, 0xA0, 0xBE, 0xED, 0xBA, 0x93];
    private static readonly byte[] CameraBytes = [0xED, 0xA0, 0xBC, 0xED, 0xBE, 0xA5];

    /// <summary>An NBT string as Java writes it: the byte count as an unsigned short, then the bytes.</summary>
    private static byte[] Str(byte[] modifiedUtf8) => [(byte)(modifiedUtf8.Length >> 8), (byte)modifiedUtf8.Length, .. modifiedUtf8];

    private static byte[] Str(string ascii) => Str(Encoding.ASCII.GetBytes(ascii));

    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();

    [Fact]
    public void TheStaffPrefixOfA263ChatLineIsKept()
    {
        Queue<byte> packet = new(Cat([8], Str(Cat(AxeBytes, "Hazar: selam"u8.ToArray()))));

        string text = new DataTypes(Mc263).ReadNextChat(packet);

        Assert.Empty(packet);
        Assert.Equal($"{Axe}Hazar: selam", text);
    }

    [Fact]
    public void CompoundTextAndTagNamesAreRead()
    {
        // {text: "<camera> Partner", "ad_ğ": "Kâğıt"} without a root name (1.20.2+).
        Queue<byte> packet = new(Cat([10],
            [8], Str("text"), Str(Cat(CameraBytes, " Partner"u8.ToArray())),
            [8], Str(Encoding.UTF8.GetBytes("ad_ğ")), Str(Encoding.UTF8.GetBytes("Kâğıt")),
            [0]));

        Dictionary<string, object> nbt = new DataTypes(Mc263).ReadNextNbt(packet);

        Assert.Empty(packet);
        Assert.Equal($"{Camera} Partner", nbt["text"]);
        Assert.Equal("Kâğıt", nbt["ad_ğ"]);
    }

    [Fact]
    public void ItemNbtBefore1205ReadsItsNameAndRootName()
    {
        // A root compound named "ğ" holding display.Name, the item name of 1.12.2.
        Queue<byte> packet = new(Cat([10], Str(Encoding.UTF8.GetBytes("ğ")),
            [10], Str("display"),
            [8], Str("Name"), Str(Cat(AxeBytes, " Balta"u8.ToArray())),
            [0],
            [0]));

        Dictionary<string, object> nbt = new DataTypes(Mc1122).ReadNextNbt(packet);

        Assert.Empty(packet);
        Assert.Equal("ğ", nbt[""]);
        Assert.Equal($"{Axe} Balta", ((Dictionary<string, object>)nbt["display"])["Name"]);
    }

    [Fact]
    public void WhatTheClientWritesIsWhatJavaWrites()
    {
        DataTypes data = new(Mc263);

        Assert.Equal(Cat([10], [8], Str("text"), Str(Cat(CameraBytes, " Partner"u8.ToArray())), [0]),
            data.GetNbt(new Dictionary<string, object> { ["text"] = $"{Camera} Partner" }));
        Assert.Equal(Cat([8], Str(AxeBytes)), data.GetNbt(new Dictionary<string, object> { [""] = Axe }));
        Assert.Equal(Cat([8], Str(AxeBytes)), data.GetNbtTag(Axe));

        // U+0000 as C0 80; a tag name's length counts bytes, not characters.
        Assert.Equal(Cat([10], [8], Str(Encoding.UTF8.GetBytes("ad_ğ")), Str([0x61, 0xC0, 0x80, 0x62]), [0]),
            data.GetNbt(new Dictionary<string, object> { ["ad_ğ"] = "a\0b" }));
    }

    [Fact]
    public void PlainAsciiNbtIsWrittenAsBefore()
    {
        byte[] expected = [10, 8, 0, 4, (byte)'t', (byte)'e', (byte)'x', (byte)'t', 0, 10, .. "Elmas Kiti"u8, 0];

        Assert.Equal(expected, new DataTypes(Mc263).GetNbt(new Dictionary<string, object> { ["text"] = "Elmas Kiti" }));
    }

    [Fact]
    public void ItemNbtGoesBackByteForByte()
    {
        // 1.12.2 item NBT with an emoji name, a lore line holding U+0000 and a Turkish tag name.
        byte[] java = Cat([10], Str(""),
            [10], Str("display"),
            [8], Str("Name"), Str(Cat("{\"text\":\""u8.ToArray(), AxeBytes, "\"}"u8.ToArray())),
            [9], Str("Lore"), [8], [0, 0, 0, 1], Str([0xC4, 0x9F, 0xC0, 0x80]),
            [0],
            [3], Str(Encoding.UTF8.GetBytes("sayı")), [0, 0, 0, 7],
            [0]);
        DataTypes data = new(Mc1122);

        Dictionary<string, object> nbt = data.ReadNextNbt(new Queue<byte>(java));

        Assert.Equal("ğ\0", ((object[])((Dictionary<string, object>)nbt["display"])["Lore"])[0]);
        Assert.Equal(java, data.GetNbt(nbt));
    }
}
