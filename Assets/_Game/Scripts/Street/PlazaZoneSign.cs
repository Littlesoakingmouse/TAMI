using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>
    /// Biển mở một khu dọc đường dưới (ăn uống, trò chơi dân gian, biểu diễn). Chỉ hiện ở khu mở được tiếp theo;
    /// chạm vào thì nhân vật đi tới và mở bảng mở khu.
    /// </summary>
    public class PlazaZoneSign : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        public int Zone { get; private set; }

        const float SignScale = 1.3f;

        SpriteRenderer sr;
        TextMesh label;
        BoxCollider2D col;

        public static PlazaZoneSign Create(Transform parent, int zone, Vector2 position)
        {
            var g = Game.I;
            var root = new GameObject("ZoneSign_" + (zone + 1));
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var sign = root.AddComponent<PlazaZoneSign>();
            sign.Zone = zone;
            // Biển gỗ có chân; tên khu và giá viết ngay trên mặt biển.
            sign.sr = g.NewSprite(root.transform, g.art.expandSign, 0, "Art");
            sign.sr.transform.localScale = Vector3.one * SignScale;
            sign.label = g.NewLabel(root.transform, "", 3, 0.036f, Color.white, "Label");
            sign.label.fontStyle = FontStyle.Bold;
            sign.label.transform.localPosition = new Vector3(0f, 1.1f * SignScale, 0f);
            sign.col = root.AddComponent<BoxCollider2D>();
            sign.col.size = new Vector2(3.1f * SignScale, 2f * SignScale);
            sign.col.offset = new Vector2(0f, SignScale);
            root.AddComponent<YSort>().isStatic = true;
            sign.Refresh(false);
            return sign;
        }

        public void Refresh(bool visible)
        {
            sr.enabled = col.enabled = visible;
            label.gameObject.SetActive(visible);
            if (!visible) return;
            var street = Game.I.Street;
            string name = street.ZoneName(Zone).ToUpper();
            if (name.StartsWith("KHU ")) name = name.Substring(4);
            label.text = "MỞ KHU\n" + name + "\n" + NumberFormat.Short(street.ZoneCost(Zone)) + " xu";
        }

        public Vector3 InteractPoint => transform.position + new Vector3(0f, 1.6f, 0f);

        public void OnPointerClick(PointerEventData eventData) => Interaction.Request(this);

        public void Interact() => Game.I.UI.OpenPlazaZonePanel(Zone);
    }
}
