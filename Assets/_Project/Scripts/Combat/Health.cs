using System;
using System.Collections.Generic;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Combat
{
    /// <summary>
    /// Shared hit points for player, enemies, and boss. Enforces the hit rules in one place:
    /// no friendly fire, short invulnerability after a hit, and each AttackId counts once.
    /// Other systems react through the C# events (code) or UnityEvents (inspector hooks for feedback).
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        private const int RememberedAttackIds = 32;

        [SerializeField] private Team _team = Team.Enemy;
        [SerializeField, Min(1)] private int _maxHealth = 3;
        [Tooltip("Seconds after a hit during which further hits are ignored. Player ~0.6, enemies ~0.")]
        [SerializeField, Min(0f)] private float _invulnerabilitySeconds;

        [Header("Inspector hooks (feedback: flash, sound, VFX)")]
        [SerializeField] private UnityEvent _onDamaged;
        [SerializeField] private UnityEvent _onDied;

        private readonly Queue<int> _recentAttackOrder = new Queue<int>();
        private readonly HashSet<int> _recentAttackIds = new HashSet<int>();
        private float _invulnerableUntil;

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        /// <summary>(current, max) on every change, including resets.</summary>
        public event Action<int, int> Changed;

        public Team Team => _team;
        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsDead => Current <= 0;

        private void Awake() => Current = _maxHealth;

        public bool TryApplyDamage(in DamageInfo info)
        {
            if (IsDead || info.SourceTeam == _team || info.Amount <= 0) return false;
            if (Time.time < _invulnerableUntil) return false;
            if (!RememberAttack(info.AttackId)) return false;

            Current = Mathf.Max(0, Current - info.Amount);
            _invulnerableUntil = Time.time + _invulnerabilitySeconds;

            Damaged?.Invoke(info);
            _onDamaged.Invoke();
            Changed?.Invoke(Current, _maxHealth);

            if (IsDead)
            {
                // Runs once: IsDead blocks every later hit until ResetHealth.
                Died?.Invoke();
                _onDied.Invoke();
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(_maxHealth, Current + amount);
            Changed?.Invoke(Current, _maxHealth);
        }

        /// <summary>Full health, clears hit memory. Call from retry/respawn.</summary>
        public void ResetHealth()
        {
            Current = _maxHealth;
            _invulnerableUntil = 0f;
            _recentAttackIds.Clear();
            _recentAttackOrder.Clear();
            Changed?.Invoke(Current, _maxHealth);
        }

        /// <returns>False if this attack already hit us.</returns>
        private bool RememberAttack(int attackId)
        {
            if (attackId == 0) return true; // 0 = untracked damage (e.g. fall damage)
            if (!_recentAttackIds.Add(attackId)) return false;

            _recentAttackOrder.Enqueue(attackId);
            if (_recentAttackOrder.Count > RememberedAttackIds)
                _recentAttackIds.Remove(_recentAttackOrder.Dequeue());
            return true;
        }
    }
}
