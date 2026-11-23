using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV18 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV18 _menu;
        public void Bind(FlowMenuV18 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
