using UnityEngine;

namespace Tami
{
    /// <summary>Thông số theo từng cấp sạp hàng (4 cấp).</summary>
    [System.Serializable]
    public struct StallLevel
    {
        [Tooltip("Giá lên cấp này, tính theo bội số giá mở quầy")]
        public float upgradeCostMultiplier;
        [Tooltip("Hệ số giá bán mọi món ở cấp sạp này")]
        public float incomeMultiplier;
        public float serviceTimeMultiplier;
        [Tooltip("Sức chứa quỹ, tính theo số lần bán món đắt nhất")]
        public int vaultCapacitySales;
    }

    [System.Serializable]
    public class SegmentInfo
    {
        public string name;
        public double unlockCost;
        [Tooltip("Đoạn này có sân khấu thay cho quầy hàng")]
        public bool stageSegment;
        [Tooltip("Thoại khi mở đoạn phố")]
        public DialogueLine[] dialogue;
    }

    /// <summary>Cấu hình cân bằng và nội dung của toàn game.</summary>
    [CreateAssetMenu(menuName = "Tami/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public double startingCoins = 1500;
        public int startingGems;

        [Tooltip("Nâng cấp sạp hàng: 4 cấp")]
        public StallLevel[] levels;

        [Header("Món ăn: mỗi quầy 3 món, món 1 có sẵn khi dựng quầy")]
        [Tooltip("Giá bán từng món, bội số của tiền bán cơ bản của quầy")]
        public float[] dishPriceMultipliers = { 1f, 2.5f, 6f };
        [Tooltip("Giá mở từng món, bội số giá mở quầy (món 1 miễn phí)")]
        public float[] dishUnlockCostMultipliers = { 0f, 2f, 8f };

        [Header("Nâng cấp doanh thu")]
        public int revenueMaxLevel = 10;
        [Tooltip("Mỗi cấp tăng thêm bấy nhiêu phần giá bán mọi món (0,25 = +25%)")]
        public float revenuePerLevel = 0.25f;
        [Tooltip("Giá cấp 1, bội số giá mở quầy")]
        public float revenueCostMultiplier = 1.5f;
        [Tooltip("Mỗi cấp sau đắt gấp bấy nhiêu lần cấp trước")]
        public float revenueCostGrowth = 1.9f;

        [Header("Trang trí (đặt tự do trên gạch)")]
        [Tooltip("Số đồ trang trí tối đa cho mỗi đoạn phố đã mở")]
        public int decorPerSegment = 6;
        [Tooltip("Tỉ lệ tiền hoàn lại khi gỡ đồ trang trí")]
        [Range(0f, 1f)] public float decorRefund = 0.5f;

        [Header("Người chào hàng (đứng trước quầy mời khách)")]
        [Tooltip("Giá thuê, bội số giá mở quầy")]
        public float barkerCostMultiplier = 3f;
        [Tooltip("Xác suất khách vốn không định ghé vẫn được mời ghé quầy")]
        [Range(0f, 1f)] public float barkerInviteChance = 0.4f;
        [Tooltip("Khoảng giây giữa hai lần chào hàng")]
        public float barkerTalkInterval = 6f;

        public SegmentInfo[] segments;
        [Tooltip("Số đoạn phố mở được trong bản này (4 đoạn phố ăn uống); các đoạn sau hiện \"Cập nhật sau\"")]
        public int playableSegments = 4;
        [Tooltip("Giá mở từng khu dọc đường dưới, theo thứ tự: ghế ngồi, trò chơi dân gian, ghế ngồi, biểu diễn. " +
                 "Khu thứ i nằm dưới đoạn phố thứ i và chỉ mở được khi đoạn đó đã mở")]
        public double[] plazaZoneCosts = { 5000, 50000, 2e6, 25e6 };
        [Tooltip("Giá mở từng trò chơi dân gian (theo thứ tự: Đập niêu, Đá cầu, Tuho), mở lần lượt sau khi mở khu trò chơi")]
        public double[] folkGameCosts = { 10000, 60000, 300000 };
        public StallData[] stalls;
        public NpcTypeData[] npcTypes;
        public DecorationData[] decorations;
        public PerformerData[] performers;

        [Header("Lưu lượng khách (GDD mục 4.6)")]
        public float baseSpawnPerMinute = 12f;
        public int baseMaxCustomers = 8;
        public int customersPerSegment = 6;
        public int queueCapacity = 3;
        public float npcWalkSpeed = 1.9f;
        [Tooltip("Tỉ lệ kích thước khách so với hình gốc (để cân với nhân vật chính)")]
        public float npcScale = 1.15f;
        [Tooltip("Số giây khách chịu đợi trước khi bỏ đi")]
        public float npcPatience = 12f;

        [Header("Độ hấp dẫn (GDD mục 4.8)")]
        public int attractionPerStall = 2;
        public int attractionPerLevel = 1;
        public int attractionPerStage = 15;

        [Header("Khách đặc biệt")]
        public float bloggerBoost = 0.3f;
        public float bloggerBoostDuration = 120f;
        [Tooltip("Thưởng xu của khách bí ẩn = bội số này x tiền mỗi lần bán của quầy tốt nhất")]
        public float mysteryRewardSales = 20f;
        public double mysteryRewardMin = 50;
        [Range(0f, 1f)] public float mysteryGemChance = 0.3f;
        public int mysteryGems = 5;

        [Header("Khu ăn uống")]
        [Tooltip("Khả năng khách đang cầm món xuống bàn ăn ngồi ăn sau khi ghé hết các quầy")]
        [Range(0f, 1f)] public float dineChance = 0.35f;
        [Tooltip("Thời gian ngồi ăn (giây)")]
        public float eatSeconds = 7f;

        [Header("Trò chơi dân gian")]
        [Tooltip("Khả năng khách ghé chơi trò dân gian (khi không ngồi ăn)")]
        [Range(0f, 1f)] public float folkPlayChance = 0.3f;
        [Tooltip("Thời gian khách chơi (giây)")]
        public float folkPlaySeconds = 6f;
        [Tooltip("Phí khách trả mỗi lượt chơi = số phút thu nhập hiện tại của phố này (tối thiểu folkGameFeeMin)")]
        public float folkGameFeeMinutes = 0.1f;
        public double folkGameFeeMin = 20;
        [Tooltip("Người chơi chơi xong thì trò chơi nghỉ bấy nhiêu giây")]
        public float folkGameCooldown = 120f;
        [Tooltip("Thưởng mini-game khi đạt điểm tối đa = số phút thu nhập hiện tại này (tối thiểu folkRewardMin)")]
        public float folkRewardMinutes = 3f;
        public double folkRewardMin = 300;
        [Tooltip("Tăng thu nhập khi chơi giỏi (đạt ít nhất nửa số điểm) và khi đạt điểm tối đa")]
        public float folkBoostGood = 1.5f;
        public float folkBoostPerfect = 2f;
        public float folkBoostSeconds = 90f;

        [Header("Quản lý (GDD mục 4.7)")]
        public float managerCostMultiplier = 5f;
        public float managerCollectInterval = 10f;

        [Header("Thu nhập offline (GDD mục 4.11)")]
        public float offlineRate = 0.5f;
        public float offlineBaseHours = 4f;
        public float[] offlineUpgradeHours = { 8f, 12f };
        public int[] offlineUpgradeGems = { 100, 200 };

        [Header("Biểu diễn (GDD mục 4.9)")]
        public float performSpawnMultiplier = 2f;
        public float performMaxCustomersBonus = 0.5f;
        public float performCooldown = 600f;
        public int performResetGems = 10;

        [Header("Câu chuyện (GDD mục 2)")]
        public DialogueLine[] introLines;

        public StallData FindStall(string id)
        {
            foreach (var s in stalls) if (s != null && s.id == id) return s;
            return null;
        }

        public DecorationData FindDecoration(string id)
        {
            foreach (var d in decorations) if (d != null && d.id == id) return d;
            return null;
        }

        public PerformerData FindPerformer(string id)
        {
            foreach (var p in performers) if (p != null && p.id == id) return p;
            return null;
        }
    }
}
