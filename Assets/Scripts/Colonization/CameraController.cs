using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Colonization
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        private Camera view;
        public Vector2 MapSize { get; set; } = new Vector2(64, 38);
        public bool Locked { get; set; }
        private void Awake() => view = GetComponent<Camera>();
        private void Update()
        {
            if (Locked) return;
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            Vector3 delta = Vector3.zero;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) delta.x--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) delta.x++;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) delta.y++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) delta.y--;
            }
            transform.position += delta.normalized * (view.orthographicSize * 1.6f * Time.unscaledDeltaTime);
            if (mouse != null && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                view.orthographicSize = Mathf.Clamp(view.orthographicSize - mouse.scroll.ReadValue().y * .015f, 4, 22);
                if (mouse.middleButton.isPressed)
                {
                    Vector2 drag = mouse.delta.ReadValue() * (view.orthographicSize * 2 / Screen.height);
                    transform.position -= new Vector3(drag.x, drag.y, 0);
                }
            }
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -3, MapSize.x + 3); p.y = Mathf.Clamp(p.y, -3, MapSize.y + 3);
            transform.position = p;
        }
        public void Focus(Vector3 position) => transform.position = new Vector3(position.x, position.y, -10);
    }
}
