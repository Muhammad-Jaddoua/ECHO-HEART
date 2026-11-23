using System;
using Echoheart.World;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static MovementCueV121 Sample(MovementCuePolicyV121 p, bool grounded, float speed, float dt = .02f,
        bool playing = true, bool discontinuity = false) => p.Step(grounded, speed, dt, playing, discontinuity);
    private static MovementCuePolicyV121 Grounded()
    {
        var p = new MovementCuePolicyV121();
        Check(Sample(p, true, -2) == MovementCueV121.None, "First grounded sample makes a cue.");
        return p;
    }
    private static void Main()
    {
        var p = new MovementCuePolicyV121();
        Check(Sample(p, false, 8) == MovementCueV121.None, "Airborne spawn plays jump.");
        Check(Sample(p, false, -20, 1) == MovementCueV121.None, "Airborne spawn plays fall cue.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Spawn settling plays landing.");
        Check(Sample(p, false, 8) == MovementCueV121.Jump, "Upward departure loses jump.");
        Check(Sample(p, false, 3, .1f) == MovementCueV121.None, "Rising repeats jump.");
        Check(Sample(p, false, 0) == MovementCueV121.None, "Apex makes a cue.");
        Check(Sample(p, false, -6) == MovementCueV121.None, "Descent makes a cue.");
        Check(Sample(p, true, -2) == MovementCueV121.Land, "Meaningful jump loses landing.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Grounded frame repeats landing.");

        p = Grounded();
        Check(Sample(p, false, -1, .06f) == MovementCueV121.None, "Walking off plays jump.");
        Check(Sample(p, false, -5, .06f) == MovementCueV121.None, "Falling plays jump.");
        Check(Sample(p, true, -2) == MovementCueV121.Land, "Meaningful ledge fall loses landing.");
        p = Grounded();
        Check(Sample(p, false, -10) == MovementCueV121.None, "Brief ground loss plays jump.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Ground jitter makes landing.");
        p = Grounded();
        Sample(p, false, -2, .2f);
        Check(Sample(p, true, -2) == MovementCueV121.None, "Slow contact loss makes landing.");
        p = Grounded();
        Sample(p, false, -3, .1f);
        Check(Sample(p, true, -2) == MovementCueV121.Land, "Fall/time boundary loses landing.");
        p = Grounded();
        Check(Sample(p, false, .5f, .2f) == MovementCueV121.None, "Rise boundary counts as jump.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Low rise invents landing.");
        p = Grounded();
        Check(Sample(p, false, .5001f, .13f) == MovementCueV121.Jump, "Rise above boundary loses jump.");
        Check(Sample(p, true, -2) == MovementCueV121.Land, "Jump needs an unnecessary high fall speed.");
        p = Grounded();
        Check(Sample(p, false, 8) == MovementCueV121.Jump, "Short hop loses its departure cue.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Very short hop makes landing.");

        p = Grounded();
        Check(Sample(p, false, 8, 1, false) == MovementCueV121.None, "Paused departure makes a cue.");
        Check(Sample(p, false, 8) == MovementCueV121.Jump, "Pause consumed departure.");
        Check(Sample(p, true, -2, 20, false) == MovementCueV121.None, "Pause makes landing.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Pause advanced airtime/contact state.");
        p = Grounded(); Sample(p, false, 8, .13f);
        Check(Sample(p, false, 8, 20, false) == MovementCueV121.None, "Paused air repeats jump.");
        Check(Sample(p, false, 4) == MovementCueV121.None, "Resume repeats jump.");
        Check(Sample(p, true, -2) == MovementCueV121.Land, "Pause lost established airborne phase.");
        p = Grounded(); Sample(p, false, 8, .13f); p.Reset();
        Check(Sample(p, false, -20, 1) == MovementCueV121.None, "Reset retains airborne phase.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Retry makes settling landing.");
        Check(Sample(p, false, 8) == MovementCueV121.Jump, "Reprimed retry loses next jump.");
        Check(Sample(p, false, 8, .2f, true, true) == MovementCueV121.None, "Teleport makes a cue.");
        Check(Sample(p, true, -2) == MovementCueV121.None, "Teleport keeps old airborne phase.");
        Check(Sample(p, false, 8) == MovementCueV121.Jump, "Teleport never reprimes.");

        foreach (float dt in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            p = Grounded();
            Check(Sample(p, false, 8, dt) == MovementCueV121.None, "Invalid time makes a cue.");
            Check(Sample(p, false, 8) == MovementCueV121.Jump, "Invalid time consumed departure.");
        }
        foreach (float speed in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            p = Grounded(); Sample(p, false, 8, .13f);
            Check(Sample(p, false, speed) == MovementCueV121.None, "Invalid speed makes a cue.");
            Check(Sample(p, true, -2) == MovementCueV121.None, "Invalid speed retained stale landing.");
            Check(Sample(p, false, 8) == MovementCueV121.Jump, "Invalid speed never reprimes.");
        }
        // Different sampling rates must produce one jump/landing per physical arc, no jump on a fall.
        foreach (int rate in new[] { 30, 60, 144 })
        {
            p = Grounded(); int jumps = 0, lands = 0; float dt = 1f / rate;
            for (int frame = 0; frame < rate; frame++)
            {
                MovementCueV121 cue = Sample(p, false, 8f - 25f * frame * dt, dt);
                if (cue == MovementCueV121.Jump) jumps++;
                if (cue == MovementCueV121.Land) lands++;
            }
            Check(jumps == 1 && lands == 0, "Arc produces duplicate or premature cues.");
            Check(Sample(p, true, -2, dt) == MovementCueV121.Land, "Arc loses landing at sample rate.");
            bool quiet = true;
            for (int frame = 0; frame < rate; frame++) quiet &= Sample(p, true, -2, dt) == MovementCueV121.None;
            Check(quiet, "Sustained grounding repeats cues.");
            for (int frame = 0; frame < rate; frame++) quiet &= Sample(p, false, -1f - frame * dt * 25f, dt) == MovementCueV121.None;
            Check(quiet, "Continuous fall plays jump or lands in midair.");
            Check(Sample(p, true, -2, dt) == MovementCueV121.Land, "Long fall loses landing at sample rate.");
        }
        p = Grounded(); Sample(p, false, -4, float.MaxValue);
        Check(Sample(p, true, -2, float.MaxValue) == MovementCueV121.Land, "Long finite interval overflows airborne duration.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.21 movement cue policy (C# 9.0, no Unity stubs).");
    }
}
