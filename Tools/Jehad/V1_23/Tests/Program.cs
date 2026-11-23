using System;
using Echoheart.World;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static float Squared(float x, float y, float z) => x * x + y * y + z * z;
    private static void Main()
    {
        foreach (float nearby in new[] { 0f, .001f, 1f, 4f, 16f, 63.99f, 64f })
        {
            Check(!HitFeedbackPointV123.UseActor(true, nearby), "Nearby player impact is moved.");
            Check(!HitFeedbackPointV123.UseActor(false, nearby), "Nearby non-player impact is moved.");
        }
        foreach (float distant in new[] { 64.001f, 65f, 900f, float.MaxValue })
        {
            Check(HitFeedbackPointV123.UseActor(true, distant), "Distant player impact is left behind.");
            Check(!HitFeedbackPointV123.UseActor(false, distant), "Distant enemy/boss impact is moved.");
        }
        foreach (float invalid in new[] { -1f, float.MinValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check(HitFeedbackPointV123.UseActor(true, invalid), "Invalid player distance is not anchored.");
            Check(!HitFeedbackPointV123.UseActor(false, invalid), "Player-only rule changes non-player feedback.");
        }
        Check(!HitFeedbackPointV123.UseActor(true, Squared(.25f, 1f, 0)), "Ordinary body hit loses its contact point.");
        Check(HitFeedbackPointV123.UseActor(true, Squared(0, -20f - 3f, 0)), "Below-world point survives recovery to y=3.");
        Check(HitFeedbackPointV123.UseActor(true, Squared(0, 80f, 0)), "Vertical displacement is ignored.");
        Check(HitFeedbackPointV123.UseActor(true, Squared(9f, 0, 0)), "Horizontal displacement is ignored.");
        Check(!HitFeedbackPointV123.UseActor(true, Squared(2f, 2f, 2f)), "A nearby diagonal hit is moved.");
        Check(!HitFeedbackPointV123.UseActor(true, Squared(0, 1f, 0)), "Legacy no-point body fallback is moved again.");
        Check(HitFeedbackPointV123.UseActor(true, Squared(5f, 5f, 5f)), "Combined 3D displacement is ignored.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.23 player hit-point policy (C# 9.0, no Unity stubs).");
    }
}
