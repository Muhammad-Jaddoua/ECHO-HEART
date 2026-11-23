using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV115 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV115 _menu;
        public void Bind(FlowMenuV115 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
