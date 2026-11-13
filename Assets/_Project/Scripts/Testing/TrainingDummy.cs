using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Testing
{
    /// <summary>
    /// Sandbox target for attacks: flashes red when hit, logs damage, and revives after a delay.
    /// Lets combat be tested before real enemies exist. Not used in the shipped scene.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private float _reviveDelay = 2f;
        [SerializeField] private Color _hitColor = Color.red;

        private Health _health;
        private Renderer _renderer;
        private Color _baseColor;
        private float _flashUntil;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _baseColor = _renderer.material.color;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_renderer != null && _flashUntil > 0f && Time.time >= _flashUntil)
            {
                _renderer.material.color = _baseColor;
                _flashUntil = 0f;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            Debug.Log($"[{name}] took {info.Amount} (attack {info.AttackId}) -> {_health.Current}/{_health.Max}", this);
            if (_renderer == null) return;
            _renderer.material.color = _hitColor;
            _flashUntil = Time.time + 0.15f;
        }

        private void OnDied()
        {
            Debug.Log($"[{name}] defeated, reviving in {_reviveDelay}s", this);
            Invoke(nameof(Revive), _reviveDelay);
        }

        private void Revive() => _health.ResetHealth();
    }
}
