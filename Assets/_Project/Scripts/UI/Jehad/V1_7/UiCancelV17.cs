using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV17 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV17 _menu;
        public void Bind(FlowMenuV17 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
