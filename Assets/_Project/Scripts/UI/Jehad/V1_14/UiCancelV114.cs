using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    public sealed class UiCancelV114 : MonoBehaviour, ICancelHandler
    {
        private FlowMenuV114 _menu;
        public void Bind(FlowMenuV114 menu) => _menu = menu;
        public void OnCancel(BaseEventData data) { if (_menu != null) _menu.Back(); data.Use(); }
    }
}
