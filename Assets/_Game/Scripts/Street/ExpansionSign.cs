using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>Tấm biển "Mở rộng phố" (đặt sẵn trong scene). Khi chơi tự dời tới cuối đoạn phố đã mở.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class ExpansionSign : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        public TextMesh label;

        SpriteRenderer sr;
        BoxCollider2D col;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<BoxCollider2D>();
        }

        public void Refresh()
        {
            var street = Game.I.Street;
            // Biển gỗ mới: đặt hình từ GameArt và đưa chữ vào giữa mặt biển.
            var art = Game.I.art.expandSign;
            if (art != null && sr.sprite != art)
            {
                sr.sprite = art;
                if (label != null)
                {
                    label.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                    label.color = Color.white;
                }
            }
            // Mở hết phần chơi được thì biển vẫn đứng đó, báo phần sau sẽ có trong bản cập nhật.
            bool show = street.CanExpand || street.ComingSoon;
            sr.enabled = show;
            col.enabled = show;
            if (label != null)
            {
                label.gameObject.SetActive(show);
                if (street.CanExpand) label.text = "MỞ RỘNG PHỐ\n" + NumberFormat.Short(street.NextSegment.unlockCost) + " xu";
                else if (show) label.text = "CẬP NHẬT SAU\nSắp ra mắt";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Game.I.Street.ComingSoon)
            {
                Game.I.UI.Toast(ComingSoonMessage);
                return;
            }
            Interaction.Request(this);
        }

        public const string ComingSoonMessage = "Phần tiếp theo của con phố sẽ có trong bản cập nhật sau. Hẹn gặp lại!";

        public Vector3 InteractPoint => new Vector3(transform.position.x - 1f, Game.I.Street.StandY, 0f);

        public void Interact()
        {
            Game.I.UI.OpenExpandPanel();
        }
    }
}
