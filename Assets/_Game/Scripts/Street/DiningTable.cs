using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Bàn ăn ở khu ăn uống bên dưới đường đi. Khách mua xong có thể ghé ngồi ăn món vừa mua, món đặt lên mặt
    /// bàn và vơi dần khi ăn. Hai kiểu:
    /// - Bàn phủ khăn: ghế đứng sau khách (lưng ghế lộ hai bên vai), khăn dài che phía trước, hợp mọi bộ đồ.
    /// - Góc ghế đẩu vỉa hè: bàn thấp, ghế đẩu hai bên, khách ngồi lộ chân (chỉ bộ có chân rời và hanbok).
    /// </summary>
    public class DiningTable : MonoBehaviour
    {
        /// <summary>Bàn phủ khăn: khách ngồi lùi sau mép bàn bấy nhiêu để YSort vẽ trọn người khách sau bàn.</summary>
        public const float SeatBehind = 0.4f;
        /// <summary>Ghế đẩu: khách ngồi nhích ra trước bàn một chút để vẽ đè lên mép bàn thấp.</summary>
        const float StoolSeatFront = -0.05f;
        /// <summary>Ghế lùi sau chỗ khách một chút để YSort vẽ ghế sau người.</summary>
        const float ChairBehind = 0.06f;

        /// <summary>Góc ghế đẩu vỉa hè: khách ngồi lộ chân (Npc gập chân, nâng người lên mặt ghế).</summary>
        public bool OnStools { get; private set; }

        float[] seatX;
        float seatDepth;
        Npc[] occupants;
        SpriteRenderer[] dishes;

        public int FreeSeats
        {
            get
            {
                int n = 0;
                foreach (var o in occupants) if (o == null) n++;
                return n;
            }
        }

        const float DishScale = 0.55f;

        /// <param name="chairs">Hình ghế cho từng chỗ ngồi, đặt ngay sau khách (null thì không có ghế).</param>
        /// <param name="seatX">Vị trí ngang các chỗ ngồi so với tâm bàn (đơn vị thế giới).</param>
        /// <param name="dishY">Độ cao mặt bàn để đặt món (đơn vị thế giới, tính từ chân bàn).</param>
        /// <param name="onStools">Góc ghế đẩu: khách ngồi hai bên bàn thấp thay vì sau bàn phủ khăn.</param>
        public static DiningTable Create(Transform parent, Sprite sprite, Sprite[] chairs, Vector2 position, float[] seatX, float dishY, bool onStools)
        {
            var g = Game.I;
            var sr = g.NewSprite(parent, sprite, 0, "DiningTable");
            sr.transform.position = position;
            var table = sr.gameObject.AddComponent<DiningTable>();
            table.seatX = seatX;
            table.OnStools = onStools;
            table.seatDepth = onStools ? StoolSeatFront : SeatBehind;
            table.occupants = new Npc[seatX.Length];
            table.dishes = new SpriteRenderer[seatX.Length];
            for (int i = 0; i < seatX.Length; i++)
            {
                // Món đặt trên mặt bàn trước chỗ ngồi (góc ghế đẩu: ở mép bàn phía khách), vẽ trên bàn
                // (tạo trước YSort để YSort gom luôn).
                var dish = g.NewSprite(sr.transform, null, 1, "Dish");
                dish.transform.localPosition = new Vector3(seatX[i] * (onStools ? 0.32f : 0.85f), dishY, 0f);
                dish.transform.localScale = Vector3.one * DishScale;
                dish.enabled = false;
                table.dishes[i] = dish;

                var chair = chairs != null && chairs.Length > 0 ? chairs[i % chairs.Length] : null;
                if (chair == null) continue;
                var c = g.NewSprite(parent, chair, 0, onStools ? "Stool" : "Chair");
                c.transform.position = table.SeatPoint(i) + new Vector2(0f, ChairBehind);
                c.gameObject.AddComponent<YSort>().isStatic = true;
            }
            sr.gameObject.AddComponent<YSort>().isStatic = true;
            return table;
        }

        /// <summary>Món trên bàn vơi dần theo tiến độ ăn (0 vừa đặt, 1 ăn xong).</summary>
        public void SetDishProgress(int seat, float eaten)
        {
            if (seat < 0 || seat >= dishes.Length) return;
            dishes[seat].transform.localScale = Vector3.one * (DishScale * Mathf.Lerp(1f, 0.55f, eaten));
        }

        public bool TryReserve(Npc npc, out int seat)
        {
            for (seat = 0; seat < occupants.Length; seat++)
            {
                if (occupants[seat] != null) continue;
                occupants[seat] = npc;
                return true;
            }
            seat = -1;
            return false;
        }

        /// <summary>Chỗ khách đứng (chân) khi ngồi vào ghế thứ <paramref name="seat"/>.</summary>
        public Vector2 SeatPoint(int seat) => (Vector2)transform.position + new Vector2(seatX[seat], seatDepth);

        public void ShowDish(int seat, Sprite sprite)
        {
            dishes[seat].sprite = sprite;
            dishes[seat].enabled = sprite != null;
            SetDishProgress(seat, 0f);
        }

        public void Free(int seat)
        {
            if (seat < 0 || seat >= occupants.Length) return;
            occupants[seat] = null;
            dishes[seat].enabled = false;
        }
    }
}
