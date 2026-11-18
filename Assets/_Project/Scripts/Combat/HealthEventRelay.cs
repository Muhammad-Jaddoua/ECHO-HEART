using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Combat
{
    /// <summary>
    /// Bridges a Health component to the global GameEvents bus so HUD, flow, and the Heart
    /// never need a reference to the player or boss. Put one on the player and one on the boss.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthEventRelay : MonoBehaviour
    {
        public enum Mode { Player, Boss }

        [SerializeField] private Mode _mode = Mode.Player;

        private Health _health;
        // Defeat/death is a one-shot flow trigger (Heart unlock, retry screen); never let it repeat.
        private bool _deathRaised;

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<Health>();
            // Unsubscribe first so a repeated enable can never double-subscribe.
            _health.Changed -= HandleChanged;
            _health.Died -= HandleDied;
            _health.Changed += HandleChanged;
            _health.Died += HandleDied;
        }

        private void Start()
        {
            // Emit the starting value once so the HUD shows full health before the first hit.
            // Goes through HandleChanged, so the death guard is untouched.
            HandleChanged(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.Changed -= HandleChanged;
            _health.Died -= HandleDied;
        }

        private void HandleChanged(int current, int max)
        {
            // Health returning above 0 means a reset/respawn happened: arm the next death.
            if (current > 0) _deathRaised = false;

            if (_mode == Mode.Player) GameEvents.RaisePlayerHealthChanged(current, max);
            else GameEvents.RaiseBossHealthChanged(current, max);
        }

        private void HandleDied()
        {
            if (_deathRaised) return;
            _deathRaised = true;

            if (_mode == Mode.Player) GameEvents.RaisePlayerDied();
            else GameEvents.RaiseBossDefeated();
        }
    }
}
