using System;
using Echoheart.Core;

namespace Echoheart.UI
{
    public enum UiPageV16 { Home, Settings, Controls, Credits }
    public enum UiModalV16 { None, Retry, Title, Quit, Defaults, Display }
    public enum UiBackV16 { None, Pause, Resume, Page, Modal, RevertDisplay }

    /// <summary>UI-only routing. Gameplay state remains owned by SessionFlowV13.</summary>
    public sealed class UiPolicyV16
    {
        public GameState State { get; private set; } = GameState.Title;
        public UiPageV16 Page { get; private set; }
        public UiModalV16 Modal { get; private set; }
        public void SetState(GameState state) { State = state; Page = UiPageV16.Home; Modal = UiModalV16.None; }
        public bool Open(UiPageV16 page)
        {
            if (State == GameState.Playing || Modal != UiModalV16.None) return false;
            Page = page; return true;
        }
        public bool Confirm(UiModalV16 modal)
        {
            if (State == GameState.Playing || Modal != UiModalV16.None || modal == UiModalV16.None) return false;
            Modal = modal; return true;
        }
        public bool Consume(UiModalV16 expected)
        {
            if (Modal == UiModalV16.None || Modal != expected) return false;
            Modal = UiModalV16.None; return true;
        }
        public UiBackV16 Back()
        {
            if (Modal != UiModalV16.None) { bool display = Modal == UiModalV16.Display; Modal = UiModalV16.None; return display ? UiBackV16.RevertDisplay : UiBackV16.Modal; }
            if (Page != UiPageV16.Home) { Page = UiPageV16.Home; return UiBackV16.Page; }
            return State == GameState.Playing ? UiBackV16.Pause : State == GameState.Paused ? UiBackV16.Resume : UiBackV16.None;
        }
    }
    public struct UiValuesV16
    {
        public float Music, Sfx, HudScale;
        public bool HighContrast, ReduceMotion, Hints, Crosshair;
        public static UiValuesV16 Defaults => new UiValuesV16 { Music = .65f, Sfx = .8f, HudScale = 1f, Hints = true, Crosshair = true };
        private static float Safe(float value, float fallback, float low, float high) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(low, Math.Min(high, value));
        public UiValuesV16 Sanitized()
        {
            var value = this; value.Music = Safe(Music, .65f, 0f, 1f); value.Sfx = Safe(Sfx, .8f, 0f, 1f); value.HudScale = Safe(HudScale, 1f, .85f, 1.15f); return value;
        }
    }
    public sealed class UiDisplayTimerV16
    {
        public bool Active { get; private set; }
        public float Remaining { get; private set; }
        public bool Begin() { if (Active) return false; Active = true; Remaining = 15f; return true; }
        public bool Accept() { if (!Active) return false; Active = false; Remaining = 0f; return true; }
        public bool Tick(float seconds)
        {
            if (!Active || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            Remaining = Math.Max(0f, Remaining - seconds); if (Remaining > 0f) return false; Active = false; return true;
        }
    }
}
