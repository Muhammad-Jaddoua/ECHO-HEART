using System;
using System.Collections.Generic;
using Echoheart.Core;

namespace Echoheart.UI
{
    /// <summary>Remembers a subpage's origin within one game-state visit; stale controls are rejected on recall.</summary>
    public sealed class UiFocusHistoryV18<T> where T : class
    {
        private readonly Dictionary<UiPageV17,T> _pages = new Dictionary<UiPageV17,T>();
        private GameState _state = GameState.Title;
        public void EnterState(GameState state)
        {
            if (_state == state) return;
            _pages.Clear(); _state = state;
        }
        public void Remember(UiPageV17 page,T control) { if (control != null) _pages[page] = control; }
        public void Forget(UiPageV17 page) { _pages.Remove(page); }
        public T Recall(UiPageV17 page,Func<T,bool> usable)
        {
            if (!_pages.TryGetValue(page,out T control)) return null;
            if (usable != null && usable(control)) return control;
            _pages.Remove(page); return null;
        }
    }
}
