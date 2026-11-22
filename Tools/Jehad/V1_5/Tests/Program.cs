using System;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.World;

internal static class Program
{
    private static int _assertions;
    private static void Check(bool valid, string reason) { _assertions++; if (!valid) throw new Exception(reason); }
    private static void Main()
    {
        Check(EnvironmentMusicV15.Select(0, 5, -100, false) == MusicRegionV15.Meadow, "Wrong entry music.");
        Check(EnvironmentMusicV15.Select(-70, 4, -40, false) == MusicRegionV15.Amber, "Wrong woodland music.");
        Check(EnvironmentMusicV15.Select(70, 3, -3, false) == MusicRegionV15.Lake, "Wrong lake music.");
        Check(EnvironmentMusicV15.Select(20, 7, 70, false) == MusicRegionV15.Pine, "Wrong pine music.");
        Check(EnvironmentMusicV15.Select(-56, 53, 58, false) == MusicRegionV15.Alpine, "Wrong summit music.");
        Check(EnvironmentMusicV15.Select(-56, 53, 58, true) == MusicRegionV15.Guardian, "Arena must take priority.");
        var blend = new EnvironmentMusicV15();
        blend.Request(MusicRegionV15.Lake, .3f); blend.Request(MusicRegionV15.Meadow, .3f);
        Check(blend.Region == MusicRegionV15.Meadow, "Boundary jitter switched music.");
        blend.Request(MusicRegionV15.Lake, .7f); Check(blend.Region == MusicRegionV15.Lake, "Stable lake entry never switched.");
        Check(blend.Weight(0) == 1f && blend.Weight(2) == 0f, "A region request snapped audible weights.");
        blend.Advance(.1f); Check(blend.Weight(2) > 0 && blend.Weight(2) < 1, "No continuous crossfade.");
        float first = blend.Weight(0), lake = blend.Weight(2);
        blend.Request(MusicRegionV15.Guardian, 0, true); blend.Advance(0);
        Check(blend.Region == MusicRegionV15.Guardian && blend.Weight(0) == first && blend.Weight(2) == lake, "Urgent boss transition snapped the mix.");
        for (int frame = 0; frame < 900; frame++)
        {
            if (frame % 17 == 0) blend.Request((MusicRegionV15)((frame / 17) % 6), 0, true);
            blend.Advance(1f / 60f); float sum = 0;
            for (int i = 0; i < 6; i++) { float w = blend.Weight(i); Check(!float.IsNaN(w) && w >= 0 && w <= 1, "Rapid reversal made invalid weights."); sum += w; }
            Check(Math.Abs(sum - 1f) < .00002f, "Crossfade lost normalized energy weights.");
        }
        first = blend.Weight(0); blend.Advance(float.NaN); blend.Advance(float.PositiveInfinity); blend.Advance(-1);
        Check(blend.Weight(0) == first, "Invalid elapsed time poisoned music weights.");
        blend.Request(MusicRegionV15.Meadow, 0, true); blend.Advance(100);
        Check(blend.Weight(0) > .999f, "Mix did not settle after a long frame.");
        foreach (GameState state in Enum.GetValues(typeof(GameState)))
        {
            var chest = new ChestStateV15(); Check(!chest.TryOpen(state, false), "A distant chest opened.");
            Check(chest.TryOpen(state, true) == (state == GameState.Playing), "Chest ignored game-state guard.");
        }
        foreach (RetryScope scope in Enum.GetValues(typeof(RetryScope)))
        {
            var chest = new ChestStateV15(); Check(chest.TryOpen(GameState.Playing, true), "Fresh chest would not open.");
            Check(!chest.TryOpen(GameState.Playing, true), "Chest duplicated its reveal.");
            chest.ResetFor(scope); Check(chest.Opened == (scope != RetryScope.NewGame), "Retry changed chest persistence.");
            chest.ResetFor(RetryScope.NewGame); Check(!chest.Opened && chest.TryOpen(GameState.Playing, true), "New Game did not re-arm chest.");
        }
        Console.WriteLine($"PASS: {_assertions} assertions; actual V1.5 region/mix and chest policy sources compiled against actual CoreTypes (C# 9.0).");
    }
}
