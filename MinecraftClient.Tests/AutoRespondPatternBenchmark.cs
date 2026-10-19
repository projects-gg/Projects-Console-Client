using System.Diagnostics;
using System.Text.RegularExpressions;
using MinecraftClient.ChatBots;
using Xunit.Abstractions;

namespace MinecraftClient.Tests;

/// <summary>
/// Time per chat line of AutoRespond rules, 4.0.0 (<c>new Regex(pattern)</c>) against
/// <see cref="AutoRespondPattern"/>, on 256-character lines. Slow by design, since the old engine is
/// measured up to <see cref="BeforeCap"/> per pattern, so it runs only when asked:
/// <c>MCC_AUTORESPOND_BENCHMARK=1 dotnet test MinecraftClient.Tests --filter AutoRespondPatternBenchmark --logger "console;verbosity=detailed"</c>
/// </summary>
public sealed class AutoRespondPatternBenchmark(ITestOutputHelper output)
{
    private static readonly TimeSpan BeforeCap = TimeSpan.FromSeconds(3);

    [BenchmarkFact]
    public void SlowShapes()
    {
        output.WriteLine($"Before: 4.0.0, capped at {BeforeCap.TotalSeconds:0} s. After: first line, then the median of the next 200.");
        output.WriteLine("| Pattern | 4.0.0 | After, first line | After, next lines | Engine after |");
        output.WriteLine("| --- | ---: | ---: | ---: | --- |");
        foreach ((string pattern, string line) in AutoRespondPatternTests.SlowShapes)
            Row(pattern, line);
    }

    [BenchmarkFact]
    public void OrdinaryPatterns()
    {
        string chat = AutoRespondPatternTests.Line("[Satış] Ali: kılıç satıyorum ucuz, ", "!");
        output.WriteLine("| Pattern | 4.0.0 | After, first line | After, next lines | Engine after |");
        output.WriteLine("| --- | ---: | ---: | ---: | --- |");
        foreach (string pattern in AutoRespondPatternTests.OrdinaryPatterns)
            Row(pattern, chat);
    }

    private void Row(string pattern, string line)
    {
        var before = new Regex(pattern, RegexOptions.None, BeforeCap); // 4.0.0's engine; the timeout only caps the benchmark.
        string beforeTime;
        try
        {
            double first = Time(() => before.Match(line));
            beforeTime = first > 50 ? Ms(first) : Ms(Median(20, () => before.Match(line)));
        }
        catch (RegexMatchTimeoutException)
        {
            beforeTime = $"> {BeforeCap.TotalSeconds:0} s";
        }

        Assert.True(AutoRespondPattern.TryCreate(pattern, _ => { }, _ => { }, out AutoRespondPattern? after));
        double firstLine = Time(() => after.Match(line));
        double nextLines = Median(200, () => after.Match(line));
        string engine = after.IsNonBacktracking ? "non-backtracking" : after.IsTurnedOff ? "turned off" : "backtracking";
        output.WriteLine($"| `{pattern}` | {beforeTime} | {Ms(firstLine)} | {Ms(nextLines)} | {engine} |");
    }

    private static double Time(Action action)
    {
        long start = Stopwatch.GetTimestamp();
        action();
        return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    private static double Median(int runs, Action action)
    {
        double[] times = new double[runs];
        for (int i = 0; i < runs; i++)
            times[i] = Time(action);
        Array.Sort(times);
        return times[runs / 2];
    }

    private static string Ms(double ms) => ms switch
    {
        >= 1000 => $"{ms / 1000:0.00} s",
        >= 1 => $"{ms:0.0} ms",
        _ => $"{ms * 1000:0} µs",
    };
}

/// <summary>A fact that runs only when MCC_AUTORESPOND_BENCHMARK=1.</summary>
public sealed class BenchmarkFactAttribute : FactAttribute
{
    public BenchmarkFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MCC_AUTORESPOND_BENCHMARK") != "1")
            Skip = "Benchmark: set MCC_AUTORESPOND_BENCHMARK=1 to run it.";
    }
}
