using System;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Scene-local menus and an event-driven review HUD. Uses the project's Input System.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class FlowMenuV13 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private MenuAudioV13 _audio;
        [SerializeField] private bool _showReviewHud = true;
        private static readonly Color Ink = new Color(0.025f, 0.07f, 0.10f, 0.94f);
        private static readonly Color Cream = new Color(0.95f, 0.96f, 0.88f);
        private static readonly Color Mint = new Color(0.39f, 0.91f, 0.73f);
        private static readonly Color Muted = new Color(0.67f, 0.79f, 0.78f);
        private Font _font;
        private RectTransform _overlay;
        private RectTransform _hud;
        private RectTransform _title;
        private RectTransform _pause;
        private RectTransform _death;
        private RectTransform _complete;
        private RectTransform _modal;
        private RectTransform _healthFill;
        private RectTransform _bossFill;
        private RectTransform _bossHud;
        private CanvasGroup _fade;
        private Button _startButton;
        private Button _resumeButton;
        private Button _retryButton;
        private Button _againButton;
        private Button _cancelButton;
        private Text _healthText;
        private Text _abilityText;
        private Text _relicText;
        private Text _objective;
        private Text _prompt;
        private Text _deathBody;
        private Text _completeStats;
        private Text _modalTitle;
        private Text _modalBody;
        private Action _pendingConfirm;
        private GameObject _previousSelection;
        private bool _bossVisible;
        private bool _bossDefeated;

        public GameState VisibleState { get; private set; } = GameState.Title;
        public bool ConfirmationOpen => _modal != null && _modal.gameObject.activeSelf;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += ShowState;
            GameEvents.PlayerHealthChanged += ShowHealth;
            GameEvents.AbilityChanged += ShowAbility;
            GameEvents.CollectiblesChanged += ShowRelics;
            GameEvents.BossEncounterStarted += ShowBoss;
            GameEvents.BossHealthChanged += ShowBossHealth;
            GameEvents.BossDefeated += HideBoss;
            GameEvents.PromptChanged += ShowPrompt;
            GameEvents.RetryRequested += HandleRetry;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= ShowState;
            GameEvents.PlayerHealthChanged -= ShowHealth;
            GameEvents.AbilityChanged -= ShowAbility;
            GameEvents.CollectiblesChanged -= ShowRelics;
            GameEvents.BossEncounterStarted -= ShowBoss;
            GameEvents.BossHealthChanged -= ShowBossHealth;
            GameEvents.BossDefeated -= HideBoss;
            GameEvents.PromptChanged -= ShowPrompt;
            GameEvents.RetryRequested -= HandleRetry;
        }

        private void Start() => ShowState(_flow.State);

        private void Update()
        {
            if (_fade != null && _overlay.gameObject.activeSelf)
                _fade.alpha = Mathf.MoveTowards(_fade.alpha, 1f, Time.unscaledDeltaTime * 5f);
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (ConfirmationOpen) CloseConfirmation();
            else if (_flow.State == GameState.Playing) _flow.Pause();
            else if (_flow.State == GameState.Paused) _flow.Resume();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var system = new GameObject("Menu Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                system.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning("V1.3 menus require an InputSystemUIInputModule on the existing EventSystem.", this);
            }
        }

        private void BuildCanvas()
        {
            var go = new GameObject("Echoheart Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = (RectTransform)go.transform;
            _hud = Stretch("HUD", root);
            BuildHud();
            _overlay = Stretch("Menu overlay", root);
            Paint(_overlay, new Color(0.012f, 0.04f, 0.07f, 0.38f));
            _fade = _overlay.gameObject.AddComponent<CanvasGroup>();
            BuildTitle();
            BuildPause();
            BuildDeath();
            BuildComplete();
            BuildConfirmation();
            ShowState(GameState.Title);
        }

        private void BuildTitle()
        {
            _title = Card("Title", _overlay, new Vector2(600f, 770f), new Vector2(96f, 0f), new Vector2(0f, 0.5f));
            Label(_title, "SWORD  /  BOW  /  FIRE", 46f, -49f, 510f, 28f, 18, Mint);
            Label(_title, "ECHOHEART", 42f, -106f, 530f, 100f, 76, Cream, true);
            Accent(_title, 46f, -218f, 92f);
            Label(_title, "Borrow a power.\nRestore the Heart.", 46f, -251f, 500f, 115f, 34, Cream);
            Label(_title, "Explore the valley, discover hidden relics,\nand face the Corrupted Guardian.", 46f, -391f, 510f, 90f, 22, Muted);
            _startButton = ButtonAt(_title, "New game", 46f, -513f, 508f, () => _flow.StartNewGame(), true);
            ButtonAt(_title, "Quit game", 46f, -599f, 508f, () => Application.Quit(), false);
            Label(_title, "WASD move   •   Space jump   •   Esc pause", 46f, -705f, 515f, 28f, 17, Muted);
        }

        private void BuildPause()
        {
            _pause = Card("Pause", _overlay, new Vector2(560f, 780f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Label(_pause, "TAKE A BREATH", 42f, -37f, 470f, 26f, 17, Mint);
            Label(_pause, "Paused", 38f, -82f, 475f, 75f, 58, Cream, true);
            _resumeButton = ButtonAt(_pause, "Resume", 42f, -190f, 476f, () => _flow.Resume(), true);
            ButtonAt(_pause, "Retry area", 42f, -274f, 476f, () =>
                Confirm("Retry this area?", _flow.CurrentRetryScope == RetryScope.BossArena
                    ? "Restart the Guardian fight with your selected power. Your relics stay collected."
                    : "Return to the entry with full health and no power. Your relics stay collected.", () => _flow.Retry()), false);
            Label(_pause, "AUDIO", 44f, -382f, 470f, 24f, 16, Mint);
            SliderAt(_pause, "Music", -424f, _audio != null ? _audio.MusicVolume : 0.65f, value => { if (_audio != null) _audio.SetMusic(value); });
            SliderAt(_pause, "Sound effects", -516f, _audio != null ? _audio.SfxVolume : 0.8f, value => { if (_audio != null) _audio.SetSfx(value); });
            ButtonAt(_pause, "Return to title", 42f, -638f, 476f, () =>
                Confirm("Return to title?", "This session has no saved progress. Start a new game to explore again.", () => _flow.ReturnToTitle()), false);
            Label(_pause, "Esc resumes the game", 44f, -733f, 470f, 24f, 16, Muted);
        }

        private void BuildDeath()
        {
            _death = Card("Death", _overlay, new Vector2(560f, 610f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Label(_death, "THE JOURNEY CONTINUES", 42f, -39f, 480f, 28f, 17, Mint);
            Label(_death, "Try again", 38f, -91f, 480f, 80f, 58, Cream, true);
            _deathBody = Label(_death, "", 42f, -208f, 476f, 108f, 24, Muted);
            _retryButton = ButtonAt(_death, "Retry", 42f, -372f, 476f, () => _flow.Retry(), true);
            ButtonAt(_death, "Return to title", 42f, -458f, 476f, () => _flow.ReturnToTitle(), false);
        }

        private void BuildComplete()
        {
            _complete = Card("Complete", _overlay, new Vector2(620f, 680f), Vector2.zero, new Vector2(0.5f, 0.5f));
            Label(_complete, "THE ISLAND BREATHES AGAIN", 44f, -40f, 530f, 28f, 18, Mint);
            Label(_complete, "Heart restored", 38f, -97f, 550f, 84f, 54, Cream, true);
            Label(_complete, "The Guardian rests.\nThe valley is yours to remember.", 44f, -216f, 530f, 98f, 25, Muted);
            _completeStats = Label(_complete, "", 44f, -352f, 530f, 60f, 23, Cream);
            _againButton = ButtonAt(_complete, "Play again", 44f, -466f, 532f, () => _flow.StartNewGame(), true);
            ButtonAt(_complete, "Return to title", 44f, -552f, 532f, () => _flow.ReturnToTitle(), false);
        }

        private void BuildConfirmation()
        {
            _modal = Stretch("Confirmation", _overlay);
            Paint(_modal, new Color(0f, 0.015f, 0.025f, 0.78f));
            RectTransform card = Card("Confirm card", _modal, new Vector2(620f, 490f), Vector2.zero, new Vector2(0.5f, 0.5f));
            _modalTitle = Label(card, "", 40f, -48f, 540f, 70f, 38, Cream, true);
            _modalBody = Label(card, "", 40f, -160f, 540f, 112f, 23, Muted);
            ButtonAt(card, "Confirm", 40f, -305f, 540f, () =>
            {
                Action action = _pendingConfirm;
                CloseConfirmation();
                action?.Invoke();
            }, true);
            _cancelButton = ButtonAt(card, "Cancel", 40f, -389f, 540f, CloseConfirmation, false);
            _modal.gameObject.SetActive(false);
        }

        private void BuildHud()
        {
            RectTransform status = Card("Player status", _hud, new Vector2(360f, 158f), new Vector2(26f, -24f), new Vector2(0f, 1f));
            Label(status, "ECHOHEART", 20f, -14f, 320f, 26f, 17, Mint, true);
            _healthText = Label(status, "5 / 5", 240f, -52f, 96f, 27f, 20, Cream);
            RectTransform health = Node("Health", status, new Vector2(205f, 12f), new Vector2(20f, -59f), new Vector2(0f, 1f));
            Paint(health, new Color(1f, 1f, 1f, 0.13f));
            _healthFill = Stretch("Health fill", health); Paint(_healthFill, Mint);
            _abilityText = Label(status, "Power   None", 20f, -92f, 320f, 26f, 19, Cream);
            _relicText = Label(status, "Relics   0 / 16", 20f, -124f, 320f, 24f, 18, Muted);
            _objective = Label(_hud, "Follow the trails to the Guardian's arena", 26f, -201f, 640f, 35f, 22, Cream);
            _prompt = Label(_hud, "", 0f, 110f, 640f, 44f, 26, Cream, true);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _prompt.rectTransform.pivot = new Vector2(0.5f, 0f);
            _prompt.alignment = TextAnchor.MiddleCenter;
            Text controls = Label(_hud, "WASD  Move    Shift  Sprint    Space  Jump    MMB  Orbit    Esc  Pause", 26f, 25f, 900f, 25f, 17, Cream);
            controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = controls.rectTransform.pivot = Vector2.zero;
            foreach (Text label in new[] { _objective, _prompt, controls })
            {
                Shadow shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0.02f, 0.03f, 0.85f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
            RectTransform pause = Node("Pause shortcut", _hud, new Vector2(146f, 56f), new Vector2(-26f, -24f), new Vector2(1f, 1f));
            ButtonAt(pause, "Pause", 0f, 0f, 146f, () => _flow.Pause(), false);
            _bossHud = Card("Boss status", _hud, new Vector2(520f, 83f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));
            Label(_bossHud, "CORRUPTED GUARDIAN", 22f, -12f, 476f, 27f, 19, Cream, true);
            RectTransform boss = Node("Boss health", _bossHud, new Vector2(476f, 12f), new Vector2(22f, -52f), new Vector2(0f, 1f));
            Paint(boss, new Color(1f, 1f, 1f, 0.13f)); _bossFill = Stretch("Boss fill", boss); Paint(_bossFill, new Color(0.70f, 0.43f, 0.85f));
            _bossHud.gameObject.SetActive(false);
        }

        private void ShowState(GameState state)
        {
            if (_title == null) return;
            bool changed = VisibleState != state;
            VisibleState = state;
            CloseConfirmation();
            _title.gameObject.SetActive(state == GameState.Title);
            _pause.gameObject.SetActive(state == GameState.Paused);
            _death.gameObject.SetActive(state == GameState.Dead);
            _complete.gameObject.SetActive(state == GameState.Complete);
            _overlay.gameObject.SetActive(state != GameState.Playing);
            _hud.gameObject.SetActive(_showReviewHud && state == GameState.Playing);
            if (changed && _fade != null) _fade.alpha = 0f;
            if (state == GameState.Title) { _bossVisible = false; _bossDefeated = false; }
            if (_bossHud != null) _bossHud.gameObject.SetActive(_bossVisible);
            if (_deathBody != null) _deathBody.text = _flow.CurrentRetryScope == RetryScope.BossArena
                ? "The Guardian is still waiting.\nYour selected power stays with you."
                : "Take a breath. Return to the clearing\nand find your next power.";
            if (_completeStats != null && _flow.Relics != null)
                _completeStats.text = $"Relics found  {_flow.Relics.Collected} / {_flow.Relics.Total}\nExploration time  {Mathf.FloorToInt(_flow.ElapsedSeconds / 60f):00}:{Mathf.FloorToInt(_flow.ElapsedSeconds % 60f):00}";
            UpdateObjective();
            Button selected = state == GameState.Title ? _startButton : state == GameState.Paused ? _resumeButton : state == GameState.Dead ? _retryButton : state == GameState.Complete ? _againButton : null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected != null ? selected.gameObject : null);
        }

        private void ShowHealth(int current, int max)
        {
            if (_healthText != null) _healthText.text = $"{current} / {max}";
            if (_healthFill != null) _healthFill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)current / max) : 0f, 1f);
        }
        private void ShowAbility(AbilityDefinition ability) { if (_abilityText != null) _abilityText.text = "Power   " + (ability != null ? ability.DisplayName : "None"); }
        private void ShowRelics(int current, int total) { if (_relicText != null) _relicText.text = $"Relics   {current} / {total}"; }
        private void ShowPrompt(string text) { if (_prompt != null) _prompt.text = text; }
        private void ShowBoss() { _bossVisible = true; _bossDefeated = false; if (_bossHud != null) _bossHud.gameObject.SetActive(true); UpdateObjective(); }
        private void ShowBossHealth(int current, int max) { if (_bossFill != null) _bossFill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)current / max) : 0f, 1f); }
        private void HideBoss() { _bossVisible = false; _bossDefeated = true; if (_bossHud != null) _bossHud.gameObject.SetActive(false); UpdateObjective(); }
        private void HandleRetry(RetryScope scope) { _bossVisible = false; _bossDefeated = false; if (_bossHud != null) _bossHud.gameObject.SetActive(false); UpdateObjective(); }
        private void UpdateObjective()
        {
            if (_objective != null) _objective.text = _bossDefeated || _flow.HeartUnlocked ? "Restore the Island Heart" : _bossVisible ? "Defeat the Corrupted Guardian" : "Follow the trails to the Guardian's arena";
        }

        private void Confirm(string title, string body, Action action)
        {
            _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            _pendingConfirm = action; _modalTitle.text = title; _modalBody.text = body; _modal.gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_cancelButton.gameObject);
        }

        private void CloseConfirmation()
        {
            if (_modal == null) return;
            bool wasOpen = _modal.gameObject.activeSelf;
            _modal.gameObject.SetActive(false); _pendingConfirm = null;
            if (wasOpen && _previousSelection != null && _previousSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_previousSelection);
        }

        private RectTransform Card(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            RectTransform card = Node(name, parent, size, position, anchor); Paint(card, Ink);
            return card;
        }
        private void Accent(Transform parent, float x, float y, float width) { Paint(Node("Accent", parent, new Vector2(width, 4f), new Vector2(x, y), new Vector2(0f, 1f)), Mint); }
        private RectTransform Node(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Node(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private Image Paint(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
        }
        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, bool bold = false)
        {
            RectTransform rect = Node("Label", parent, new Vector2(width, height), new Vector2(x, y), new Vector2(0f, 1f));
            Text text = rect.gameObject.AddComponent<Text>(); text.font = _font; text.text = value; text.fontSize = size; text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private Button ButtonAt(Transform parent, string text, float x, float y, float width, Action action, bool primary)
        {
            RectTransform rect = Node(text, parent, new Vector2(width, 66f), new Vector2(x, y), new Vector2(0f, 1f));
            Image image = Paint(rect, Color.white);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = primary ? Mint : new Color(0.10f, 0.19f, 0.21f);
            colors.highlightedColor = primary ? new Color(0.66f, 1f, 0.86f) : new Color(0.16f, 0.32f, 0.32f);
            colors.selectedColor = primary ? new Color(0.70f, 1f, 0.88f) : new Color(0.18f, 0.38f, 0.36f);
            colors.pressedColor = new Color(0.28f, 0.64f, 0.53f); colors.fadeDuration = 0.08f; button.colors = colors;
            Text label = Label(rect, text, 20f, -17f, width - 40f, 40f, 26, primary ? new Color(0.03f, 0.13f, 0.14f) : Cream, true); label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() => { if (_audio != null) _audio.PlayPress(); action(); });
            return button;
        }
        private void SliderAt(Transform parent, string name, float y, float value, Action<float> changed)
        {
            Label(parent, name, 44f, y, 310f, 26f, 20, Cream);
            Text percent = Label(parent, $"{Mathf.RoundToInt(value * 100f)}%", 430f, y, 88f, 26f, 18, Muted);
            RectTransform rect = Node(name + " slider", parent, new Vector2(476f, 30f), new Vector2(42f, y - 37f), new Vector2(0f, 1f));
            RectTransform track = Node("Track", rect, new Vector2(476f, 6f), new Vector2(0f, -12f), new Vector2(0f, 1f)); Paint(track, new Color(1f, 1f, 1f, 0.15f));
            RectTransform fill = Stretch("Fill", track); Paint(fill, Mint);
            RectTransform handles = Stretch("Handle area", rect); handles.offsetMin = new Vector2(8f, 0f); handles.offsetMax = new Vector2(-8f, 0f);
            RectTransform handle = Node("Handle", handles, new Vector2(18f, 24f), Vector2.zero, new Vector2(0f, 0.5f)); Image knob = Paint(handle, Cream);
            Slider slider = rect.gameObject.AddComponent<Slider>(); slider.targetGraphic = knob; slider.fillRect = fill; slider.handleRect = handle; slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { percent.text = $"{Mathf.RoundToInt(v * 100f)}%"; changed(v); });
        }
    }
}
