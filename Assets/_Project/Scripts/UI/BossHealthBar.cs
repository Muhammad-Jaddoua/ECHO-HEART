using Echoheart.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>
    /// Boss health bar. Self-contained: it listens ONLY to GameEvents and never references the boss, so it can
    /// live in any canvas (the HUD canvas belongs to another teammate). Shown from BossEncounterStarted until
    /// the boss dies, the player dies, a retry happens or the game returns to Title.
    /// Visibility uses a CanvasGroup (not SetActive) so this component keeps listening while the bar is hidden.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("Visibility")]
        [Tooltip("Alpha 0 + no raycasts while hidden. Put it on the bar's root.")]
        [SerializeField] private CanvasGroup _group;
        [Tooltip("Seconds to keep the empty bar after BossDefeated. 0 hides immediately.")]
        [SerializeField] private float _hideDelayAfterDefeat = 1.0f;

        [Header("Fill")]
        [Tooltip("Main fill. If Image Type is Filled it uses fillAmount, otherwise it scales its RectTransform on X.")]
        [SerializeField] private Image _fill;
        [Tooltip("Optional pale 'damage taken' fill behind the main one; eases down so hits are readable.")]
        [SerializeField] private Image _ghost;
        [SerializeField] private float _ghostDelay = 0.35f;
        [SerializeField] private float _ghostSpeed = 0.6f;

        [Header("Text (optional)")]
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _bossName = "Corrupted Guardian";
        [SerializeField] private bool _showNumbers = true;

        private float _target = 1f;
        private float _ghostValue = 1f;
        private float _ghostHoldUntil;
        private float _hideAt = -1f;
        private bool _warned;

        public bool IsVisible { get; private set; }
        /// <summary>Normalised value currently shown on the main fill (0..1).</summary>
        public float FillValue => _target;

        private void Awake()
        {
            Validate();
            SetVisible(false);
            ApplyFill(1f);
            RefreshLabel(-1, -1);
        }

        private void OnEnable()
        {
            GameEvents.BossEncounterStarted += OnEncounterStarted;
            GameEvents.BossHealthChanged += OnHealthChanged;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.RetryRequested += OnRetry;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.BossEncounterStarted -= OnEncounterStarted;
            GameEvents.BossHealthChanged -= OnHealthChanged;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.RetryRequested -= OnRetry;
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            // Unscaled time so the bar still animates if a pause/hit-stop sets timeScale to 0.
            if (_hideAt >= 0f && Time.unscaledTime >= _hideAt)
            {
                _hideAt = -1f;
                SetVisible(false);
            }

            if (_ghost != null && _ghostValue > _target && Time.unscaledTime >= _ghostHoldUntil)
            {
                _ghostValue = Mathf.Max(_target, _ghostValue - _ghostSpeed * Time.unscaledDeltaTime);
                ApplyGhost(_ghostValue);
            }
        }

        private void OnEncounterStarted()
        {
            _hideAt = -1f;
            SetVisible(true);
        }

        private void OnHealthChanged(int current, int max)
        {
            float value = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            bool lost = value < _target;
            _target = value;
            ApplyFill(value);

            if (lost)
            {
                _ghostHoldUntil = Time.unscaledTime + _ghostDelay;
            }
            else
            {
                // Healing/reset: the ghost must never sit below the bar.
                _ghostValue = value;
                ApplyGhost(value);
            }

            RefreshLabel(current, max);
        }

        private void OnBossDefeated()
        {
            if (_hideDelayAfterDefeat <= 0f)
            {
                HideNow();
            }
            else
            {
                _hideAt = Time.unscaledTime + _hideDelayAfterDefeat;
            }
        }

        private void OnPlayerDied() => HideNow();

        // Every scope hides: the bar belongs to the arena fight, and the boss re-announces itself via
        // BossEncounterStarted/BossHealthChanged when the fight starts again.
        private void OnRetry(RetryScope scope)
        {
            HideNow();
            ResetFill();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Title)
            {
                HideNow();
                ResetFill();
            }
        }

        private void HideNow()
        {
            _hideAt = -1f;
            SetVisible(false);
        }

        private void ResetFill()
        {
            _target = 1f;
            _ghostValue = 1f;
            ApplyFill(1f);
            ApplyGhost(1f);
            RefreshLabel(-1, -1);
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void ApplyFill(float value) => ApplyTo(_fill, value);

        private void ApplyGhost(float value) => ApplyTo(_ghost, value);

        private static void ApplyTo(Image image, float value)
        {
            if (image == null) return;
            if (image.type == Image.Type.Filled)
            {
                image.fillAmount = value;
                return;
            }
            Vector3 scale = image.rectTransform.localScale;
            scale.x = value;
            image.rectTransform.localScale = scale;
        }

        private void RefreshLabel(int current, int max)
        {
            if (_label == null) return;
            _label.text = _showNumbers && max > 0 ? $"{_bossName}  {current}/{max}" : _bossName;
        }

        private void Validate()
        {
            if (_warned) return;
            if (_group == null || _fill == null)
            {
                _warned = true;
                Debug.LogWarning("[BossHealthBar] Assign _group (CanvasGroup) and _fill (Image); the bar will not display correctly.", this);
            }
        }
    }
}
