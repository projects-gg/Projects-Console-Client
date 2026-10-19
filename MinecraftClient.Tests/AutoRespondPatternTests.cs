using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MinecraftClient.ChatBots;

namespace MinecraftClient.Tests;

public sealed class AutoRespondPatternTests
{
    // Rules the launcher accepts (crates/pl-afk/src/pattern.rs tests), the regex lines its matches.ini
    // writer produces for `#`, and other typical chat rules.
    internal static readonly string[] OrdinaryPatterns =
    [
        @"^\[Özel\] (\w+) -> sen: (.*)$", @"(?i)satıyorum\s+\d+", "selam|merhaba",
        @"^(?<oyuncu>[A-Za-z0-9_]{3,16}) katıldı$", @"\bAFK\b", @"\p{Lu}{2,}", @"[\p{L}\d]+", @"\x23ç",
        "(?im:^hey)", @"fiyat: \$\d+(\.\d{1,2})?", @"a\%b\/c\-d", "(?s).*", "(ab)+", "(a?)+", "a{1000}",
        "(?:x?y)*", "(?:a|b)+$", @"(\s|\S)*x", @"(.|\n)*x", @"(\.|\w)+$", @"(\d|,)+$", "(ab|b)+$", "(a?)+$",
        @"(\d,?)+$", @"(-?\d)+$", "(?:merhaba|selam)+$", @"^(\w+\s?)$", "(?:a|b)+(?:a|b)+$", ".*a.*a.*a!",
        "(?:.*)a.*a.*a!", "(?i).*a.*a.*a!", "x.*a.*a!", @"^\[.*\] .* -> .*: .*$", "^.{0,100}a.{0,100}a.{0,100}a!",
        @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", @"(?i)^\[.*\] (\w+): .*(satıyorum|alıyorum).*$", ".*selam.*",
        "[^:]*: [^>]*> .*!", @"\w+\s\w+\s\w+\s\w+\s\w+!", @"^(.*) -> sen: (.*)$", "(a?){0,20}a{20}", "(?:a|b?)+c",
        @"\x23genel", @"[\x23!]a\\\x23b", @"(?i)\x231 \(en iyi\) oyuncu\?", @"^<(\w+)> (.+)$",
        @"(?i)\b(selam|sa|slm)\b", @"(\w+) seni (\w+) ile öldürdü", @"(?i)afk ?m[ıi]s[ıi]n", @"(\d+)\s*(tl|₺)",
        "(a|ab)(c|bcd)(d*)", "(a+?)(a*)", @"(\w+?)(\d*)$", "(?<x>a)|(?<y>b)", "a(b|bc)*c",
    ];

    internal static readonly string[] ChatLines =
    [
        "[Özel] Ali -> sen: selam nasılsın", "<Ali> merhaba millet", "Ali_123 katıldı", "SATIYORUM 64 elmas",
        "fiyat: $12.50", "192.168.1.1", "#genel herkese selam", "#1 (EN iyi) OYUNCU?", "#a\\#b", "a%b/c-d",
        "hey\nhey", "AFK", "afk mısın", "Ali seni Veli ile öldürdü", "50 tl", "100₺", "abcbcd", "aaab", "",
        "[Satış] Ali: kılıç satıyorum ucuz", "x a a!", "a b c d e!", "1,2,3", "-1-2", "#ç", ": ._,1#- Ba,.. c1x\nb,1b :cBa#",
    ];

    private readonly List<string> log = [];
    private readonly List<string> debugLog = [];

    private AutoRespondPattern Create(string pattern)
    {
        Assert.True(AutoRespondPattern.TryCreate(pattern, log.Add, debugLog.Add, out AutoRespondPattern? result), pattern);
        return result;
    }

    [Fact]
    public void OrdinaryPatternsMatchExactlyLikeBefore()
    {
        var random = new Random(4001);
        const string alphabet = "abcxyABC 1#-.,:>[]\n_ıİçÇ";
        List<string> lines = [.. ChatLines];
        for (int i = 0; i < 200; i++)
        {
            var line = new StringBuilder();
            for (int j = random.Next(0, 64); j > 0; j--)
                line.Append(alphabet[random.Next(alphabet.Length)]);
            lines.Add(line.ToString());
        }

        foreach (string pattern in OrdinaryPatterns)
        {
            var before = new Regex(pattern); // How AutoRespond 4.0.0 built every rule.
            AutoRespondPattern after = Create(pattern);
            foreach (string line in lines)
            {
                Match expected = before.Match(line);
                Match? actual = after.Match(line);
                Assert.True(expected.Success == actual is not null, $"{pattern} on \"{line}\"");
                if (actual is null)
                    continue;
                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Length, actual.Length);
                Assert.Equal(expected.Groups.Count, actual.Groups.Count);
                for (int group = 0; group < expected.Groups.Count; group++)
                    Assert.Equal(expected.Groups[group].Value, actual.Groups[group].Value);
            }
            Assert.False(after.IsNonBacktracking, pattern);
            Assert.Equal(pattern, after.ToString());
        }
        Assert.Empty(log);
        Assert.Empty(debugLog);
    }

    [Fact]
    public void HashWrittenAsX23StillMatchesHash()
    {
        // The launcher writes `#` as \x23 because MCC's matches.ini reader cuts every line at `#`.
        Assert.Equal("#genel", Create(@"\x23genel").Match("#genel herkese")?.Value);
        Assert.Equal(@"#a\#b", Create(@"[\x23!]a\\\x23b").Match(@"x #a\#b")?.Value);
        Assert.Equal("#1 (EN iyi) OYUNCU?", Create(@"(?i)\x231 \(en iyi\) oyuncu\?").Match("#1 (EN iyi) OYUNCU?")?.Value);
        Assert.Null(Create(@"\x23genel").Match("genel"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void CaseInsensitivityReadsTheCultureLikeBefore(string cultureName)
    {
        CultureInfo saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : new CultureInfo(cultureName);
            foreach (string pattern in new[] { "(?i)ılık", "(?i)iyi", "(?i)İYİ", "(?i)IŞIK", "(?i)selam" })
            {
                var before = new Regex(pattern);
                var nonBacktracking = new Regex(pattern, RegexOptions.NonBacktracking);
                AutoRespondPattern after = Create(pattern);
                foreach (string line in new[] { "ILIK", "ılık", "İYİ", "IYI", "iyi", "ışık", "IŞIK", "Işık", "SELAM", "Selam" })
                {
                    Assert.True(before.IsMatch(line) == after.Match(line) is not null, $"{pattern} on {line}");
                    // The engine a rule moves to reads (?i) the same way.
                    Assert.True(before.IsMatch(line) == nonBacktracking.IsMatch(line), $"{pattern} on {line}");
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void InvalidPatternsAreRefused()
    {
        foreach (string pattern in new[] { "[abc", "a)", "a{2,1}", @"\p{Greek", "(?<a" })
            Assert.False(AutoRespondPattern.TryCreate(pattern, log.Add, debugLog.Add, out _), pattern);
    }

    [Fact]
    public void PatternsTheNonBacktrackingEngineCanNotRunStillMatch()
    {
        AutoRespondPattern backreference = Create(@"(\w)\1");
        Assert.False(backreference.SupportsNonBacktracking);
        Assert.Equal("ll", backreference.Match("hello")?.Value);
        Assert.Equal("l", backreference.Match("hello")?.Groups[1].Value);

        Assert.Equal("b", Create("(?<=a)b").Match("cbab")?.Value);
        Assert.Equal(3, Create("(?<=a)b").Match("cbab")?.Index);
        Assert.Null(Create("(?>a+)ab").Match("aaab"));
        Assert.Equal("selam", Create("(?!sa)s[a-z]+").Match("sa selam")?.Value);
        Assert.True(Create("a").SupportsNonBacktracking);
    }

    // The slow shapes the launcher refuses today (pl-afk pattern.rs), each on a 256-character line
    // built to make the backtracking engine try every way to split it.
    internal static readonly (string Pattern, string Line)[] SlowShapes =
    [
        ("(a+)+$", Line("a", "!")),
        (@"(\w+\s?)*$", Line("a", "!")),
        ("(a*)*b", Line("a", "")),
        ("(a|aa)+$", Line("a", "!")),
        ("(a|a?)+$", Line("a", "!")),
        (@"(.|\s)*x", Line(" ", "")),
        ("(a?a)+$", Line("a", "!")),
        ("(aa?)+$", Line("a", "!")),
        ("(a|b|ab)+$", Line("ab", "!")),
        ("(ab?|b)+$", Line("ab", "!")),
        (".*a.*a.*a.*a!", Line("a", "")),
        ("a.*a.*a.*a!", Line("a", "")),
        (@"\w*a\w*a\w*a\w*a!", Line("a", "")),
        ("(?m)^.*a.*a.*a.*a!", Line("a", "")),
        ("(?:.*a)?(?:.*a)?(?:.*a)?!", Line("a", "")),
        (".*a.*b.*c.*d", Line("abc", "")),
        ("(.*)a.*a.*a!", Line("a", "")),
        ("(a?){25}a{25}", Line("aaaaaaaaaaaaaaaaaaaaaaaa!", "")),
        ("(?:a|b?){25}c", Line("a", "")),
        ("(?:a?b?)+c", Line("ab", "")),
        ("(?:a?b?c?)+d", Line("abc", "")),
    ];

    /// <summary>A 256-character line: <paramref name="unit"/> repeated, ending with <paramref name="end"/>.</summary>
    internal static string Line(string unit, string end)
    {
        var line = new StringBuilder(256);
        while (line.Length < 256 - end.Length)
            line.Append(unit);
        line.Length = 256 - end.Length;
        return line.Append(end).ToString();
    }

    [Fact]
    public void SlowShapesNeverStallTheClient()
    {
        foreach ((string pattern, string line) in SlowShapes)
        {
            AutoRespondPattern rule = Create(pattern);
            Assert.True(rule.SupportsNonBacktracking, pattern);
            // Most of these lines do not match; `(\w+\s?)*$` and `(a|a?)+$` match empty at the end.
            Match expected = new Regex(pattern, RegexOptions.NonBacktracking).Match(line);

            var watch = Stopwatch.StartNew();
            Match? actual = rule.Match(line);
            // At most one match timeout, then the non-backtracking engine. The bound is loose for slow CI
            // machines: before this change these lines took from seconds to longer than the age of the universe.
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"{pattern}: first line took {watch.Elapsed}");
            Assert.True(expected.Success == actual is not null, pattern);
            Assert.Equal(expected.Index, actual?.Index ?? 0);
            Assert.Equal(expected.Length, actual?.Length ?? 0);

            watch.Restart();
            for (int i = 0; i < 20; i++)
                Assert.True(expected.Success == rule.Match(line) is not null, pattern);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"{pattern}: 20 more lines took {watch.Elapsed}");
        }
        Assert.Empty(log); // Rules that move to the non-backtracking engine keep working: nothing to tell the player.
    }

    [Fact]
    public void ARuleThatMovedToTheNonBacktrackingEngineStillMatches()
    {
        AutoRespondPattern rule = Create("^(a|aa)+$");
        Assert.Null(rule.Match(Line("a", "!")));
        Assert.True(rule.IsNonBacktracking);
        Assert.Single(debugLog);
        Assert.Contains("^(a|aa)+$", debugLog[0]);

        Match? match = rule.Match("aaaa");
        Assert.NotNull(match);
        Assert.Equal("aaaa", match.Value);
        Assert.Null(rule.Match("aaab"));

        AutoRespondPattern words = Create(@"^(\w+\s?)*$");
        Assert.Null(words.Match(Line("a", "!")));
        Assert.True(words.IsNonBacktracking);
        Assert.Equal("selam millet", words.Match("selam millet")?.Value);
    }

    [Fact]
    public void ARuleStuckOnTheBacktrackingEngineTimesOutAsNoMatchAndIsTurnedOff()
    {
        // The lookahead keeps this catastrophic pattern off the non-backtracking engine.
        AutoRespondPattern rule = Create("^(?=a)(a|aa)+$");
        Assert.False(rule.SupportsNonBacktracking);
        string slow = Line("a", "!");

        Assert.Null(rule.Match(slow));
        Assert.Single(log); // One line on the first timeout...
        Assert.Contains("^(?=a)(a|aa)+$", log[0]);
        Assert.Equal("aaaa", rule.Match("aaaa")?.Value); // ...and the rule keeps working on other lines.

        for (int i = 1; i < AutoRespondPattern.TimeoutLimit; i++)
            Assert.Null(rule.Match(slow));
        Assert.True(rule.IsTurnedOff);
        Assert.Equal(2, log.Count); // ...and one more when it is turned off, never one per line.

        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
            Assert.Null(rule.Match(slow));
        Assert.Null(rule.Match("aaaa"));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"a turned-off rule took {watch.Elapsed}");
        Assert.Equal(2, log.Count);
        Assert.Empty(debugLog);
    }

    [Fact]
    public void LoadingSkipsOnlyTheRuleWithAnInvalidPattern()
    {
        string file = Path.Combine(Path.GetTempPath(), $"mcc-autorespond-{Guid.NewGuid():N}.ini");
        string saved = AutoRespond.Config.Matches_File;
        try
        {
            File.WriteAllText(file, string.Join("\r\n",
                "# Projects Launcher",
                "[Match]", @"regex=^selam (\w+)$", "action=send merhaba $1", "ownersonly=false", "cooldown=0", "",
                "[Match]", "regex=[bozuk", "action=send olmaz", "",
                "[Match]", "match=bozuk", "regex=(eksik", "action=send olmaz", "",
                "[Match]", @"regex=\x23genel", "actionother=send genel", "",
                "[Match]", "match=iyi geceler", "action=send iyi geceler", ""), new UTF8Encoding(false));
            AutoRespond.Config.Matches_File = file;

            var bot = new AutoRespond();
            bot.Initialize();

            var rules = (System.Collections.IList)typeof(AutoRespond)
                .GetField("respondRules", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bot)!;
            // 4.0.0 threw at "[bozuk" and lost that rule and every later one; now only the two broken
            // rules are dropped (the second one also had a match= line).
            Assert.Equal(3, rules.Count);
            string[] described = rules.Cast<object>().Select(rule => rule.ToString()!).ToArray();
            Assert.Contains(described, rule => rule.Contains(@"^selam (\w+)$"));
            Assert.Contains(described, rule => rule.Contains(@"\x23genel"));
            Assert.Contains(described, rule => rule.Contains("iyi geceler"));
            Assert.DoesNotContain(described, rule => rule.Contains("bozuk"));
        }
        finally
        {
            AutoRespond.Config.Matches_File = saved;
            File.Delete(file);
        }
    }
}
