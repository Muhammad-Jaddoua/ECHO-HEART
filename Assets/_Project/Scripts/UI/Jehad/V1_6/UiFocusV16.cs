using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoheart.UI
{
    public sealed class UiFocusV16 : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject _frame;
        private bool _selected, _hover;
        public bool MouseSelects { get; set; } = true;
        public void Bind(GameObject frame) { _frame = frame; Refresh(); }
        private void Refresh() { Selectable control=GetComponent<Selectable>();if (_frame != null) _frame.SetActive((_selected || _hover) && control!=null && control.IsInteractable()); }
        public void OnSelect(BaseEventData data) { _selected = true; Refresh(); }
        public void OnDeselect(BaseEventData data) { _selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData data) { _hover = true;Selectable control=GetComponent<Selectable>();if(MouseSelects&&control!=null&&control.IsInteractable()&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(gameObject);Refresh(); }
        public void OnPointerExit(PointerEventData data) { _hover = false; Refresh(); }
        private void OnDisable() { _selected = _hover = false; Refresh(); }
    }
}
