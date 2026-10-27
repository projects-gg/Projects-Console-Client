using System;
using System.IO;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Java's "modified UTF-8" (<c>DataInput.readUTF</c>, <c>DataOutput.writeUTF</c>), the string form of NBT: tag names
    /// and TAG_String values, so text components from 1.20.3 (chat, titles, item names, lore) and item NBT before
    /// 1.20.5. It differs from UTF-8 in two ways: a character outside the Basic Multilingual Plane (an emoji such as
    /// U+1FA93) is written as its two UTF-16 surrogates, three bytes each, and U+0000 is written as <c>C0 80</c>.
    /// Network strings with a VarInt length are plain UTF-8 and do not use this.
    /// </summary>
    internal static class ModifiedUtf8
    {
        /// <summary>The most bytes an NBT string can have: its length is an unsigned short.</summary>
        public const int MaxByteCount = ushort.MaxValue;

        private const char Replacement = '�';

        /// <summary>
        /// Decode <paramref name="bytes"/> the way Java's <c>readUTF</c> does, so the text is what a player of the game
        /// sees (the two- and three-byte forms are taken as Java takes them). Where Java gives up on the whole packet,
        /// the client does not: an unpaired surrogate and a malformed or truncated sequence become U+FFFD, and a
        /// well-formed four-byte UTF-8 sequence (written by a server that is not Java) becomes its character.
        /// </summary>
        public static string Decode(ReadOnlySpan<byte> bytes)
        {
            if (Ascii.IsValid(bytes))
                return Encoding.ASCII.GetString(bytes);

            // Every sequence below gives at most one char per byte it takes.
            char[] chars = new char[bytes.Length];
            int count = 0;
            int i = 0;
            while (i < bytes.Length)
            {
                int b = bytes[i];
                if (b < 0x80)
                {
                    chars[count++] = (char)b;
                    i++;
                }
                else if (b is >= 0xC0 and <= 0xDF)
                {
                    if (Continues(bytes, i, 1))
                    {
                        chars[count++] = (char)(((b & 0x1F) << 6) | (bytes[i + 1] & 0x3F));
                        i += 2;
                    }
                    else
                    {
                        chars[count++] = Replacement;
                        i++;
                    }
                }
                else if (b is >= 0xE0 and <= 0xEF)
                {
                    // Surrogates come out as they are written and are paired below.
                    if (Continues(bytes, i, 2))
                    {
                        chars[count++] = (char)(((b & 0x0F) << 12) | ((bytes[i + 1] & 0x3F) << 6) | (bytes[i + 2] & 0x3F));
                        i += 3;
                    }
                    else
                    {
                        chars[count++] = Replacement;
                        i += Partial(bytes, i, 2);
                    }
                }
                else if (b is >= 0xF0 and <= 0xF4 && Continues(bytes, i, 3))
                {
                    int codePoint = ((b & 0x07) << 18) | ((bytes[i + 1] & 0x3F) << 12) | ((bytes[i + 2] & 0x3F) << 6)
                                    | (bytes[i + 3] & 0x3F);
                    if (codePoint is >= 0x10000 and <= 0x10FFFF)
                    {
                        codePoint -= 0x10000;
                        chars[count++] = (char)(0xD800 + (codePoint >> 10));
                        chars[count++] = (char)(0xDC00 + (codePoint & 0x3FF));
                    }
                    else
                    {
                        chars[count++] = Replacement;
                    }

                    i += 4;
                }
                else if (b is >= 0xF0 and <= 0xF4)
                {
                    chars[count++] = Replacement;
                    i += Partial(bytes, i, 3);
                }
                else
                {
                    // A continuation byte without its lead, or a byte no sequence starts with.
                    chars[count++] = Replacement;
                    i++;
                }
            }

            Span<char> text = chars.AsSpan(0, count);
            ReplaceUnpairedSurrogates(text);
            return new string(text);
        }

        /// <summary>
        /// The bytes Java's <c>writeUTF</c> writes for <paramref name="text"/>, without the length in front: every UTF-16
        /// unit on its own (a surrogate pair as two three-byte sequences), U+0000 as <c>C0 80</c>.
        /// </summary>
        /// <exception cref="InvalidDataException">The text needs more than <see cref="MaxByteCount"/> bytes.</exception>
        public static byte[] Encode(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            int length = text.Length <= MaxByteCount ? GetByteCount(text) : MaxByteCount + 1;
            if (length > MaxByteCount)
                throw new InvalidDataException(
                    $"An NBT string can hold at most {MaxByteCount} bytes of modified UTF-8; this one needs more.");

            // Only U+0001-U+007F take one byte each.
            if (length == text.Length)
                return Encoding.ASCII.GetBytes(text);

            byte[] bytes = new byte[length];
            int i = 0;
            foreach (char c in text)
            {
                if (c is >= '\u0001' and <= '\u007F')
                {
                    bytes[i++] = (byte)c;
                }
                else if (c <= '߿')
                {
                    bytes[i++] = (byte)(0xC0 | (c >> 6));
                    bytes[i++] = (byte)(0x80 | (c & 0x3F));
                }
                else
                {
                    bytes[i++] = (byte)(0xE0 | (c >> 12));
                    bytes[i++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    bytes[i++] = (byte)(0x80 | (c & 0x3F));
                }
            }

            return bytes;
        }

        /// <summary>How many bytes <see cref="Encode"/> writes for <paramref name="text"/>.</summary>
        public static int GetByteCount(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            int length = 0;
            foreach (char c in text)
                length += c is >= '\u0001' and <= '\u007F' ? 1 : c <= '߿' ? 2 : 3;
            return length;
        }

        /// <summary>The <paramref name="needed"/> bytes after the lead at <paramref name="lead"/> are all continuations.</summary>
        private static bool Continues(ReadOnlySpan<byte> bytes, int lead, int needed)
        {
            if (lead + needed >= bytes.Length)
                return false;

            for (int k = 1; k <= needed; k++)
            {
                if ((bytes[lead + k] & 0xC0) != 0x80)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The bytes one U+FFFD stands for when a sequence breaks off: its lead and the continuation bytes that came
        /// before the break (fewer than <paramref name="needed"/>).
        /// </summary>
        private static int Partial(ReadOnlySpan<byte> bytes, int lead, int needed)
        {
            int taken = 1;
            while (taken <= needed && lead + taken < bytes.Length && (bytes[lead + taken] & 0xC0) == 0x80)
                taken++;
            return Math.Min(taken, needed);
        }

        /// <summary>A surrogate without its other half becomes U+FFFD (it is not text, and JSON readers refuse it).</summary>
        private static void ReplaceUnpairedSurrogates(Span<char> text)
        {
            for (int k = 0; k < text.Length; k++)
            {
                if (char.IsHighSurrogate(text[k]) && k + 1 < text.Length && char.IsLowSurrogate(text[k + 1]))
                    k++;
                else if (char.IsSurrogate(text[k]))
                    text[k] = Replacement;
            }
        }
    }
}
