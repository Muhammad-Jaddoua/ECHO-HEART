using Echoheart.Core;

namespace Echoheart.World
{
    /// <summary>Engine-independent rules; scene side effects stay in SessionFlowV13.</summary>
    public sealed class SessionPolicyV13
    {
        public GameState State { get; private set; } = GameState.Title;
        public RetryScope RetryScope { get; private set; } = RetryScope.MainArea;
        public bool HeartUnlocked { get; private set; }

        public bool StartNewGame()
        {
            if (State != GameState.Title && State != GameState.Complete) return false;
            RetryScope = RetryScope.NewGame;
            HeartUnlocked = false;
            State = GameState.Playing;
            return true;
        }

        public bool Pause()
        {
            if (State != GameState.Playing) return false;
            State = GameState.Paused;
            return true;
        }

        public bool Resume()
        {
            if (State != GameState.Paused) return false;
            State = GameState.Playing;
            return true;
        }

        public bool EnterArena()
        {
            if (State != GameState.Playing || RetryScope == RetryScope.BossArena) return false;
            RetryScope = RetryScope.BossArena;
            return true;
        }

        public bool Die()
        {
            if (State != GameState.Playing) return false;
            State = GameState.Dead;
            return true;
        }

        public bool Retry()
        {
            if (State != GameState.Dead && State != GameState.Paused) return false;
            if (RetryScope != RetryScope.BossArena) RetryScope = RetryScope.MainArea;
            HeartUnlocked = false;
            State = GameState.Playing;
            return true;
        }

        public bool DefeatBoss()
        {
            if (State != GameState.Playing || RetryScope != RetryScope.BossArena || HeartUnlocked) return false;
            HeartUnlocked = true;
            return true;
        }

        public bool Complete()
        {
            if (State != GameState.Playing || !HeartUnlocked) return false;
            State = GameState.Complete;
            return true;
        }

        public bool ReturnToTitle()
        {
            if (State == GameState.Title) return false;
            State = GameState.Title;
            return true;
        }

        public static bool ClearsAbility(RetryScope scope) => scope != RetryScope.BossArena;
    }
}
