using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.World
{
    /// <summary>Inspector boundary for the player's controls, health, pose and ability holder.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class PlayerFlowBindingV13 : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private Health _health;
        [SerializeField] private Behaviour[] _gameplayInput;
        [SerializeField] private UnityEvent _onRespawned = new UnityEvent();
        [SerializeField] private UnityEvent _clearAbilityRequested = new UnityEvent();
        [SerializeField] private UnityEvent<AbilityDefinition> _equipAbilityRequested = new UnityEvent<AbilityDefinition>();
        [Tooltip("Only the isolated review capsule publishes the requested ability itself. Turn off when connecting the real holder.")]
        [SerializeField] private bool _publishReviewAbilityChanges = true;
        private CharacterController _controller;

        public Transform Player => _player;
        public Health Health => _health;
        public AbilityDefinition CurrentAbility { get; private set; }

        private void Awake()
        {
            if (_player != null) _player.TryGetComponent(out _controller);
        }

        private void OnEnable() => GameEvents.AbilityChanged += ObserveAbility;
        private void OnDisable() => GameEvents.AbilityChanged -= ObserveAbility;
        private void ObserveAbility(AbilityDefinition ability) => CurrentAbility = ability;

        public void SetControlsEnabled(bool enabled)
        {
            if (_gameplayInput == null) return;
            foreach (Behaviour input in _gameplayInput) if (input != null) input.enabled = enabled;
        }

        public void ApplyAbility(AbilityDefinition ability)
        {
            if (ability == null) _clearAbilityRequested.Invoke();
            else _equipAbilityRequested.Invoke(ability);
            if (_publishReviewAbilityChanges) GameEvents.RaiseAbilityChanged(ability);
        }

        public void RestoreHealth()
        {
            if (_health != null) _health.ResetHealth();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_player == null) return;
            bool enabled = _controller != null && _controller.enabled;
            if (_controller != null) _controller.enabled = false;
            _player.SetPositionAndRotation(position, rotation);
            if (_controller != null) _controller.enabled = enabled;
            Physics.SyncTransforms();
            _onRespawned.Invoke();
        }
    }
}
