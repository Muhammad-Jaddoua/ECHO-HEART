using System;
using System.Collections.Generic;
using Echoheart.Core;
using Echoheart.World;

internal static class Program
{
    private static int _assertions;
    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        TitleRejectsGameplay();
        DeathAndRetryRoutes();
        BossAndHeartAreOneShot();
        PausePreservesProgress();
        NewRunResetsProgress();
        ExploreEveryReachableState();
        Console.WriteLine($"PASS: {_assertions} assertions across 6 flow scenarios; actual SessionPolicyV13 + CoreTypes compiled.");
    }

    private static void TitleRejectsGameplay()
    {
        var p = new SessionPolicyV13();
        Check(p.State == GameState.Title, "Scene must start at title.");
        Check(!p.Pause() && !p.Resume() && !p.Die() && !p.Retry() && !p.DefeatBoss() && !p.Complete(), "Title accepted gameplay input.");
        Check(p.StartNewGame(), "New game did not begin.");
        Check(!p.StartNewGame(), "A double click restarted a live run.");
        Check(p.State == GameState.Playing && !p.HeartUnlocked, "Fresh run has stale state.");
    }

    private static void DeathAndRetryRoutes()
    {
        var p = new SessionPolicyV13(); p.StartNewGame();
        Check(p.Die() && !p.Die(), "Player death must be one-shot.");
        Check(p.Retry() && p.RetryScope == RetryScope.MainArea, "Main death used the wrong checkpoint.");
        Check(SessionPolicyV13.ClearsAbility(p.RetryScope), "Main retry must clear power.");
        Check(!p.Retry(), "Live gameplay accepted a second retry.");
        Check(p.EnterArena() && !p.EnterArena(), "Arena entry must be idempotent.");
        Check(p.Die() && p.Retry(), "Boss death/retry failed.");
        Check(p.RetryScope == RetryScope.BossArena && !SessionPolicyV13.ClearsAbility(p.RetryScope), "Boss retry lost the selected power.");
        Check(!p.HeartUnlocked, "Boss retry left the Heart unlocked.");
    }

    private static void BossAndHeartAreOneShot()
    {
        var p = new SessionPolicyV13(); p.StartNewGame();
        Check(!p.Complete() && !p.DefeatBoss(), "Main-area events unlocked completion.");
        p.EnterArena();
        Check(!p.Complete(), "Heart completed before boss defeat.");
        Check(p.DefeatBoss() && !p.DefeatBoss(), "Boss defeat was not one-shot.");
        Check(p.Complete() && !p.Complete(), "Completion was not one-shot.");
        Check(p.State == GameState.Complete && !p.Die() && !p.Retry() && !p.Pause(), "Completed run accepted gameplay commands.");
    }

    private static void PausePreservesProgress()
    {
        var p = new SessionPolicyV13(); p.StartNewGame(); p.EnterArena(); p.DefeatBoss();
        Check(p.Pause() && !p.Pause(), "Pause must be guarded.");
        Check(!p.Complete() && !p.Die() && !p.DefeatBoss(), "Paused simulation mutated progression.");
        Check(p.Resume() && p.HeartUnlocked, "Resume lost boss progress.");
        Check(p.Pause() && p.Retry() && !p.HeartUnlocked, "An explicit boss retry must restart the fight.");
    }

    private static void NewRunResetsProgress()
    {
        var p = new SessionPolicyV13(); p.StartNewGame(); p.EnterArena(); p.DefeatBoss(); p.Complete();
        Check(p.StartNewGame(), "Play again did not start a fresh run.");
        Check(!p.HeartUnlocked && p.RetryScope == RetryScope.NewGame, "Play again retained old completion/checkpoint state.");
        Check(SessionPolicyV13.ClearsAbility(RetryScope.NewGame), "New game must clear power.");
        Check(p.ReturnToTitle() && !p.ReturnToTitle(), "Return to title must be idempotent.");
        Check(p.StartNewGame(), "Title could not restart the session.");
    }

    private static void ExploreEveryReachableState()
    {
        var commands = new Func<SessionPolicyV13, bool>[]
        {
            p => p.StartNewGame(), p => p.Pause(), p => p.Resume(), p => p.EnterArena(), p => p.Die(),
            p => p.Retry(), p => p.DefeatBoss(), p => p.Complete(), p => p.ReturnToTitle()
        };
        var queue = new Queue<int[]>(); queue.Enqueue(Array.Empty<int>());
        var seen = new HashSet<string>();
        while (queue.Count > 0)
        {
            int[] history = queue.Dequeue();
            var current = Replay(history, commands);
            string key = $"{current.State}/{current.RetryScope}/{current.HeartUnlocked}";
            if (!seen.Add(key)) continue;
            for (int i = 0; i < commands.Length; i++)
            {
                var p = Replay(history, commands); var state = p.State; var scope = p.RetryScope; bool unlocked = p.HeartUnlocked;
                bool accepted = commands[i](p);
                Check(!p.HeartUnlocked || p.RetryScope == RetryScope.BossArena, "Unlocked Heart exists outside the boss checkpoint.");
                Check(p.State != GameState.Complete || p.HeartUnlocked, "Completion exists without restoration eligibility.");
                if (!accepted) Check(p.State == state && p.RetryScope == scope && p.HeartUnlocked == unlocked, "Rejected command changed session data.");
                int[] next = new int[history.Length + 1]; Array.Copy(history, next, history.Length); next[next.Length - 1] = i; queue.Enqueue(next);
            }
        }
        Check(seen.Count >= 10, "State exploration did not cover the complete flow graph.");
    }

    private static SessionPolicyV13 Replay(int[] history, Func<SessionPolicyV13, bool>[] commands)
    {
        var p = new SessionPolicyV13(); foreach (int command in history) commands[command](p); return p;
    }
}
