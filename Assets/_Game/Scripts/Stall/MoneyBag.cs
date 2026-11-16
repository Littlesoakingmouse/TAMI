using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Tami
{
    /// <summary>Icon túi tiền trên mái quầy: nhân vật chính đi tới gần để thu (GDD mục 4.3).</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class MoneyBag : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        Stall stall;
        SpriteRenderer sr;
        CircleCollider2D col;
        Vector3 basePos;
        float t;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<CircleCollider2D>();
            basePos = transform.localPosition;
            // Túi nhún mỗi khung: collider gắn Rigidbody kinematic chỉ dời vị trí, không phải dựng lại như collider tĩnh.
            if (!TryGetComponent<Rigidbody2D>(out _)) gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        }

        public void Init(Stall owner) => stall = owner;

        /// <summary>Đặt độ cao túi tiền (theo nóc sạp của cấp hiện tại).</summary>
        public void SetBaseY(float y)
        {
            basePos.y = y;
            transform.localPosition = basePos;
        }

        public void Refresh()
        {
            if (stall == null) return;
            bool show = stall.Vault > 0;
            sr.enabled = show;
            col.enabled = show;
            if (!show) return;

            float fill = (float)(stall.Vault / stall.VaultCapacity);
            bool full = stall.VaultFull;
            float scale = full ? 1.15f : fill >= 1f / 3f ? 0.95f : 0.75f;
            transform.localScale = Vector3.one * scale;
            // Đầy quỹ thì đổi sang túi đỏ (thiếu hình thì nhuộm đỏ túi thường như trước).
            var art = Game.I.art;
            bool fullArt = full && art.moneyBagFull != null;
            sr.sprite = fullArt ? art.moneyBagFull : art.moneyBag;
            sr.color = full && !fullArt ? new Color(1f, 0.45f, 0.42f) : Color.white;
        }

        void Update()
        {
            if (stall == null || !sr.enabled) return;
            t += Time.deltaTime;
            float shake = stall.VaultFull ? Mathf.Sin(t * 38f) * 0.05f : 0f;
            transform.localPosition = basePos + new Vector3(shake, Mathf.Sin(t * 3f) * 0.08f, 0f);
        }

        /// <summary>Chạm túi tiền: nhân vật đi tới quầy rồi thu (đi ngang qua cũng tự thu).</summary>
        public void OnPointerClick(PointerEventData eventData) => Interaction.Request(this);

        public Vector3 InteractPoint => stall.InteractPoint;

        public void Interact() => stall.Collect();
    }
}
