using UnityEngine;
using UnityEngine.EventSystems;

namespace Echoheart.UI
{
    /// <summary>
    /// Remembers which half of a value button the pointer pressed, so one button can step left or right.
    /// Keyboard/gamepad submit never sets it, so it keeps stepping forward as before.
    /// </summary>
    public sealed class UiSplitPressV117 : MonoBehaviour, IPointerDownHandler
    {
        private bool _leftPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            _leftPressed = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local)
                && local.x < rect.rect.center.x;
        }

        /// <summary>True once if the last press was on the left half; cleared so a later Enter goes forward.</summary>
        public bool ConsumeLeft()
        {
            bool left = _leftPressed;
            _leftPressed = false;
            return left;
        }
    }
}
