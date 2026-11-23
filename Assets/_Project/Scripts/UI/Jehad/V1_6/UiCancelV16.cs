using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV16 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV16 _menu;
        public void Bind(FlowMenuV16 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
