using UnityEngine;
using UnityEngine.EventSystems;

namespace Colonization
{
    public sealed class TownDropSlot : MonoBehaviour, IDropHandler
    {
        public Hex Hex { get; private set; }
        private TownView view;
        public void Initialize(TownView owner, Hex hex) { view = owner; Hex = hex; }
        public void OnDrop(PointerEventData eventData)
        {
            if(eventData.pointerDrag == null) return;
            TownUnitDrag drag = eventData.pointerDrag.GetComponent<TownUnitDrag>();
            if(drag != null) view.Assign(drag.Resident, Hex);
        }
    }
}
