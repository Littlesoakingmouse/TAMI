using UnityEngine;

namespace Tami
{
    /// <summary>Dữ liệu một loại quầy (GDD mục 5.4).</summary>
    [CreateAssetMenu(menuName = "Tami/Stall Data", fileName = "Stall_")]
    public class StallData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Origin origin;

        [Tooltip("Đoạn phố (bắt đầu từ 1) cần mở trước khi dựng được quầy này")]
        public int segment = 1;

        public double unlockCost = 100;
        [Tooltip("Tiền bán món 1 ở sạp cấp 1")]
        public double baseIncome = 5;
        [Tooltip("Thời gian phục vụ một khách ở sạp cấp 1 (giây)")]
        public float baseServiceTime = 3f;

        [Tooltip("Tên 3 món; món 1 có sẵn, món 2 và 3 mở thêm")]
        public string[] products = new string[3];
        [Tooltip("Hình 3 món, hiện trên mặt quầy khi đã mở và bay lên đầu khách khi mua")]
        public Sprite[] dishSprites = new Sprite[3];

        [Tooltip("Hình sạp ở cấp 1, 2, 3, 4")]
        public Sprite[] levelSprites = new Sprite[4];
        [Tooltip("Độ cao mặt quầy so với chân sạp (đơn vị của prefab), nơi bày món và người bán đứng sau")]
        public float counterTopY = 1.6f;
        [Tooltip("Khoảng cách giữa các món bày trên mặt quầy")]
        public float dishSpacing = 0.72f;
        [Tooltip("Tỉ lệ hình món bày trên mặt quầy")]
        public float dishScale = 0.8f;

        [Tooltip("Bộ hình nhân viên của quầy (id trong GameArt.staffRigs), dùng chung cho người bán và người chào hàng")]
        public string staffRig;

        [Header("Lời thoại")]
        [Tooltip("Người giới thiệu quầy khi dựng: mayor, minji, ongba")]
        public string introSpeaker = "mayor";
        [TextArea] public string introLine;
        [Tooltip("Câu khách thỉnh thoảng nói khi mua (GDD mục 6.1)")]
        public string[] customerLines;
        [Tooltip("Câu người chào hàng mời khách (trống thì dùng câu chung)")]
        public string[] barkerLines;
    }
}
