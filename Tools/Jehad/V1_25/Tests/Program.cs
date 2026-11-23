using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Echoheart.UI;

internal static class Program
{
    private const string Suffix = " seconds.\nChoose Revert if the image is difficult to see.";
    private static int _checks;
    private static string _sink;
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    // Exact formatting expressions used by the UI before V1.25, without any Unity API.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string OldDisplay(int value) => "Reverting in " + value + Suffix;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string OldPending(int value) => value > 0 ? value + " more" : "";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Func<string> format)
    {
        for (int i = 0; i < 1000; i++) _sink = format();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) _sink = format();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(_sink);
        return bytes;
    }
    private static void Main()
    {
        var display = new CountTextV125("Reverting in ", Suffix, false);
        var pending = new CountTextV125("", " more", true);
        for (int value = 0; value <= 15; value++)
        {
            string actual = display.Format(value);
            Check(actual == OldDisplay(value), "Countdown wording changed.");
            Check(ReferenceEquals(actual, display.Format(value)), "Stable countdown number rebuilt its string.");
        }
        for (int value = 0; value <= 31; value++)
        {
            string actual = pending.Format(value);
            Check(actual == OldPending(value), "Pending notice wording changed.");
            Check(ReferenceEquals(actual, pending.Format(value)), "Stable pending count rebuilt its string.");
        }
        foreach (int value in new[] { int.MinValue, -10, -1 })
            Check(pending.Format(value) == OldPending(value), "Nonpositive pending count became visible.");
        foreach (int value in new[] { int.MinValue, int.MaxValue })
            Check(display.Format(value) == OldDisplay(value), "Integer edge formatting changed.");
        for (int value = 15; value >= 0; value--)
            Check(display.Format(value) == OldDisplay(value), "Decreasing countdown retained old text.");
        Check(pending.Format(0) == "", "Cleared queue retained its pending label.");
        Check(new CountTextV125(null, null, false).Format(4) == "4", "Empty affixes failed.");
        int runtimeValue = (Environment.TickCount & 15) + 1;
        long cachedDisplay = Measure(() => display.Format(runtimeValue));
        long oldDisplay = Measure(() => OldDisplay(runtimeValue));
        long cachedPending = Measure(() => pending.Format(runtimeValue));
        long oldPending = Measure(() => OldPending(runtimeValue));
        Check(cachedDisplay == 0, "Stable countdown formatting allocated after warmup.");
        Check(oldDisplay > cachedDisplay, "Countdown allocation baseline did not exercise the original formatter.");
        Check(cachedPending == 0, "Stable pending-label formatting allocated after warmup.");
        Check(oldPending > cachedPending, "Pending-label allocation baseline did not exercise the original formatter.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.25 count-text cache (C# 9.0, no Unity stubs).");
        Console.WriteLine(JsonSerializer.Serialize(new { iterations = 10000, cached_display_bytes = cachedDisplay,
            previous_display_expression_bytes = oldDisplay, cached_pending_bytes = cachedPending,
            previous_pending_expression_bytes = oldPending,
            scope = "Managed allocation microbenchmark of formatting only under .NET 8; not Unity UI/GC/frame-time profiling." }));
    }
}
