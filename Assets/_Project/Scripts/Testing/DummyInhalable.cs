using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Testing
{
    /// <summary>
    /// Stand-in enemy for building the inhale before real enemies exist. Grants its ability once,
    /// hides on capture, and reappears after a delay (mimics the replenishing enemy sources).
    /// </summary>
    public class DummyInhalable : MonoBehaviour, IInhalable
    {
        [SerializeField] private AbilityDefinition _grantedAbility;
        [SerializeField] private float _respawnDelay = 3f;

        private bool _beingInhaled;
        private bool _captured;

        public bool CanBeInhaled => !_captured && !_beingInhaled && isActiveAndEnabled;
        public AbilityDefinition GrantedAbility => _grantedAbility;
        public Transform InhaleTarget => transform;

        public void OnInhaleStart(Transform inhaler)
        {
            _beingInhaled = true;
            Debug.Log($"[{name}] inhale started by {inhaler.name}", this);
        }

        public void OnInhaleCancel()
        {
            _beingInhaled = false;
            Debug.Log($"[{name}] inhale cancelled", this);
        }

        public void OnCaptured()
        {
            if (_captured) { Debug.LogError($"[{name}] captured twice! Inhale must call OnCaptured once.", this); return; }
            _captured = true;
            _beingInhaled = false;
            Debug.Log($"[{name}] captured -> grants {(_grantedAbility ? _grantedAbility.DisplayName : "nothing")}", this);
            SetVisible(false);
            Invoke(nameof(Respawn), _respawnDelay);
        }

        private void Respawn()
        {
            _captured = false;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = visible;
        }
    }
}
