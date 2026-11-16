using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>Nhận chạm, kéo, thả trên sân chơi mini-game; báo vị trí theo tỉ lệ khung (0..1 mỗi chiều).</summary>
    public class FolkInputArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public System.Action<Vector2> down, drag, up;

        RectTransform rect;

        void Awake() => rect = (RectTransform)transform;

        Vector2 Normalized(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var p);
            var r = rect.rect;
            return new Vector2((p.x - r.xMin) / r.width, (p.y - r.yMin) / r.height);
        }

        public void OnPointerDown(PointerEventData e) => down?.Invoke(Normalized(e));
        public void OnDrag(PointerEventData e) => drag?.Invoke(Normalized(e));
        public void OnPointerUp(PointerEventData e) => up?.Invoke(Normalized(e));
    }
}
