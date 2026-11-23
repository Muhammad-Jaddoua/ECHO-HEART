using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    [DefaultExecutionOrder(-120)]
    public sealed class WorldToastV15 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        private Text _label;
        private CanvasGroup _group;
        private float _remaining;
        private void Awake()
        {
            var root = new GameObject("World discoveries", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false); var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 95;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            var go = new GameObject("Discovery toast", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(Shadow));
            go.transform.SetParent(root.transform, false); var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -132f); rect.sizeDelta = new Vector2(880f, 90f);
            _label = go.GetComponent<Text>(); _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 25; _label.alignment = TextAnchor.MiddleCenter; _label.color = new Color(1f, 0.94f, 0.72f);
            _label.raycastTarget = false; go.GetComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
            _group = go.GetComponent<CanvasGroup>(); _group.blocksRaycasts = false; _group.interactable = false; _group.alpha = 0f;
        }
        public void Show(string message) { _label.text = message; _remaining = 3.2f; }
        private void OnEnable() => GameEvents.RetryRequested += Clear;
        private void OnDisable() => GameEvents.RetryRequested -= Clear;
        private void Clear(RetryScope scope) { _remaining = 0f; if (_group != null) _group.alpha = 0f; }
        private void Update()
        {
            bool playing = _flow != null && _flow.State == GameState.Playing;
            if (playing) _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            _group.alpha = playing ? Mathf.Clamp01(_remaining * 2f) : 0f;
        }
    }
}
