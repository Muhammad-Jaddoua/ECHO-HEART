using System;
using System.Collections.Generic;
using Echoheart.Audio;
using UnityEngine;

namespace Echoheart.UI
{
    [DefaultExecutionOrder(-220)]
    public sealed class UiSettingsV17 : MonoBehaviour
    {
        [SerializeField] private MenuAudioV13 _audio;
        private readonly UiDisplayEditV17 _edit = new UiDisplayEditV17();
        private UiValuesV16 _values = UiValuesV16.Defaults;
        private UiDisplayValueV17 _initial;
        private bool _captured, _touchedGraphics;
        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        private static readonly int[] Limits = { 30, 60, 120, -1 };
        public event Action Changed;
        public event Action DisplayReverted;
        public UiValuesV16 Values => _values;
        public bool Previewing => _edit.Previewing;
        public bool HasDisplayChanges => _edit.Dirty;
        public float PreviewSeconds => _edit.Remaining;
        public Vector2Int DraftResolution => new Vector2Int(_edit.Draft.Width, _edit.Draft.Height);
        public bool DraftFullscreen => _edit.Draft.Mode != (int)FullScreenMode.Windowed;
        public string ModeName => _edit.Draft.Mode == (int)FullScreenMode.Windowed ? "Windowed" : _edit.Draft.Mode == (int)FullScreenMode.ExclusiveFullScreen ? "Exclusive fullscreen" : _edit.Draft.Mode == (int)FullScreenMode.MaximizedWindow ? "Maximized window" : "Borderless fullscreen";
        public int DraftQuality => _edit.Draft.Quality;
        public bool DraftVsync => _edit.Draft.Sync > 0;
        public string SyncName => _edit.Draft.Sync > 1 ? "On (every " + _edit.Draft.Sync + " refreshes)" : DraftVsync ? "On" : "Off";
        public int DraftLimit => _edit.Draft.Limit;
        public string QualityName => QualitySettings.names.Length == 0 ? "Default" : QualitySettings.names[Mathf.Clamp(DraftQuality, 0, QualitySettings.names.Length - 1)];
        public string LimitName => DraftLimit < 0 ? "Unlimited" : DraftLimit + " fps";
        private static UiDisplayValueV17 Read() => new UiDisplayValueV17 { Width = Screen.width, Height = Screen.height, Mode = (int)Screen.fullScreenMode, Quality = QualitySettings.GetQualityLevel(), Sync = QualitySettings.vSyncCount, Limit = Application.targetFrameRate };
        private void Awake()
        {
            _initial = Read(); _captured = true;
            RefreshResolutions();
            if (_audio != null) { _values.Music = _audio.MusicVolume; _values.Sfx = _audio.SfxVolume; }
            ReadDraft();
        }
        private void RefreshResolutions()
        {
            _resolutions.Clear();
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 960 && size.y >= 540 && !_resolutions.Contains(size)) _resolutions.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height); if (!_resolutions.Contains(current)) _resolutions.Add(current);
            if (_resolutions.Count == 1) foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080) }) if (!_resolutions.Contains(size)) _resolutions.Add(size);
            _resolutions.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
        }
        public void Set(UiValuesV16 values)
        {
            _values = values.Sanitized();
            if (_audio != null) { _audio.SetMusic(_values.Music); _audio.SetSfx(_values.Sfx); }
            Changed?.Invoke();
        }
        public void RestoreDefaults() { Set(UiValuesV16.Defaults); _edit.Stage(_initial); Changed?.Invoke(); }
        public void ReadDraft() { if (!Previewing) { RefreshResolutions(); _edit.Sync(Read()); Changed?.Invoke(); } }
        public void DiscardDisplay() { _edit.Discard(); Changed?.Invoke(); }
        private void Stage(UiDisplayValueV17 value) { if (_edit.Stage(value)) Changed?.Invoke(); }
        // step is +1 (next) or -1 (previous) so an overshoot can be undone without wrapping all the way round.
        private static int Wrap(int index, int step, int count) => ((index + step) % count + count) % count;
        public void CycleResolution(int step = 1)
        {
            int index = _resolutions.IndexOf(DraftResolution); Vector2Int size = _resolutions[Wrap(Mathf.Max(index, 0), step, _resolutions.Count)];
            var draft = _edit.Draft; draft.Width = size.x; draft.Height = size.y; Stage(draft);
        }
        public void CycleQuality(int step = 1) { var draft=_edit.Draft; draft.Quality=Wrap(draft.Quality,step,Mathf.Max(1,QualitySettings.names.Length)); Stage(draft); }
        public void ToggleFullscreen() { var draft=_edit.Draft; draft.Mode=draft.Mode==(int)FullScreenMode.Windowed?(int)FullScreenMode.FullScreenWindow:(int)FullScreenMode.Windowed; Stage(draft); }
        public void ToggleVsync() { var draft=_edit.Draft; draft.Sync=draft.Sync>0?0:1; Stage(draft); }
        public void CycleLimit(int step = 1) { var draft=_edit.Draft; draft.Limit=Limits[Wrap(Mathf.Max(Array.IndexOf(Limits,draft.Limit),0),step,Limits.Length)]; Stage(draft); }
        public bool PreviewDisplay()
        {
            if (!_edit.Begin(Read())) return false;
            _touchedGraphics=true; Apply(_edit.Draft); Changed?.Invoke(); return true;
        }
        public void KeepDisplay() { if (_edit.Keep(Read())) Changed?.Invoke(); }
        public void RevertDisplay() { if (_edit.Revert()) RevertSnapshot(); }
        private void RevertSnapshot() { Apply(_edit.Before); Changed?.Invoke(); DisplayReverted?.Invoke(); }
        private static void Apply(UiDisplayValueV17 value)
        {
            if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel()!=value.Quality) QualitySettings.SetQualityLevel(value.Quality, true);
            if (QualitySettings.vSyncCount!=value.Sync) QualitySettings.vSyncCount=value.Sync;
            if (Application.targetFrameRate!=value.Limit) Application.targetFrameRate=value.Limit;
            if (Screen.width!=value.Width || Screen.height!=value.Height || (int)Screen.fullScreenMode!=value.Mode) Screen.SetResolution(value.Width,value.Height,(FullScreenMode)value.Mode);
        }
        private void Update() { if (_edit.Tick(Time.unscaledDeltaTime)) RevertSnapshot(); }
        private void OnApplicationFocus(bool focused) { if (!focused) RevertDisplay(); }
        private void OnDisable() { if (_captured) RevertDisplay(); }
        private void OnDestroy() { if (_captured && _touchedGraphics) Apply(_initial); }
    }
}
