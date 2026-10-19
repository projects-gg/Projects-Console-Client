using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// The regex of an AutoRespond rule. Every chat line runs through every rule, so no line may stall
    /// the client. A rule matches with .NET's backtracking engine, exactly as before, but within
    /// <see cref="MatchTimeout"/>. A line that runs it out of time moves the rule to the non-backtracking
    /// engine for good, whose time grows linearly with the line, and that line is matched there.
    /// Patterns that engine can not run (backreferences, lookarounds, atomic groups, conditionals,
    /// balancing groups, <c>\G</c>, an automaton over its size limit) stay where they are: a line that
    /// runs out of time counts as no match, and after <see cref="TimeoutLimit"/> such lines the rule is
    /// turned off.
    /// </summary>
    /// <remarks>
    /// The non-backtracking engine is the rescue, not the first choice: on .NET 10 it returns a
    /// different match than the backtracking engine for some ordinary patterns (dotnet/runtime#135585,
    /// #135586, #135332), which would change the <c>$1</c> values of replies. It runs without a match
    /// timeout, since a finite one makes it miss matches (dotnet/runtime#134653).
    /// </remarks>
    internal sealed class AutoRespondPattern
    {
        /// <summary>
        /// Longest the backtracking engine may take on one chat line. Ordinary patterns need
        /// microseconds; only one that backtracks out of control gets here.
        /// </summary>
        internal static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Timeouts after which a rule the non-backtracking engine can not run is turned off, until the
        /// bot loads again.
        /// </summary>
        internal const int TimeoutLimit = 3;

        private readonly Regex backtracking;
        private readonly Regex? nonBacktracking;
        private readonly Action<string> log;
        private readonly Action<string> logDebug;
        private bool movedToNonBacktracking;
        private int timeouts;

        private AutoRespondPattern(Regex backtracking, Regex? nonBacktracking, Action<string> log, Action<string> logDebug)
        {
            this.backtracking = backtracking;
            this.nonBacktracking = nonBacktracking;
            this.log = log;
            this.logDebug = logDebug;
        }

        /// <summary>The non-backtracking engine can run this pattern.</summary>
        internal bool SupportsNonBacktracking => nonBacktracking is not null;

        /// <summary>A line ran the backtracking engine out of time and the rule now matches without it.</summary>
        internal bool IsNonBacktracking => movedToNonBacktracking;

        /// <summary>The rule ran out of time <see cref="TimeoutLimit"/> times and no longer matches.</summary>
        internal bool IsTurnedOff => timeouts >= TimeoutLimit;

        /// <summary>
        /// Builds a rule's regex with .NET's default options (case-sensitive unless the pattern says
        /// <c>(?i)</c>), as AutoRespond always has. Fails only when <paramref name="pattern"/> is not a
        /// valid .NET regex.
        /// </summary>
        /// <param name="pattern">The text after <c>regex=</c></param>
        /// <param name="log">Writes a console line: on the first timeout of a rule that stays on the backtracking engine, and when it is turned off</param>
        /// <param name="logDebug">Writes a debug line when the rule moves to the non-backtracking engine</param>
        /// <param name="result">The rule's regex</param>
        internal static bool TryCreate(string pattern, Action<string> log, Action<string> logDebug, [NotNullWhen(true)] out AutoRespondPattern? result)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            ArgumentNullException.ThrowIfNull(log);
            ArgumentNullException.ThrowIfNull(logDebug);
            result = null;
            Regex backtracking;
            try
            {
                backtracking = new Regex(pattern, RegexOptions.None, MatchTimeout);
            }
            catch (ArgumentException)
            {
                return false;
            }
            Regex? nonBacktracking;
            try
            {
                // Built now, so that both engines read (?i) with the same culture.
                nonBacktracking = new Regex(pattern, RegexOptions.NonBacktracking);
            }
            catch (NotSupportedException)
            {
                nonBacktracking = null;
            }
            result = new AutoRespondPattern(backtracking, nonBacktracking, log, logDebug);
            return true;
        }

        /// <summary>
        /// The first match in <paramref name="input"/>, or null when there is none. A line that runs a
        /// rule without the non-backtracking engine out of time counts as no match.
        /// </summary>
        internal Match? Match(string input)
        {
            if (movedToNonBacktracking)
                return Found(nonBacktracking!.Match(input));
            if (IsTurnedOff)
                return null;
            try
            {
                return Found(backtracking.Match(input));
            }
            catch (RegexMatchTimeoutException)
            {
                if (nonBacktracking is not null)
                {
                    movedToNonBacktracking = true;
                    logDebug(string.Format(Translations.bot_autoRespond_regex_non_backtracking, backtracking));
                    return Found(nonBacktracking.Match(input));
                }
                timeouts++;
                if (timeouts == 1)
                    log(string.Format(Translations.bot_autoRespond_regex_timeout, backtracking));
                if (timeouts == TimeoutLimit)
                    log(string.Format(Translations.bot_autoRespond_regex_turned_off, backtracking));
                return null;
            }
        }

        /// <summary>The pattern, like <see cref="Regex.ToString"/>.</summary>
        public override string ToString() => backtracking.ToString();

        private static Match? Found(Match match) => match.Success ? match : null;
    }
}
