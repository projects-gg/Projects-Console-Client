using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinecraftClient.Dialogs;

/// <summary>
/// A dialog's values written as the game writes them (client 4.2.5; Minecraft's own <c>InputControlHandlers</c>,
/// <c>StringTag.escapeWithoutQuotes</c>, <c>StringTemplate</c> and <c>NumberRangeInput.RangeInfo</c>), so that what
/// a button sends is exactly what the game would send: a dynamic command's template filled in, a text escaped, a
/// slider's number in Java's own text.
/// </summary>
public static class DialogValues
{
    /// <summary>
    /// A text input's value in a command template (the game's <c>StringTag.escapeWithoutQuotes</c>): <c>"</c>,
    /// <c>'</c> and <c>\</c> escaped with a backslash, <c>\b \t \n \f \r</c> as such, other control characters as
    /// <c>\xHH</c>.
    /// </summary>
    public static string EscapeText(string value)
    {
        StringBuilder escaped = new(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '"' or '\'' or '\\':
                    escaped.Append('\\').Append(c);
                    break;
                case '\b':
                    escaped.Append("\\b");
                    break;
                case '\t':
                    escaped.Append("\\t");
                    break;
                case '\n':
                    escaped.Append("\\n");
                    break;
                case '\f':
                    escaped.Append("\\f");
                    break;
                case '\r':
                    escaped.Append("\\r");
                    break;
                case < ' ':
                    escaped.Append("\\x").Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                    break;
                default:
                    escaped.Append(c);
                    break;
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    /// A character a text input takes (the game's <c>StringUtil.isAllowedChatCharacter</c>: no section sign, no
    /// control character, no DEL), a line break only in a multiline input.
    /// </summary>
    public static bool AllowedInText(char c, bool multiline) =>
        c == '\n' ? multiline : c != '§' && c >= ' ' && c != '\u007F';

    /// <summary>
    /// A slider's value in a command template (the game's <c>valueToString</c>): a whole number as an integer (Java's
    /// <c>(int)</c> cast), anything else as Java's <c>Float.toString</c>.
    /// </summary>
    public static string SliderText(float value)
    {
        int whole = JavaInt(value);
        return whole == value ? whole.ToString(CultureInfo.InvariantCulture) : JavaFloat(value);
    }

    /// <summary>Java's <c>(int)</c> cast of a float: NaN is 0, the rest saturates.</summary>
    private static int JavaInt(float value) =>
        float.IsNaN(value) ? 0 : value >= 2147483647F ? int.MaxValue : value <= -2147483648F ? int.MinValue : (int)value;

    /// <summary>
    /// Java's <c>Float.toString</c>: the shortest digits that read back as <paramref name="value"/>, as a plain decimal
    /// with at least one fraction digit from 10^-3 up to 10^7, else as <c>d.dddE±n</c>.
    /// </summary>
    public static string JavaFloat(float value)
    {
        if (float.IsNaN(value))
            return "NaN";
        if (float.IsInfinity(value))
            return value > 0 ? "Infinity" : "-Infinity";
        if (value == 0)
            return BitConverter.SingleToInt32Bits(value) < 0 ? "-0.0" : "0.0";

        (bool negative, string digits, int pointAt) = Shortest(value);
        // Scientific exponent: value = d.ddd × 10^sci.
        int sci = pointAt - 1;
        StringBuilder text = new();
        if (negative)
            text.Append('-');
        if (sci is >= -3 and < 7)
        {
            AppendPlain(text, digits, pointAt, ".0");
        }
        else
        {
            text.Append(digits[0]).Append('.').Append(digits.Length > 1 ? digits[1..] : "0")
                .Append('E').Append(sci.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary>
    /// A float as the shortest decimal that reads back as it, never with an exponent (as Rust writes an <c>f32</c>):
    /// how a card passes a slider's value.
    /// </summary>
    public static string Plain(float value)
    {
        if (!float.IsFinite(value) || value == 0)
            return value == 0 ? "0" : value.ToString(CultureInfo.InvariantCulture);
        (bool negative, string digits, int pointAt) = Shortest(value);
        StringBuilder text = new();
        if (negative)
            text.Append('-');
        AppendPlain(text, digits, pointAt, string.Empty);
        return text.ToString();
    }

    /// <summary>
    /// A finite, non-zero float's shortest round-trip digits (.NET's "R"), without leading or trailing zeros, and how
    /// many of them stand before the decimal point (value = 0.digits × 10^pointAt).
    /// </summary>
    private static (bool Negative, string Digits, int PointAt) Shortest(float value)
    {
        string shortest = value.ToString("R", CultureInfo.InvariantCulture);
        bool negative = shortest[0] == '-';
        if (negative)
            shortest = shortest[1..];
        int e = shortest.IndexOfAny(['E', 'e']);
        int exponent = e < 0 ? 0 : int.Parse(shortest[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        string mantissa = e < 0 ? shortest : shortest[..e];
        int point = mantissa.IndexOf('.');
        string digits = point < 0 ? mantissa : mantissa.Remove(point, 1);
        int pointAt = (point < 0 ? mantissa.Length : point) + exponent;
        int lead = 0;
        while (lead < digits.Length - 1 && digits[lead] == '0')
            lead++;
        return (negative, digits[lead..].TrimEnd('0'), pointAt - lead);
    }

    /// <summary>Digits as a plain decimal; <paramref name="wholeEnd"/> ends a whole number (Java writes ".0").</summary>
    private static void AppendPlain(StringBuilder text, string digits, int pointAt, string wholeEnd)
    {
        if (pointAt <= 0)
            text.Append("0.").Append('0', -pointAt).Append(digits);
        else if (pointAt >= digits.Length)
            text.Append(digits).Append('0', pointAt - digits.Length).Append(wholeEnd);
        else
            text.Append(digits, 0, pointAt).Append('.').Append(digits, pointAt, digits.Length - pointAt);
    }

    /// <summary>
    /// The value of a slider of <c>[start, end]</c> at <paramref name="place"/> (0 to 1), as the game computes it
    /// (<c>RangeInfo.computeScaledValue</c>, in floats): with a step, the nearest <c>initial + n × step</c> inside the
    /// range (the game counts its steps from the initial value).
    /// </summary>
    public static float SliderValue(float place, float start, float end, float? step, float initial)
    {
        float lerped = start + place * (end - start);
        if (step is not { } size)
            return lerped;
        int n = JavaRound((lerped - initial) / size);
        float value = initial + n * size;
        if (!OutOfRange(value, start, end))
            return value;
        int back = n - Math.Sign(n);
        return initial + back * size;
    }

    /// <summary>The value a slider shows before it is moved: at its initial value's place.</summary>
    public static float UntouchedSlider(float start, float end, float? step, float initial) =>
        SliderValue(Place(initial, start, end), start, end, step, initial);

    /// <summary>
    /// Whether a slider can take <paramref name="value"/>: within <c>[start, end]</c>, and with a step one of
    /// <c>initial + n × step</c>.
    /// </summary>
    public static bool SliderTakes(float value, float start, float end, float? step, float initial)
    {
        // The slider's far end is start + (end - start) in floats, which may be a hair past end.
        if (!float.IsFinite(value) || (OutOfRange(value, start, end) && value != start + (end - start)))
            return false;
        if (step is not { } size)
            return true;
        int n = JavaRound((value - initial) / size);
        float candidate = initial + n * size;
        return candidate == value;
    }

    /// <summary>The game's <c>scaledValueToSlider</c>: a value's place on the slider.</summary>
    private static float Place(float value, float start, float end) =>
        start == end ? 0.5F : (value - start) / (end - start);

    private static bool OutOfRange(float value, float start, float end)
    {
        float place = Place(value, start, end);
        return place < 0 || place > 1;
    }

    /// <summary>Java's <c>Math.round(float)</c>: the nearest int, a half up, saturating.</summary>
    private static int JavaRound(float value)
    {
        if (float.IsNaN(value))
            return 0;
        double rounded = Math.Floor((double)value + 0.5);
        return rounded >= int.MaxValue ? int.MaxValue : rounded <= int.MinValue ? int.MinValue : (int)rounded;
    }

    /// <summary>
    /// A dynamic command's template as the game reads it (<c>StringTemplate.fromString</c>): text and <c>$(name)</c>
    /// variables, a name of letters, digits and <c>_</c> (possibly empty). Null when the game would refuse it: a
    /// <c>$(</c> without <c>)</c>, a name of other characters, or no variable at all.
    /// </summary>
    public static (List<string> Segments, List<string> Variables)? ParseTemplate(string template)
    {
        List<string> segments = [];
        List<string> variables = [];
        int start = 0;
        int dollar = template.IndexOf('$');
        while (dollar != -1)
        {
            if (dollar == template.Length - 1 || template[dollar + 1] != '(')
            {
                dollar = template.IndexOf('$', dollar + 1);
                continue;
            }

            segments.Add(template[start..dollar]);
            int close = template.IndexOf(')', dollar + 1);
            if (close == -1)
                return null;
            string name = template[(dollar + 2)..close];
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return null;
            }

            variables.Add(name);
            start = close + 1;
            dollar = template.IndexOf('$', start);
        }

        if (start == 0)
            return null;
        if (start != template.Length)
            segments.Add(template[start..]);
        return (segments, variables);
    }

    /// <summary>
    /// A dynamic command's template filled in as the game fills it (<c>ParsedTemplate.instantiate</c>): each variable
    /// once, never inside a value, an unknown one as nothing. Null when the game would refuse the template.
    /// </summary>
    public static string? FillTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        var parsed = ParseTemplate(template);
        if (parsed is null)
            return null;
        (List<string> segments, List<string> variables) = parsed.Value;
        StringBuilder filled = new();
        for (int i = 0; i < variables.Count; i++)
        {
            filled.Append(segments[i]);
            filled.Append(values.TryGetValue(variables[i], out string? value) ? value : string.Empty);
        }

        if (segments.Count > variables.Count)
            filled.Append(segments[^1]);
        return filled.ToString();
    }
}
