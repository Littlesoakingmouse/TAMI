using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>Chạm vào nền (gạch, đường, cỏ) để nhân vật chính đi tới chỗ đó. Gắn trên object Ground của con phố.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class GroundClick : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (CameraScroller.DraggedThisPress || DecorMode.Active) return;
            var g = Game.I;
            if (g == null || g.Player == null) return;
            Vector3 world = g.Cam.ScreenToWorldPoint(eventData.position);
            g.Player.MoveTo(world);
        }
    }
}
