using System;
using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// Global event hub. Gameplay systems raise, UI/audio/flow listen, so the HUD never needs a reference
    /// to the player or boss. Always subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        /// <summary>(current, max)</summary>
        public static event Action<int, int> PlayerHealthChanged;
        /// <summary>New held power; null means none.</summary>
        public static event Action<AbilityDefinition> AbilityChanged;
        /// <summary>(collected, total)</summary>
        public static event Action<int, int> CollectiblesChanged;
        public static event Action BossEncounterStarted;
        /// <summary>(current, max)</summary>
        public static event Action<int, int> BossHealthChanged;
        public static event Action BossDefeated;
        public static event Action PlayerDied;
        public static event Action<RetryScope> RetryRequested;
        public static event Action GameCompleted;
        /// <summary>Context prompt text ("Hold E to inhale"); empty string hides it.</summary>
        public static event Action<string> PromptChanged;

        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaiseAbilityChanged(AbilityDefinition ability) => AbilityChanged?.Invoke(ability);
        public static void RaiseCollectiblesChanged(int collected, int total) => CollectiblesChanged?.Invoke(collected, total);
        public static void RaiseBossEncounterStarted() => BossEncounterStarted?.Invoke();
        public static void RaiseBossHealthChanged(int current, int max) => BossHealthChanged?.Invoke(current, max);
        public static void RaiseBossDefeated() => BossDefeated?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseRetryRequested(RetryScope scope) => RetryRequested?.Invoke(scope);
        public static void RaiseGameCompleted() => GameCompleted?.Invoke();
        public static void RaisePromptChanged(string text) => PromptChanged?.Invoke(text);

        // Static events survive play sessions when domain reload is disabled; clear them on every play start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StateChanged = null;
            PlayerHealthChanged = null;
            AbilityChanged = null;
            CollectiblesChanged = null;
            BossEncounterStarted = null;
            BossHealthChanged = null;
            BossDefeated = null;
            PlayerDied = null;
            RetryRequested = null;
            GameCompleted = null;
            PromptChanged = null;
        }
    }
}
