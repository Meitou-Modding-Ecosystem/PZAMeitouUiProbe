// PZA's UI Probe for Meitou. Author: PZA. Licence: GPLv3 or later.
// The log (ModContext.Log) and rule counters: every rule counts when it fires; a line every 5 s lists what changed.
using System.Diagnostics;
using System.Text;

namespace PZA.UiProbe.Meitou;

public static class Counters
{
    static readonly Dictionary<string, long> s_counts = new(StringComparer.Ordinal);
    static readonly Dictionary<string, long> s_reported = new(StringComparer.Ordinal);

    public static void Add(string rule) => s_counts[rule] = s_counts.GetValueOrDefault(rule) + 1;
    public static long Get(string rule) => s_counts.GetValueOrDefault(rule);

    public static string? Changes()
    {
        var sb = new StringBuilder();
        foreach (var (rule, n) in s_counts.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            long before = s_reported.GetValueOrDefault(rule);
            if (n == before) continue;
            sb.Append(sb.Length == 0 ? "" : " ").Append(rule).Append('=').Append(n).Append("(+").Append(n - before).Append(')');
            s_reported[rule] = n;
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}

public static class Log
{
    public static Action<string> Sink = _ => { };
    static long s_lastReport = Stopwatch.GetTimestamp();
    static long s_lastException;

    public static void Line(string text) => Sink("PZA's UI Probe: " + text);

    public static void Tick()
    {
        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(s_lastReport, now).TotalSeconds < 5) return;
        s_lastReport = now;
        if (Counters.Changes() is { } changes) Sink("PZA's UI Probe: [5s] " + changes);
    }

    public static void Exception(string where, Exception e)
    {
        Counters.Add("error." + where.Replace(' ', '_'));
        long now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(s_lastException, now).TotalSeconds < 1) return;
        s_lastException = now;
        Line("error in " + where + ": " + e.GetType().Name + ": " + e.Message);
    }
}
