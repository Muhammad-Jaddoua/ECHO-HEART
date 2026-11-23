using System;
using Echoheart.Core;

namespace Echoheart.UI
{
    public enum UiPageV17 { Home, Settings, Controls, Credits }
    public enum UiModalV17 { None, Retry, Title, Quit, Defaults, Display, DiscardDisplay }
    public enum UiBackV17 { None, Pause, Resume, Page, Modal, RevertDisplay }

    /// <summary>UI-only routing. Gameplay state remains owned by SessionFlowV13.</summary>
    public sealed class UiPolicyV17
    {
        public GameState State { get; private set; } = GameState.Title;
        public UiPageV17 Page { get; private set; }
        public UiModalV17 Modal { get; private set; }
        public void SetState(GameState state) { State = state; Page = UiPageV17.Home; Modal = UiModalV17.None; }
        public bool Open(UiPageV17 page)
        {
            if (State == GameState.Playing || Modal != UiModalV17.None) return false;
            Page = page; return true;
        }
        public bool Confirm(UiModalV17 modal)
        {
            if (State == GameState.Playing || Modal != UiModalV17.None || modal == UiModalV17.None) return false;
            Modal = modal; return true;
        }
        public bool Consume(UiModalV17 expected)
        {
            if (Modal == UiModalV17.None || Modal != expected) return false;
            Modal = UiModalV17.None; return true;
        }
        public UiBackV17 Back()
        {
            if (Modal != UiModalV17.None) { bool display = Modal == UiModalV17.Display; Modal = UiModalV17.None; return display ? UiBackV17.RevertDisplay : UiBackV17.Modal; }
            if (Page != UiPageV17.Home) { Page = UiPageV17.Home; return UiBackV17.Page; }
            return State == GameState.Playing ? UiBackV17.Pause : State == GameState.Paused ? UiBackV17.Resume : UiBackV17.None;
        }
    }
}
