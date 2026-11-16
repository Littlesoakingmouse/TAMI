using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Hướng dẫn 2 phút (GDD mục 8.3): dựng quầy, chờ khách, thu tiền, dựng quầy thứ hai, nâng cấp.
    /// Mỗi bước có một câu thoại và mũi tên chỉ vào mục tiêu.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public int Step { get; private set; } = -1;
        public bool Active => Step >= 0;

        SpriteRenderer arrow;
        Transform target;
        Vector3 targetOffset;
        bool sideways;
        float t;

        public void Begin()
        {
            var g = Game.I;
            if (g.Data.tutorialDone || Active) return;
            if (arrow == null)
            {
                arrow = g.NewSprite(transform, g.art.arrow, 31000, "TutorialArrow");
                arrow.enabled = false;
            }
            Enter(0);
        }

        void Enter(int step)
        {
            Step = step;
            switch (step)
            {
                case 0: Say("Đây là cháu, chủ hội chợ! Chạm xuống đường để đi, chạm vào ô có dấu + để đi tới và dựng quầy đầu tiên nhé!"); break;
                case 1: Say("Khách đang tới! Khi khách mua, tiền sẽ dồn vào túi trên mái quầy."); break;
                case 2: Say("Có tiền rồi! Đi tới gần quầy có túi tiền, cháu sẽ tự thu tiền về."); break;
                case 3: Say("Giỏi lắm! Giờ dựng thêm một quầy nữa ở ô bên cạnh."); break;
                case 4: Say("Mở thêm món để khách mua món đắt hơn. Đứng gần quầy, chạm vào quầy rồi chọn Mở món. Sau này còn nâng sạp, thuê người bán và tăng doanh thu nữa!"); break;
            }
        }

        static void Say(string text) => Game.I.UI.ShowDialogue(new[] { new DialogueLine("mayor", text) });

        void Finish()
        {
            Step = -1;
            target = null;
            arrow.enabled = false;
            Game.I.Data.tutorialDone = true;
            Game.I.UI.ShowDialogue(new[]
            {
                new DialogueLine("mayor", "Tuyệt vời! Từ giờ hãy mở thêm quầy và mở rộng con phố. Chúc cháu buôn may bán đắt!"),
            });
            Game.I.SaveNow();
        }

        void Update()
        {
            if (!Active) return;
            var g = Game.I;
            var street = g.Street;
            target = null;

            switch (Step)
            {
                case 0:
                    if (street.Stalls.Count >= 1) { Enter(1); break; }
                    Point(FirstEmptySlot(), new Vector3(0f, 2.3f), false);
                    break;
                case 1:
                    if (street.Stalls.Exists(s => s.Vault > 0)) Enter(2);
                    break;
                case 2:
                    if (g.Data.stats.coinsCollected > 0) { Enter(street.Stalls.Count >= 2 ? 4 : 3); break; }
                    var withMoney = street.Stalls.Find(s => s.Vault > 0);
                    // Túi tiền nằm sát mép trên màn hình nên mũi tên chỉ ngang từ bên trái.
                    if (withMoney != null) Point(withMoney.transform, new Vector3(-0.75f, 3.5f), true);
                    break;
                case 3:
                    if (street.Stalls.Count >= 2) { Enter(4); break; }
                    Point(FirstEmptySlot(), new Vector3(0f, 2.3f), false);
                    break;
                case 4:
                    if (street.Stalls.Exists(s => s.DishCount >= 2 || s.Level >= 2)) { Finish(); return; }
                    Stall cheapest = null;
                    foreach (var s in street.Stalls)
                    {
                        if (cheapest == null || s.NextDishCost < cheapest.NextDishCost) cheapest = s;
                    }
                    if (cheapest != null && g.Economy.CanAfford(cheapest.NextDishCost)) Point(cheapest.transform, new Vector3(0f, 2.2f), false);
                    break;
            }

            bool show = target != null && !g.UI.ModalOpen && !g.UI.DialogueOpen;
            arrow.enabled = show;
            if (!show) return;
            t += Time.unscaledDeltaTime;
            // Mũi tên vẽ chĩa xuống, mũi ở pivot; xoay 90 độ để chĩa sang phải.
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, sideways ? 90f : 0f);
            Vector3 back = sideways ? Vector3.left : Vector3.up;
            arrow.transform.position = target.position + targetOffset + back * (Mathf.Abs(Mathf.Sin(t * 4f)) * 0.35f);
        }

        void Point(Transform tr, Vector3 offset, bool pointRight)
        {
            target = tr;
            targetOffset = offset;
            sideways = pointRight;
        }

        static Transform FirstEmptySlot()
        {
            foreach (var slot in Game.I.Street.Slots) if (slot.Stall == null) return slot.transform;
            return null;
        }
    }
}
