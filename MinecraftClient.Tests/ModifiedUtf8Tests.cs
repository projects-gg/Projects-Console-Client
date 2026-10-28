using System.Text;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

/// <summary>
/// NBT strings are Java's modified UTF-8 (DataOutput.writeUTF). The client read them as plain UTF-8, so every
/// character outside the Basic Multilingual Plane, such as the SMP's staff and partner prefixes (U+1FA93 axe,
/// U+1F3A5 movie camera), arrived as six U+FFFD in chat, titles and item names from 1.20.3.
/// </summary>
public sealed class ModifiedUtf8Tests
{
    private const string Axe = "\U0001FA93";
    private const string Camera = "\U0001F3A5";

    // What a Java server writes for them: each UTF-16 surrogate as three bytes.
    private static readonly byte[] AxeBytes = [0xED, 0xA0, 0xBE, 0xED, 0xBA, 0x93];
    private static readonly byte[] CameraBytes = [0xED, 0xA0, 0xBC, 0xED, 0xBE, 0xA5];

    private static byte[] Bytes(params object[] parts) => parts.SelectMany(part => part switch
    {
        byte[] raw => raw,
        string text => Encoding.ASCII.GetBytes(text),
        int single => new[] { (byte)single },
        _ => throw new ArgumentException("bytes, ASCII text or one byte"),
    }).ToArray();

    [Fact]
    public void EmojiOutsideTheBmpAreReadAsJavaWritesThem()
    {
        Assert.Equal(Axe, ModifiedUtf8.Decode(AxeBytes));
        Assert.Equal(Camera, ModifiedUtf8.Decode(CameraBytes));
        Assert.Equal($"{Axe}Hazar: selam", ModifiedUtf8.Decode(Bytes(AxeBytes, "Hazar: selam")));
        Assert.Equal($"[{Camera} Partner] Ada", ModifiedUtf8.Decode(Bytes("[", CameraBytes, " Partner] Ada")));

        // The symptom of 4.2.0: the plain UTF-8 reader turned each emoji into six replacement characters.
        Assert.Equal(new string('�', 6), Encoding.UTF8.GetString(AxeBytes));
    }

    [Fact]
    public void EmojiAreWrittenAsTwoThreeByteSurrogates()
    {
        Assert.Equal(AxeBytes, ModifiedUtf8.Encode(Axe));
        Assert.Equal(Bytes(CameraBytes, " Ada"), ModifiedUtf8.Encode($"{Camera} Ada"));
        Assert.Equal(6, ModifiedUtf8.GetByteCount(Axe));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Elmas Kiti")]
    [InlineData("minecraft:diamond")]
    [InlineData("{\"text\":\"[SMP] \",\"color\":\"gold\"}")]
    public void PlainAsciiIsUnchanged(string text)
    {
        byte[] ascii = Encoding.ASCII.GetBytes(text);

        Assert.Equal(ascii, ModifiedUtf8.Encode(text));
        Assert.Equal(text, ModifiedUtf8.Decode(ascii));
        Assert.Equal(Encoding.UTF8.GetString(ascii), ModifiedUtf8.Decode(ascii));
    }

    [Theory]
    [InlineData("Savaşçı Kiti")]
    [InlineData("Kâğıt, İğne, Şifre: ****")]
    [InlineData("€ ⏎ ᴛᴀᴋᴀs ")]
    [InlineData("\u0080߿ࠀ￿")]
    public void OtherBmpTextIsTheSameAsUtf8(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);

        Assert.Equal(utf8, ModifiedUtf8.Encode(text));
        Assert.Equal(text, ModifiedUtf8.Decode(utf8));
    }

    [Fact]
    public void NulIsTwoBytesAndBothFormsRead()
    {
        Assert.Equal(new byte[] { 0xC0, 0x80 }, ModifiedUtf8.Encode("\0"));
        Assert.Equal(Bytes("a", 0xC0, 0x80, "b"), ModifiedUtf8.Encode("a\0b"));
        Assert.Equal(2, ModifiedUtf8.GetByteCount("\0"));

        Assert.Equal("a\0b", ModifiedUtf8.Decode(Bytes("a", 0xC0, 0x80, "b")));
        Assert.Equal("a\0b", ModifiedUtf8.Decode(Bytes("a", 0, "b")));
        Assert.Equal("\0", ModifiedUtf8.Decode([0xE0, 0x80, 0x80]));
    }

    [Fact]
    public void LongerFormsAreTakenAsJavaTakesThem()
    {
        Assert.Equal("A", ModifiedUtf8.Decode([0xC1, 0x81]));
        Assert.Equal("/", ModifiedUtf8.Decode([0xE0, 0x80, 0xAF]));
    }

    [Theory]
    [InlineData(new byte[] { 0xED, 0xA0, 0xBE }, "�")] // a high surrogate alone
    [InlineData(new byte[] { 0xED, 0xBA, 0x93 }, "�")] // a low surrogate alone
    [InlineData(new byte[] { 0xED, 0xBA, 0x93, 0xED, 0xA0, 0xBE }, "��")] // the halves the wrong way round
    [InlineData(new byte[] { 0xED, 0xA0, 0xBE, 0x41 }, "�A")]
    [InlineData(new byte[] { 0xED, 0xA0, 0xBE, 0xED, 0xA0, 0xBE, 0xED, 0xBA, 0x93 }, "�\U0001FA93")]
    [InlineData(new byte[] { 0x41, 0x80, 0x42 }, "A�B")] // a continuation byte without its lead
    [InlineData(new byte[] { 0xBF }, "�")]
    [InlineData(new byte[] { 0xC3 }, "�")] // cut after the lead
    [InlineData(new byte[] { 0xC3, 0x41 }, "�A")]
    [InlineData(new byte[] { 0xE2, 0x82 }, "�")] // one replacement for the broken sequence
    [InlineData(new byte[] { 0xE2, 0x82, 0x41 }, "�A")]
    [InlineData(new byte[] { 0xF0, 0x9F, 0xAA, 0x41 }, "�A")]
    [InlineData(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, "�")] // above U+10FFFF
    [InlineData(new byte[] { 0xF0, 0x80, 0x80, 0x80 }, "�")] // a four-byte form of U+0000
    [InlineData(new byte[] { 0xF8, 0x41, 0xFF }, "�A�")]
    public void BrokenInputBecomesReplacementCharacters(byte[] bytes, string expected)
    {
        Assert.Equal(expected, ModifiedUtf8.Decode(bytes));
    }

    [Fact]
    public void FourByteUtf8FromAServerThatIsNotJavaIsRead()
    {
        Assert.Equal(Axe, ModifiedUtf8.Decode(Encoding.UTF8.GetBytes(Axe)));
        Assert.Equal($"{Camera} Ada", ModifiedUtf8.Decode(Encoding.UTF8.GetBytes($"{Camera} Ada")));
    }

    [Fact]
    public void AnyInputReadsWithoutThrowingAndGivesWellFormedText()
    {
        UTF8Encoding strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        Random random = new(4021);
        for (int run = 0; run < 2000; run++)
        {
            byte[] bytes = new byte[random.Next(0, 40)];
            random.NextBytes(bytes);

            string text = ModifiedUtf8.Decode(bytes);

            // A lone surrogate would make the strict encoder throw (and JSON readers refuse the line).
            strict.GetBytes(text);
            Assert.True(text.Length <= bytes.Length);
        }
    }

    [Fact]
    public void WhatIsWrittenReadsBackTheSame()
    {
        string[] pieces = ["a", "Z", " ", "\0", "ş", "İ", "ğ", "€", "", "￿", Axe, Camera, "\U0010FFFF"];
        Random random = new(421);
        for (int run = 0; run < 500; run++)
        {
            string text = string.Concat(Enumerable.Range(0, random.Next(0, 24)).Select(_ => pieces[random.Next(pieces.Length)]));

            byte[] bytes = ModifiedUtf8.Encode(text);

            Assert.Equal(ModifiedUtf8.GetByteCount(text), bytes.Length);
            Assert.Equal(text, ModifiedUtf8.Decode(bytes));
        }
    }

    [Fact]
    public void AStringLongerThanAnNbtStringCanHoldIsRefused()
    {
        Assert.Equal(ModifiedUtf8.MaxByteCount, ModifiedUtf8.Encode(new string('a', 65535)).Length);
        Assert.Equal(ModifiedUtf8.MaxByteCount, ModifiedUtf8.Encode(new string('€', 21845)).Length);

        Assert.Throws<InvalidDataException>(() => ModifiedUtf8.Encode(new string('a', 65536)));
        Assert.Throws<InvalidDataException>(() => ModifiedUtf8.Encode(new string('€', 21846)));
        Assert.Throws<InvalidDataException>(() => ModifiedUtf8.Encode(new string('ğ', 32768)));
        Assert.Throws<InvalidDataException>(() => ModifiedUtf8.Encode(string.Concat(Enumerable.Repeat(Axe, 10923))));
    }
}
