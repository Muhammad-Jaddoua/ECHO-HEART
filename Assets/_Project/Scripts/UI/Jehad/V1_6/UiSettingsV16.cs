using System;
using System.Collections.Generic;
using Echoheart.Audio;
using UnityEngine;

namespace Echoheart.UI
{
    [DefaultExecutionOrder(-220)]
    public sealed class UiSettingsV16 : MonoBehaviour
    {
        [SerializeField] private MenuAudioV13 _audio;
        private readonly UiDisplayTimerV16 _timer = new UiDisplayTimerV16();
        private UiValuesV16 _values = UiValuesV16.Defaults;
        private Snapshot _initial, _before;
        private bool _captured;
        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        private static readonly int[] Limits = { 30, 60, 120, -1 };
        public event Action Changed;
        public event Action DisplayReverted;
        public UiValuesV16 Values => _values;
        public bool Previewing => _timer.Active;
        public float PreviewSeconds => _timer.Remaining;
        public Vector2Int DraftResolution { get; private set; }
        public bool DraftFullscreen { get; private set; }
        public int DraftQuality { get; private set; }
        public bool DraftVsync { get; private set; }
        public int DraftLimit { get; private set; }
        public string QualityName => QualitySettings.names.Length == 0 ? "Default" : QualitySettings.names[Mathf.Clamp(DraftQuality, 0, QualitySettings.names.Length - 1)];
        public string LimitName => DraftLimit < 0 ? "Unlimited" : DraftLimit + " fps";
        private struct Snapshot { public int Width, Height, Quality, Sync, Limit; public FullScreenMode Mode; }
        private static Snapshot Read() => new Snapshot { Width = Screen.width, Height = Screen.height, Mode = Screen.fullScreenMode, Quality = QualitySettings.GetQualityLevel(), Sync = QualitySettings.vSyncCount, Limit = Application.targetFrameRate };
        private void Awake()
        {
            _initial = Read(); _captured = true;
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 960 && size.y >= 540 && !_resolutions.Contains(size)) _resolutions.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height); if (!_resolutions.Contains(current)) _resolutions.Add(current);
            if (_resolutions.Count == 1) foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080) }) if (!_resolutions.Contains(size)) _resolutions.Add(size);
            _resolutions.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
            if (_audio != null) { _values.Music = _audio.MusicVolume; _values.Sfx = _audio.SfxVolume; }
            ReadDraft();
        }
        public void Set(UiValuesV16 values)
        {
            _values = values.Sanitized();
            if (_audio != null) { _audio.SetMusic(_values.Music); _audio.SetSfx(_values.Sfx); }
            Changed?.Invoke();
        }
        public void RestoreDefaults() { Set(UiValuesV16.Defaults); DraftFrom(_initial); Changed?.Invoke(); }
        public void ReadDraft() { if (!Previewing) { DraftFrom(Read()); Changed?.Invoke(); } }
        private void DraftFrom(Snapshot value)
        {
            DraftResolution = new Vector2Int(value.Width, value.Height); DraftFullscreen = value.Mode != FullScreenMode.Windowed;
            DraftQuality = value.Quality; DraftVsync = value.Sync > 0; DraftLimit = value.Limit;
        }
        public void CycleResolution()
        {
            int index = _resolutions.IndexOf(DraftResolution); DraftResolution = _resolutions[(index + 1) % _resolutions.Count]; Changed?.Invoke();
        }
        public void CycleQuality() { DraftQuality = (DraftQuality + 1) % Mathf.Max(1, QualitySettings.names.Length); Changed?.Invoke(); }
        public void ToggleFullscreen() { DraftFullscreen = !DraftFullscreen; Changed?.Invoke(); }
        public void ToggleVsync() { DraftVsync = !DraftVsync; Changed?.Invoke(); }
        public void CycleLimit()
        {
            int index = Array.IndexOf(Limits, DraftLimit); DraftLimit = Limits[(index + 1) % Limits.Length]; Changed?.Invoke();
        }
        public bool PreviewDisplay()
        {
            if (!_timer.Begin()) return false;
            _before = Read();
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(DraftQuality, true);
            QualitySettings.vSyncCount = DraftVsync ? 1 : 0; Application.targetFrameRate = DraftLimit;
            Screen.SetResolution(DraftResolution.x, DraftResolution.y, DraftFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            Changed?.Invoke(); return true;
        }
        public void KeepDisplay() { if (_timer.Accept()) Changed?.Invoke(); }
        public void RevertDisplay()
        {
            if (!_timer.Active) return; _timer.Accept(); RevertSnapshot();
        }
        private void RevertSnapshot() { Apply(_before); DraftFrom(_before); Changed?.Invoke(); DisplayReverted?.Invoke(); }
        private static void Apply(Snapshot value)
        {
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(value.Quality, true);
            QualitySettings.vSyncCount = value.Sync; Application.targetFrameRate = value.Limit;
            if (Screen.width != value.Width || Screen.height != value.Height || Screen.fullScreenMode != value.Mode) Screen.SetResolution(value.Width, value.Height, value.Mode);
        }
        private void Update() { if (_timer.Tick(Time.unscaledDeltaTime)) RevertSnapshot(); }
        private void OnApplicationFocus(bool focused) { if (!focused) RevertDisplay(); }
        private void OnDisable() { if (_captured) RevertDisplay(); }
        private void OnDestroy() { if (_captured) Apply(_initial); }
    }
}
