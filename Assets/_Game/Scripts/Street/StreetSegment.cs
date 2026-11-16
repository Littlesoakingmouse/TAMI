using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Một đoạn phố đặt sẵn trong scene. Kéo thả các ô sạp, sân khấu bên trong để chỉnh bố cục
    /// (đồ trang trí do người chơi tự đặt trên gạch khi chơi).
    /// Khi chơi, đoạn chưa mở sẽ bị ẩn cho tới khi người chơi mở rộng phố.
    /// Tên, giá và thoại của đoạn nằm trong GameConfig.segments theo đúng thứ tự.
    /// </summary>
    public class StreetSegment : MonoBehaviour
    {
        [Tooltip("Chiều dài đoạn phố theo trục x, tính từ vị trí của object này")]
        public float width = 17.4f;

        [Tooltip("Ô đặt sạp, theo thứ tự khách đi qua (trái sang phải)")]
        public StallSlot[] slots = new StallSlot[0];
        public StageSpot[] stages = new StageSpot[0];

        public float LeftX => transform.position.x;
        public float RightX => transform.position.x + width;

        /// <summary>Gom lại các ô con theo vị trí x (dùng sau khi thêm/bớt ô trong scene).</summary>
        [ContextMenu("Tự tìm ô sạp, sân khấu bên trong")]
        public void CollectChildren()
        {
            slots = GetComponentsInChildren<StallSlot>(true);
            stages = GetComponentsInChildren<StageSpot>(true);
            System.Array.Sort(slots, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            System.Array.Sort(stages, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            var p = transform.position;
            Gizmos.DrawWireCube(new Vector3(p.x + width * 0.5f, p.y, 0f), new Vector3(width, 10f, 0f));
        }
    }
}
