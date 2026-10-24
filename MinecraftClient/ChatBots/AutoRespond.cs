using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.Settings;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot automatically runs actions when a user sends a message matching a specified rule
    /// </summary>
    public class AutoRespond : ChatBot
    {
        public static Configs Config = new();

        /// <summary>
        /// "strict": a [Match] block's <c>vars=</c> names the values its action uses (<c>u</c>, the sender, and
        /// <c>1</c>-<c>9</c>, the pattern's groups); the rule answers a line only when each of them passes
        /// <see cref="IsStrictValue"/>, and its script gets exactly those values as <c>%match_u%</c>,
        /// <c>%match_1%</c>, ... (docs/agent-bridge.md, "Version 2"). Without it <c>vars=</c> is ignored.
        /// </summary>
        public const string MatchVarsEnvVar = "MCC_MATCH_VARS";

        /// <summary>Longest value <see cref="IsStrictValue"/> lets through, in UTF-16 code units.</summary>
        public const int MaxStrictValueLength = 24;

        /// <summary>Longest reply a script started with strict values may send (one chat message).</summary>
        public const int MaxFilledReplyLength = 256;

        private static readonly TimeSpan RefusalLogInterval = TimeSpan.FromMinutes(1);

        /// <summary>The host asked for strict values (<see cref="MatchVarsEnvVar"/>=strict).</summary>
        internal static bool StrictValues { get; set; } = Environment.GetEnvironmentVariable(MatchVarsEnvVar) == "strict";

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoRespond";

            public bool Enabled = false;

            public string Matches_File = @"matches.ini";

            [TomlInlineComment("$ChatBot.AutoRespond.Match_Colors$")]
            public bool Match_Colors = false;

            public void OnSettingUpdate()
            {
                Matches_File ??= string.Empty;

                if (!Enabled) return;

                if (!File.Exists(Matches_File))
                {
                    LogToConsole(BotName, string.Format(Translations.bot_autoRespond_file_not_found, Path.GetFullPath(Matches_File)));
                    LogToConsole(BotName, Translations.general_bot_unload);
                    Enabled = false;
                }
            }
        }

        private List<RespondRule>? respondRules;
        internal enum MessageType { Public, Private, Other };

        /// <summary>
        /// A value a strict rule may use: 1-24 characters, each "_" or a letter or number (general category L or N)
        /// of the Basic Multilingual Plane that is not a default ignorable code point (the Hangul fillers are letters
        /// that show nothing). So no value carries a space, ".", "/", "%", "\", a link, a command or another argument.
        /// </summary>
        public static bool IsStrictValue(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxStrictValueLength)
                return false;

            foreach (char c in value)
            {
                if (c == '_')
                    continue;
                // A surrogate is outside the BMP (or broken); the fillers are the only ignorable letters of the BMP.
                if (char.IsSurrogate(c) || c is '\u115F' or '\u1160' or '\u3164' or '\uFFA0')
                    return false;
                switch (char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.LetterNumber:
                    case UnicodeCategory.OtherNumber:
                        continue;
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The names of a <c>vars=</c> line ("u,1" gives u and 1), in order, without repeats. False when a name is not
        /// <c>u</c> or <c>1</c>-<c>9</c>.
        /// </summary>
        internal static bool TryParseVars(string spec, out string[] vars)
        {
            List<string> names = new();
            foreach (string part in spec.Split(','))
            {
                string name = part.Trim();
                if (name is not ("u" or "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9"))
                {
                    vars = [];
                    return false;
                }

                if (!names.Contains(name))
                    names.Add(name);
            }

            vars = names.ToArray();
            return true;
        }

        /// <summary>
        /// Describe a respond rule based on a simple match or a regex
        /// </summary>
        internal class RespondRule
        {
            private readonly AutoRespondPattern? regex;
            private readonly string? match;
            private readonly string? actionPublic;
            private readonly string? actionPrivate;
            private readonly string? actionOther;
            private readonly bool ownersOnly;
            private readonly TimeSpan cooldown;
            private readonly string[]? vars;
            private DateTime cooldownExpiration;
            private DateTime nextRefusalLog = DateTime.MinValue;

            /// <summary>
            /// Create a respond rule from a regex and a reponse message or command
            /// </summary>
            /// <param name="regex">Regex</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="actionOther">Internal command to run for any other messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            /// <param name="cooldown">Minimal cooldown between two matches</param>
            /// <param name="vars">With strict values: the values the action uses, each checked before it runs</param>
            public RespondRule(AutoRespondPattern regex, string? actionPublic, string? actionPrivate, string? actionOther, bool ownersOnly, TimeSpan cooldown, string[]? vars = null)
            {
                this.regex = regex;
                match = null;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                this.vars = vars;
                cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>
            /// Create a respond rule from a match string and a reponse message or command
            /// </summary>
            /// <param name="match">Match string</param>
            /// <param name="actionPublic">Internal command to run for public messages</param>
            /// <param name="actionPrivate">Internal command to run for private messages</param>
            /// <param name="ownersOnly">Only match messages from bot owners</param>
            /// <param name="cooldown">Minimal cooldown between two matches</param>
            /// <param name="vars">With strict values: the values the action uses, each checked before it runs</param>
            public RespondRule(string? match, string? actionPublic, string? actionPrivate, string? actionOther, bool ownersOnly, TimeSpan cooldown, string[]? vars = null)
            {
                regex = null;
                this.match = match;
                this.actionPublic = actionPublic;
                this.actionPrivate = actionPrivate;
                this.actionOther = actionOther;
                this.ownersOnly = ownersOnly;
                this.cooldown = cooldown;
                this.vars = vars;
                cooldownExpiration = DateTime.MinValue;
            }

            /// <summary>The rule's regex or match string, for log lines.</summary>
            public string Trigger => regex?.ToString() ?? match ?? string.Empty;

            /// <summary>
            /// Match the respond rule to the specified string and return a message or command to send if a match is detected
            /// </summary>
            /// <param name="username">Player who have sent the message</param>
            /// <param name="message">Message to match against the regex or match string</param>
            /// <param name="msgType">Type of the message public/private message, or other message</param>
            /// <param name="localVars">Dictionary to populate with match variables in case of Regex match</param>
            /// <returns>Internal command to run as a response to this user, or null if no match has been detected</returns>
            public string? Match(string username, string message, MessageType msgType, Dictionary<string, object> localVars)
            {
                return Match(username, message, msgType, localVars, out _, out _);
            }

            /// <summary>
            /// <see cref="Match(string, string, MessageType, Dictionary{string, object})"/>, and for a rule with strict
            /// values the checked values its script gets (<paramref name="scriptVars"/>); <paramref name="refused"/>
            /// when the line matched but a value failed the check (the rule then does not run, and its cooldown does
            /// not start).
            /// </summary>
            internal string? Match(string username, string message, MessageType msgType, Dictionary<string, object> localVars,
                out Dictionary<string, object>? scriptVars, out bool refused)
            {
                scriptVars = null;
                refused = false;

                if (DateTime.Now < cooldownExpiration)
                    return null;

                string? toSend = null;

                if (ownersOnly && (String.IsNullOrEmpty(username) || !Settings.Config.Main.Advanced.BotOwners.Contains(username.ToLower())))
                    return null;

                switch (msgType)
                {
                    case MessageType.Public: toSend = actionPublic; break;
                    case MessageType.Private: toSend = actionPrivate; break;
                    case MessageType.Other: toSend = actionOther; break;
                }

                if (String.IsNullOrEmpty(toSend))
                    return null;

                if (regex is not null)
                {
                    Match? regexMatch = regex.Match(message);
                    if (regexMatch is null)
                        return null;

                    localVars["match_0"] = regexMatch.Groups[0].Value;
                    for (int i = regexMatch.Groups.Count - 1; i >= 1; i--)
                    {
                        toSend = toSend.Replace("$" + i, regexMatch.Groups[i].Value);
                        localVars["match_" + i] = regexMatch.Groups[i].Value;
                    }
                    toSend = toSend.Replace("$u", username);
                    localVars["match_u"] = username;
                }
                else if (!String.IsNullOrEmpty(match))
                {
                    if (!message.ToLower().Contains(match.ToLower()))
                        return null;

                    localVars["match_0"] = message;
                    localVars["match_u"] = username;
                    toSend = toSend.Replace("$u", username);
                }
                else
                {
                    return null;
                }

                if (vars is not null)
                {
                    // Strict values: every value the action uses passes the check, or the rule does not answer.
                    Dictionary<string, object> checkedVars = new();
                    foreach (string name in vars)
                    {
                        string key = "match_" + name;
                        if (!localVars.TryGetValue(key, out object? value) || value is not string text || !IsStrictValue(text))
                        {
                            refused = true;
                            return null;
                        }

                        checkedVars[key] = text;
                    }

                    scriptVars = checkedVars;
                }

                cooldownExpiration = DateTime.Now + cooldown;
                return toSend;
            }

            /// <summary>A refusal is logged at most once a minute per rule.</summary>
            internal bool ShouldLogRefusal(DateTime now)
            {
                if (now < nextRefusalLog)
                    return false;

                nextRefusalLog = now + RefusalLogInterval;
                return true;
            }

            /// <summary>
            /// Get a string representation of the RespondRule
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return string.Format(
                    Translations.bot_autoRespond_match,
                    match,
                    regex,
                    actionPublic,
                    actionPrivate,
                    actionOther,
                    ownersOnly,
                    (int)cooldown.TotalSeconds
                );
            }
        }

        /// <summary>
        /// Initialize the AutoRespond bot from the matches file
        /// </summary>
        public override void Initialize()
        {
            if (File.Exists(Config.Matches_File))
            {
                AutoRespondPattern? matchRegex = null;
                bool unusableRegex = false;
                string? matchString = null;
                string? matchAction = null;
                string? matchActionPrivate = null;
                string? matchActionOther = null;
                bool ownersOnly = false;
                TimeSpan cooldown = TimeSpan.Zero;
                string? matchVars = null;
                respondRules = new List<RespondRule>();

                LogDebugToConsole(string.Format(Translations.bot_autoRespond_loading, System.IO.Path.GetFullPath(Config.Matches_File)));

                foreach (string lineRAW in File.ReadAllLines(Config.Matches_File, Encoding.UTF8))
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[^1] == ']')
                        {
                            switch (line[1..^1].ToLower())
                            {
                                case "match":
                                    if (!unusableRegex)
                                        CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown, matchVars);
                                    matchRegex = null;
                                    unusableRegex = false;
                                    matchString = null;
                                    matchAction = null;
                                    matchActionPrivate = null;
                                    matchActionOther = null;
                                    ownersOnly = false;
                                    cooldown = TimeSpan.Zero;
                                    matchVars = null;
                                    break;
                            }
                        }
                        else
                        {
                            string argName = line.Split('=')[0];
                            if (line.Length > (argName.Length + 1))
                            {
                                string argValue = line[(argName.Length + 1)..];
                                switch (argName.ToLower())
                                {
                                    case "regex":
                                        // A pattern .NET can not read drops this rule only, not the rest of the file.
                                        unusableRegex = !AutoRespondPattern.TryCreate(argValue, LogToConsole, LogDebugToConsole, out matchRegex);
                                        if (unusableRegex)
                                            LogToConsole(string.Format(Translations.bot_autoRespond_regex_invalid, argValue));
                                        break;
                                    case "match": matchString = argValue; break;
                                    case "action": matchAction = argValue; break;
                                    case "actionprivate": matchActionPrivate = argValue; break;
                                    case "actionother": matchActionOther = argValue; break;
                                    case "ownersonly": ownersOnly = bool.Parse(argValue); break;
                                    case "cooldown": cooldown = TimeSpan.FromSeconds(int.Parse(argValue, NumberStyles.Any, CultureInfo.CurrentCulture)); break;
                                    case "vars": matchVars = argValue; break;
                                }
                            }
                        }
                    }
                }
                if (!unusableRegex)
                    CheckAddMatch(matchRegex, matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown, matchVars);
            }
            else
            {
                LogToConsole(string.Format(Translations.bot_autoRespond_file_not_found, System.IO.Path.GetFullPath(Config.Matches_File)));
                UnloadBot(); //No need to keep the bot active
            }
        }

        /// <summary>
        /// Create a new respond rule from the provided arguments, only if they are valid: at least one match and one action
        /// </summary>
        /// <param name="matchRegex">Matching regex</param>
        /// <param name="matchString">Matching string</param>
        /// <param name="matchAction">Action if the matching message is public</param>
        /// <param name="matchActionPrivate">Action if the matching message is private</param>
        /// <param name="ownersOnly">Only match messages from bot owners</param>
        /// <param name="cooldown">Minimal cooldown between two matches</param>
        /// <param name="matchVars">The block's vars= line (used only with strict values)</param>
        private void CheckAddMatch(AutoRespondPattern? matchRegex, string? matchString, string? matchAction, string? matchActionPrivate, string? matchActionOther, bool ownersOnly, TimeSpan cooldown, string? matchVars)
        {
            if (matchRegex is not null || matchString is not null || matchAction is not null || matchActionPrivate is not null || matchActionOther is not null || ownersOnly || cooldown != TimeSpan.Zero)
            {
                if (!TryGetStrictVars(matchRegex, matchVars, out string[]? vars))
                {
                    // Fails closed: a rule whose values can not be checked as asked does not run at all.
                    LogToConsole(string.Format(Translations.bot_autoRespond_vars_unusable, matchRegex?.ToString() ?? matchString));
                    return;
                }

                RespondRule rule = matchRegex is not null
                    ? new RespondRule(matchRegex, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown, vars)
                    : new RespondRule(matchString, matchAction, matchActionPrivate, matchActionOther, ownersOnly, cooldown, vars);

                if (matchAction is not null || matchActionPrivate is not null || matchActionOther is not null)
                {
                    if (matchRegex is not null || matchString is not null)
                    {
                        respondRules!.Add(rule);
                        LogDebugToConsole(string.Format(Translations.bot_autoRespond_loaded_match, rule));
                    }
                    else LogDebugToConsole(string.Format(Translations.bot_autoRespond_no_trigger, rule));
                }
                else LogDebugToConsole(string.Format(Translations.bot_autoRespond_no_action, rule));
            }
        }

        /// <summary>
        /// The strict values of a block: null without strict values or without <c>vars=</c> (the block runs as
        /// before). False when the block asks for values that can not be checked as asked: a <c>vars=</c> line with
        /// another name, or a pattern with named groups (.NET numbers them after the others, so <c>1</c> could mean
        /// another group than the one the host counted).
        /// </summary>
        internal static bool TryGetStrictVars(AutoRespondPattern? regex, string? matchVars, out string[]? vars)
        {
            vars = null;
            if (!StrictValues || matchVars is null)
                return true;
            if (!TryParseVars(matchVars, out string[] names))
                return false;
            if (regex is not null && regex.HasNamedGroups)
                return false;

            vars = names;
            return true;
        }

        /// <summary>
        /// Process messages from the server and test them against all matches
        /// </summary>
        /// <param name="text">Text from the server</param>
        public override void GetText(string text)
        {
            //Remove colour codes
            if (!Config.Match_Colors)
                text = GetVerbatim(text);

            //Get Message type
            string sender = "", message = "";
            MessageType msgType = MessageType.Other;
            if (IsChatMessage(text, ref message, ref sender))
                msgType = MessageType.Public;
            else if (IsPrivateMessage(text, ref message, ref sender))
                msgType = MessageType.Private;
            else message = text;

            //Do not process messages sent by the bot itself
            if (msgType == MessageType.Other || sender != InternalConfig.Username)
            {
                foreach (RespondRule rule in respondRules!)
                {
                    Dictionary<string, object> localVars = new();
                    string? toPerform = rule.Match(sender, message, msgType, localVars, out Dictionary<string, object>? scriptVars, out bool refused);
                    if (refused)
                    {
                        if (rule.ShouldLogRefusal(DateTime.Now))
                            LogToConsole(string.Format(Translations.bot_autoRespond_vars_refused, rule.Trigger));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(toPerform))
                    {
                        // The checked values reach the script the action starts, and nothing else does.
                        CmdResult response = new() { ScriptVariables = scriptVars };
                        LogToConsole(string.Format(Translations.bot_autoRespond_match_run, toPerform));
                        PerformInternalCommand(toPerform, ref response, localVars);
                        if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                            LogToConsole(response);

                        // Notify the hosting launcher (no-op outside bridge mode) so it can e.g. play a sound.
                        LauncherBridge.Emit("autorespond", new() { ["u"] = sender });
                    }
                }
            }
        }
    }
}
