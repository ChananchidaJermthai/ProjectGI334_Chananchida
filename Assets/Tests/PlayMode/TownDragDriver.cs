using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Colonization.Tests
{
    public static class TownDragDriver
    {
        public static IEnumerator Drag(Town.Resident resident, TownDropSlot destination, int frames=8, System.Action<Vector2> pointer=null, System.Action capture=null)
        {
            TownUnitDrag source=Object.FindObjectsByType<TownUnitDrag>().Single(d=>d.Resident==resident);
            var sourceRect=(RectTransform)source.transform;var destinationRect=(RectTransform)destination.transform;
            Canvas canvas=source.GetComponentInParent<Canvas>();Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Vector2 from=RectTransformUtility.WorldToScreenPoint(camera,sourceRect.TransformPoint(sourceRect.rect.center));
            Vector2 to=RectTransformUtility.WorldToScreenPoint(camera,destinationRect.TransformPoint(destinationRect.rect.center));
            var data=new PointerEventData(EventSystem.current) {pointerId=-1,button=PointerEventData.InputButton.Left,position=from,pressPosition=from,pointerDrag=source.gameObject,useDragThreshold=false};
            data.pointerPressRaycast=new RaycastResult {gameObject=source.gameObject,module=canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()};
            ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.beginDragHandler);
            for(int i=0;i<frames;i++)
            {
                float t=Mathf.SmoothStep(0,1,(i+1f)/frames);Vector2 next=Vector2.Lerp(from,to,t);data.delta=next-data.position;data.position=next;
                ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.dragHandler);pointer?.Invoke(next);capture?.Invoke();yield return null;
            }
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),$"Drop target {destination.name} at {to} must be raycastable; screen {Screen.width}x{Screen.height}, canvas scale {canvas.scaleFactor}.");
            data.pointerCurrentRaycast=hits[0];
            TownDropSlot hitSlot=hits[0].gameObject.GetComponentInParent<TownDropSlot>();
            Assert.That(hitSlot,Is.SameAs(destination),"The visible target must receive the drop, without hidden overlays intercepting it.");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.endDragHandler);
            yield return null;
            Assert.That(resident.Workplace,Is.SameAs(destination.Hex));
        }
    }
}
