using UnityEngine;
using UnityEngine.EventSystems;

namespace Colonization
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TownUnitDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Town.Resident Resident { get; private set; }
        private TownView view;
        private RectTransform rect;
        private CanvasGroup group;
        private Transform originalParent;
        private Vector2 originalPosition;
        private Vector3 pointerOffset;
        public void Initialize(TownView owner, Town.Resident resident)
        { view = owner; Resident = resident; rect = GetComponent<RectTransform>(); group = GetComponent<CanvasGroup>(); }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            originalParent = rect.parent; originalPosition = rect.anchoredPosition;
            rect.SetParent(view.DragLayer, true); rect.SetAsLastSibling();
            RectTransformUtility.ScreenPointToWorldPointInRectangle(view.DragLayer, eventData.position, eventData.pressEventCamera, out Vector3 point);
            pointerOffset = rect.position - point; group.blocksRaycasts = false; group.alpha = .85f;
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (originalParent == null) return;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(view.DragLayer, eventData.position, eventData.pressEventCamera, out Vector3 point);
            rect.position = point + pointerOffset;
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null) return;
            rect.SetParent(originalParent, true); rect.anchoredPosition = originalPosition;
            group.blocksRaycasts = true; group.alpha = 1; originalParent = null;
        }
    }
}
