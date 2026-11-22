using Echoheart.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoheart.World
{
    /// <summary>Boss-gated proximity interaction. Restoration can commit only once per run.</summary>
    public sealed class IslandHeartV13 : MonoBehaviour, IResettable
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private Transform _visual;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _interactDistance = 3f;
        private MaterialPropertyBlock _properties;
        private Vector3 _baseScale;
        private Light _light;
        private bool _restored;
        private bool _promptShown;
        private float _restoration;

        public bool Restored => _restored;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _baseScale = _visual != null ? _visual.localScale : Vector3.one;
            var lightObject = new GameObject("Heart light");
            lightObject.transform.SetParent(transform, false);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 8f;
            _light.shadows = LightShadows.None;
            ApplyVisual(0f);
        }

        private void Update()
        {
            if (_flow == null) return;
            if (_restored)
            {
                _restoration = Mathf.MoveTowards(_restoration, 1f, Time.unscaledDeltaTime * 1.8f);
                ApplyVisual(_restoration);
                return;
            }
            bool near = _flow.Player != null && _flow.Player.Player != null &&
                Vector3.Distance(_flow.Player.Player.position, transform.position) <= _interactDistance;
            bool show = near && _flow.State == GameState.Playing && _flow.HeartUnlocked;
            if (show != _promptShown)
            {
                _promptShown = show;
                GameEvents.RaisePromptChanged(show ? "E  Restore the Island Heart" : string.Empty);
            }
            if (_flow.State == GameState.Playing) ApplyVisual(0f);
            if (show && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
        }

        public bool TryInteract()
        {
            if (_restored || _flow == null || !_flow.HeartUnlocked || _flow.State != GameState.Playing ||
                _flow.Player == null || _flow.Player.Player == null ||
                Vector3.Distance(_flow.Player.Player.position, transform.position) > _interactDistance) return false;
            _restored = true;
            if (!_flow.CompleteFromHeart()) { _restored = false; return false; }
            _promptShown = false;
            return true;
        }

        private void ApplyVisual(float restored)
        {
            bool unlocked = _flow != null && _flow.HeartUnlocked;
            Color locked = new Color(0.40f, 0.18f, 0.52f);
            Color gold = new Color(1f, 0.73f, 0.24f);
            Color mint = new Color(0.30f, 1f, 0.78f);
            Color color = Color.Lerp(unlocked ? gold : locked, mint, restored);
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_properties);
                _properties.SetColor("_BaseColor", color);
                _renderer.SetPropertyBlock(_properties);
            }
            if (_visual != null)
                _visual.localScale = _baseScale * (1f + restored * 0.35f + (unlocked && !_restored ? Mathf.Sin(Time.time * 2f) * 0.04f : 0f));
            if (_light != null)
            {
                _light.color = color;
                _light.intensity = Mathf.Lerp(unlocked ? 1.8f : 0.4f, 4f, restored);
            }
        }

        public void ResetFor(RetryScope scope)
        {
            if (scope == RetryScope.MainArea) return;
            _restored = false;
            _promptShown = false;
            _restoration = 0f;
            ApplyVisual(0f);
        }

        private void OnDisable()
        {
            if (_promptShown) GameEvents.RaisePromptChanged(string.Empty);
            _promptShown = false;
        }
    }
}
