using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MinecraftClient.ChatBots;

namespace MinecraftClient.Tests;

/// <summary>
/// Strict values (MCC_MATCH_VARS=strict; docs/agent-bridge.md, "Version 2"): the value class and whole runs of one
/// rule against one chat line, from the cloud AFK placeholder vectors (Vectors/rule-placeholder-client-v1.json),
/// and what a script started with strict values may send.
/// </summary>
public sealed class AutoRespondVarsTests
{
    private static readonly JsonObject Vectors = JsonNode.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Vectors", "rule-placeholder-client-v1.json")))!.AsObject();

    private static readonly Regex Placeholder = new(@"\{(u|[1-9])\}");

    /// <summary>A vector's text, also when it is not well-formed (then "utf16" lists its code units).</summary>
    private static string Text(JsonNode node, string field)
    {
        if (node[field] is JsonValue value)
            return (string)value!;
        return new string(node["utf16"]!.AsArray().Select(unit => (char)Convert.ToInt32((string)unit!, 16)).ToArray());
    }

    /// <summary>The names a reply uses, in order of first use: what afk-host writes as vars=.</summary>
    private static string[] VarsOf(string reply) =>
        Placeholder.Matches(reply).Select(m => m.Groups[1].Value).Distinct().ToArray();

    /// <summary>The reply as afk-host writes it in the rule's script: {u} as %match_u%, {N} as %match_N%.</summary>
    private static string ScriptLine(string reply) => Placeholder.Replace(reply, m => $"%match_{m.Groups[1].Value}%");

    private static AutoRespond.RespondRule Rule(JsonNode rule, string[]? vars, TimeSpan cooldown = default)
    {
        const string action = "script s-r1.txt";
        string from = (string)rule["from"]!;
        string? onPublic = from is "any" or "player" ? action : null;
        string? onPrivate = from is "any" or "private" ? action : null;
        string? onOther = from is "any" or "server" ? action : null;
        string match = (string)rule["match"]!;

        if (!(bool)rule["regex"]!)
            return new AutoRespond.RespondRule(match, onPublic, onPrivate, onOther, false, cooldown, vars);

        Assert.True(AutoRespondPattern.TryCreate(match, _ => { }, _ => { }, out AutoRespondPattern? pattern), match);
        return new AutoRespond.RespondRule(pattern, onPublic, onPrivate, onOther, false, cooldown, vars);
    }

    private static AutoRespond.MessageType TypeOf(string type) => type switch
    {
        "public" => AutoRespond.MessageType.Public,
        "private" => AutoRespond.MessageType.Private,
        _ => AutoRespond.MessageType.Other,
    };

    [Fact]
    public void ValuesFollowTheStrictClass()
    {
        JsonArray values = Vectors["values"]!.AsArray();
        Assert.True(values.Count >= 40);

        List<string> wrong = values
            .Where(node => AutoRespond.IsStrictValue(Text(node!, "value")) != (bool)node!["ok"]!)
            .Select(node => (string)node!["name"]!)
            .ToList();

        Assert.Empty(wrong);
    }

    [Fact]
    public void RunsSendWhatTheVectorsSay()
    {
        string self = (string)Vectors["self"]!;
        JsonArray runs = Vectors["runs"]!.AsArray();
        Assert.True(runs.Count >= 30);

        List<string> wrong = new();
        foreach (JsonNode? run in runs)
        {
            JsonNode rule = run!["rule"]!, line = run["line"]!;
            string reply = (string)rule["reply"]!;
            AutoRespond.MessageType type = TypeOf((string)line["type"]!);
            string sender = (string)line["sender"]!, message = (string)line["message"]!;

            string? sent;
            string? why = null;
            if (type != AutoRespond.MessageType.Other && sender == self)
            {
                sent = null; // AutoRespond never answers the bot's own lines
                why = "self";
            }
            else
            {
                AutoRespond.RespondRule respond = Rule(rule, VarsOf(reply));
                string? action = respond.Match(sender, message, type, new Dictionary<string, object>(),
                    out Dictionary<string, object>? scriptVars, out bool refused);
                if (refused)
                {
                    sent = null;
                    why = "value";
                }
                else if (action is null)
                {
                    sent = null;
                    why = Answers((string)rule["from"]!, type) ? "nomatch" : "from";
                }
                else
                {
                    // What the script's send line becomes: only the checked values are there to fill it.
                    sent = Settings.Config.AppVar.ExpandVars(ScriptLine(reply), scriptVars);
                }
            }

            string? expected = run["reply"] is JsonValue value ? (string)value! : null;
            string? expectedWhy = run["why"] is JsonValue reason ? (string)reason! : null;
            if (sent != expected || why != expectedWhy)
                wrong.Add($"{(string)run["name"]!}: sent {sent ?? "nothing"} ({why}), expected {expected ?? "nothing"} ({expectedWhy})");
        }

        Assert.Empty(wrong);
    }

    private static bool Answers(string from, AutoRespond.MessageType type) => from switch
    {
        "any" => true,
        "player" => type == AutoRespond.MessageType.Public,
        "private" => type == AutoRespond.MessageType.Private,
        _ => type == AutoRespond.MessageType.Other,
    };

    [Fact]
    public void ARefusedLineDoesNotStartTheCooldown()
    {
        JsonNode rule = JsonNode.Parse("""{"match":"^!davet (.+)$","regex":true,"from":"any"}""")!;
        AutoRespond.RespondRule respond = Rule(rule, ["u", "1"], TimeSpan.FromMinutes(1));

        Assert.Null(respond.Match("Kaan", "!davet Ali /pay Kaan 1", AutoRespond.MessageType.Public, new(), out _, out bool refused));
        Assert.True(refused);

        Assert.NotNull(respond.Match("Kaan", "!davet Ali", AutoRespond.MessageType.Public, new(), out var vars, out refused));
        Assert.False(refused);
        Assert.Equal(new Dictionary<string, object> { ["match_u"] = "Kaan", ["match_1"] = "Ali" }, vars);

        // Now the cooldown runs: no answer, and no refusal either.
        Assert.Null(respond.Match("Kaan", "!davet Veli", AutoRespond.MessageType.Public, new(), out _, out refused));
        Assert.False(refused);
    }

    [Fact]
    public void OnlyTheNamedValuesReachTheScript()
    {
        JsonNode rule = JsonNode.Parse("""{"match":"^(\\S+) (\\S+) (.+)$","regex":true,"from":"any"}""")!;
        AutoRespond.RespondRule respond = Rule(rule, ["2"]);

        // Group 3 holds a command and a space: it is not checked, and the script never gets it.
        Assert.NotNull(respond.Match("Kaan", "a b /pay Kaan 1", AutoRespond.MessageType.Public, new(), out var vars, out _));
        Assert.Equal(new Dictionary<string, object> { ["match_2"] = "b" }, vars);
        Assert.Equal("%match_3% b %match_0%", Settings.Config.AppVar.ExpandVars("%match_3% %match_2% %match_0%", vars));

        // A rule without vars= gives the script nothing, as before.
        Assert.NotNull(Rule(rule, null).Match("Kaan", "a b c", AutoRespond.MessageType.Public, new(), out var none, out _));
        Assert.Null(none);
    }

    [Fact]
    public void RefusalsAreLoggedAtMostOnceAMinute()
    {
        JsonNode rule = JsonNode.Parse("""{"match":"selam","regex":false,"from":"any"}""")!;
        AutoRespond.RespondRule respond = Rule(rule, ["u"]);
        DateTime now = new(2026, 10, 24, 18, 0, 0);

        Assert.True(respond.ShouldLogRefusal(now));
        Assert.False(respond.ShouldLogRefusal(now.AddSeconds(59)));
        Assert.True(respond.ShouldLogRefusal(now.AddSeconds(60)));
    }

    [Theory]
    [InlineData("u,1", new[] { "u", "1" })]
    [InlineData("1, u ,1", new[] { "1", "u" })]
    [InlineData("9", new[] { "9" })]
    public void VarsLinesNameTheSenderAndParts(string spec, string[] expected)
    {
        Assert.True(AutoRespond.TryParseVars(spec, out string[] vars));
        Assert.Equal(expected, vars);
    }

    [Theory]
    [InlineData("u,x")]
    [InlineData("0")]
    [InlineData("10")]
    [InlineData("U")]
    [InlineData("u,,1")]
    [InlineData("match_1")]
    public void OtherNamesInVarsAreRefused(string spec)
    {
        Assert.False(AutoRespond.TryParseVars(spec, out _));
    }

    [Fact]
    public void StrictRulesWithNamedGroupsOrBadVarsAreNotLoaded()
    {
        Assert.True(AutoRespondPattern.TryCreate(@"^(?<isim>\w+) geldi (\d+)$", _ => { }, _ => { }, out var named));
        Assert.True(AutoRespondPattern.TryCreate(@"^(\w+) geldi (\d+)$", _ => { }, _ => { }, out var unnamed));
        Assert.True(named.HasNamedGroups);
        Assert.False(unnamed.HasNamedGroups);

        bool previous = AutoRespond.StrictValues;
        try
        {
            AutoRespond.StrictValues = true;
            Assert.True(AutoRespond.TryGetStrictVars(unnamed, "u,2", out string[]? vars));
            Assert.Equal(new[] { "u", "2" }, vars);
            Assert.True(AutoRespond.TryGetStrictVars(unnamed, null, out vars));
            Assert.Null(vars); // no vars=: the block runs as before
            Assert.False(AutoRespond.TryGetStrictVars(named, "1", out _));
            Assert.False(AutoRespond.TryGetStrictVars(unnamed, "u,%", out _));

            AutoRespond.StrictValues = false;
            Assert.True(AutoRespond.TryGetStrictVars(named, "1", out vars));
            Assert.Null(vars); // without the variable vars= is ignored
        }
        finally
        {
            AutoRespond.StrictValues = previous;
        }
    }

    [Theory]
    [InlineData(256, 256, false)]
    [InlineData(257, 256, true)]
    [InlineData(101, 100, true)]
    [InlineData(100, 100, false)]
    [InlineData(300, 1000, true)]
    public void AFilledReplyFitsOneChatMessage(int length, int serverLimit, bool overlong)
    {
        Assert.Equal(overlong, Script.IsOverlongReply("send " + new string('a', length), serverLimit));
        Assert.False(Script.IsOverlongReply("wait " + new string('1', length), serverLimit));
    }
}
