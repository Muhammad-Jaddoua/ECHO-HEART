using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV19 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV19 _menu;
        public void Bind(FlowMenuV19 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
