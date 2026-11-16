using UnityEngine;

namespace Tami
{
    /// <summary>Thứ nhân vật chính tương tác được khi đứng gần: ô sạp, quầy, túi tiền, sân khấu, biển mở rộng.</summary>
    public interface IInteractable
    {
        /// <summary>Chỗ nhân vật đứng để tương tác.</summary>
        Vector3 InteractPoint { get; }
        void Interact();
    }

    public static class Interaction
    {
        /// <summary>
        /// Người chơi chạm vào một vật: đứng đủ gần thì tương tác ngay, nếu xa thì nhân vật đi tới rồi mới tương tác.
        /// </summary>
        public static void Request(IInteractable target)
        {
            if (CameraScroller.DraggedThisPress || DecorMode.Active) return;
            var player = Game.I.Player;
            if (player != null) player.RequestInteract(target);
            else target.Interact();
        }
    }
}
