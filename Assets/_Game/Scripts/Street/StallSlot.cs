using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>
    /// Ô đặt sạp (đặt sẵn trong scene, prefab Slot). Vị trí của object là chân sạp.
    /// Còn trống thì hiện dấu "+", bấm vào để chọn quầy.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class StallSlot : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        [Tooltip("Khoảng cách từ chân sạp xuống chỗ khách đứng mua")]
        public float approachOffset = 0.7f;
        [Tooltip("Khoảng cách giữa hai khách trong hàng chờ")]
        public float queueSpacing = 0.72f;

        public int Index { get; private set; }
        public Stall Stall { get; private set; }
        public float X => transform.position.x;
        public float Bottom => transform.position.y;
        public float ApproachY => Bottom - approachOffset;
        /// <summary>Chỗ nhân vật chính đứng để dựng quầy hoặc mở bảng quầy.</summary>
        public Vector3 InteractPoint => new Vector3(X, Game.I.Street.StandY, 0f);

        SpriteRenderer sr;
        BoxCollider2D col;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<BoxCollider2D>();
        }

        public void Init(int index) => Index = index;

        /// <summary>Chỗ đứng thứ <paramref name="queueIndex"/> trong hàng chờ trước quầy, 0 là sát quầy.</summary>
        public Vector2 QueuePoint(int queueIndex) => new Vector2(X, ApproachY - queueSpacing * queueIndex);

        public void SetStall(Stall stall)
        {
            Stall = stall;
            sr.enabled = false;
            col.enabled = false;
        }

        void Update()
        {
            if (Stall != null) return;
            var c = sr.color;
            c.a = 0.65f + 0.3f * Mathf.Sin(Time.time * 3f + Index);
            sr.color = c;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Stall != null) return;
            Interaction.Request(this);
        }

        public void Interact()
        {
            if (Stall == null) Game.I.UI.OpenBuildPanel(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 3; i++) Gizmos.DrawWireSphere(new Vector3(X, ApproachY - queueSpacing * i, 0f), 0.2f);
        }
    }
}
