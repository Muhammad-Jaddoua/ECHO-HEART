using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Makes the boss warning readable without any art: a ground ring that grows to the slam radius, and a body
    /// colour that pulses toward a warning colour while ANY attack is telegraphed (the ranged charge cue).
    /// Reads BossBrain state only; it never drives gameplay.
    ///
    /// Ring convention: _slamRing is a flat disc whose mesh is 1 unit across at scale 1 (Unity's default
    /// Cylinder/Quad), so its X/Z scale is set to the full diameter = SlamRadius * 2 * TelegraphProgress.
    /// The scale is compensated for the ring parent's scale, and Y is left as authored.
    /// </summary>
    public class BossTelegraphVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit/Unlit

        [SerializeField] private BossBrain _brain;
        [Tooltip("Flat disc child, 1 unit across. Hidden except during a Slam telegraph.")]
        [SerializeField] private Transform _slamRing;
        [Tooltip("Body renderer whose _BaseColor pulses during any telegraph.")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Color _warningColor = new Color(1f, 0.35f, 0.05f, 1f);
        [Tooltip("Pulses per second of the body tint while charging.")]
        [SerializeField, Min(0f)] private float _pulseSpeed = 6f;

        private MaterialPropertyBlock _block;   // cached: no per-frame allocation
        private Color _baseColor = Color.white;
        private bool _tinted;
        private bool _ringShown;
        private float _ringHeight = 1f;
        private bool _initialised;

        private void Awake() => Init();

        private void Init()
        {
            if (_initialised) return;
            _initialised = true;
            _block = new MaterialPropertyBlock();

            if (_bodyRenderer != null && _bodyRenderer.sharedMaterial != null &&
                _bodyRenderer.sharedMaterial.HasProperty(BaseColorId))
                _baseColor = _bodyRenderer.sharedMaterial.GetColor(BaseColorId);

            if (_slamRing != null)
            {
                _ringHeight = _slamRing.localScale.y;
                SetRingShown(false);
            }
        }

        private void OnDisable() => Restore();

        private void Update() => Refresh();

        /// <summary>One visual update. Separate from Update only so BossSelfTest can drive it in edit mode.</summary>
        internal void Refresh()
        {
            Init();
            if (_brain == null)
            {
                Restore();
                return;
            }

            bool telegraphing = _brain.Current == BossBrain.State.Telegraph;
            float progress = telegraphing ? _brain.TelegraphProgress : 0f;

            UpdateRing(telegraphing && _brain.CurrentAttack == BossBrain.AttackKind.Slam, progress);
            UpdateTint(telegraphing, progress);
        }

        private void UpdateRing(bool show, float progress)
        {
            if (_slamRing == null) return;
            if (!show || _brain.Definition == null)
            {
                SetRingShown(false);
                return;
            }

            float diameter = _brain.Definition.SlamRadius * 2f * progress;
            Transform parent = _slamRing.parent;
            float parentScale = parent != null && parent.lossyScale.x > 0.0001f ? parent.lossyScale.x : 1f;
            float local = diameter / parentScale;
            _slamRing.localScale = new Vector3(local, _ringHeight, local);
            SetRingShown(true);
        }

        private void SetRingShown(bool shown)
        {
            if (_ringShown == shown && _slamRing.gameObject.activeSelf == shown) return;
            _ringShown = shown;
            _slamRing.gameObject.SetActive(shown);
        }

        private void UpdateTint(bool telegraphing, float progress)
        {
            if (_bodyRenderer == null) return;
            if (!telegraphing)
            {
                Restore();
                return;
            }

            // Stronger and faster-flickering as the warning runs out; never fully the base colour at the end.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI * 2f);
            float amount = Mathf.Clamp01(progress * (0.6f + 0.4f * pulse));
            _bodyRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, Color.Lerp(_baseColor, _warningColor, amount));
            _bodyRenderer.SetPropertyBlock(_block);
            _tinted = true;
        }

        private void Restore()
        {
            if (!_initialised) return;
            if (_ringShown && _slamRing != null) SetRingShown(false);
            if (!_tinted || _bodyRenderer == null) return;
            _tinted = false;
            _bodyRenderer.SetPropertyBlock(null); // drops the override: back to the material's own colour
        }
    }
}
