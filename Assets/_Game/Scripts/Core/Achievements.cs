using System.Collections.Generic;
using UnityEngine;

namespace Tami
{
    public enum AchStat { TotalSales, StallSales, StallsBuilt, Level3Stalls, SpecialGuests, Segments, CoinsCollected, Decorations, Performances, Managers }

    public class AchievementDef
    {
        public string id;
        public string title;
        public AchStat stat;
        public string param;
        public double target;
        public int gems;
    }

    /// <summary>Thành tựu theo 3 nhóm bán hàng, xây dựng, khách; thưởng Ngọc (GDD mục 4.10).</summary>
    public class Achievements
    {
        public static readonly AchievementDef[] All =
        {
            A("sell_100", "Bán 100 lần", AchStat.TotalSales, 100, 5),
            A("sell_1k", "Bán 1.000 lần", AchStat.TotalSales, 1000, 10),
            A("sell_10k", "Bán 10.000 lần", AchStat.TotalSales, 10000, 25),
            A("pho_1000", "Bán 1.000 bát phở", AchStat.StallSales, 1000, 20, "pho"),
            A("banh_mi_500", "Bán 500 ổ bánh mì", AchStat.StallSales, 500, 10, "banh_mi"),
            A("tteok_500", "Bán 500 phần tteokbokki", AchStat.StallSales, 500, 10, "tteokbokki"),
            A("stalls_4", "Mở 4 quầy", AchStat.StallsBuilt, 4, 5),
            A("stalls_8", "Mở 8 quầy", AchStat.StallsBuilt, 8, 10),
            A("stalls_16", "Mở đủ 12 quầy ẩm thực", AchStat.StallsBuilt, 12, 30),
            A("lv3_1", "Nâng một quầy lên cấp 3", AchStat.Level3Stalls, 1, 10),
            A("lv3_16", "12 quầy đạt sạp cấp 3", AchStat.Level3Stalls, 12, 50),
            A("managers_4", "Thuê 4 quản lý", AchStat.Managers, 4, 10),
            A("guests_10", "Đón 10 khách đặc biệt", AchStat.SpecialGuests, 10, 10),
            A("guests_50", "Đón 50 khách đặc biệt", AchStat.SpecialGuests, 50, 25),
            // Bản này mở được 4 đoạn phố ăn uống (GameConfig.playableSegments); quảng trường sân khấu để cập nhật sau.
            A("segments_4", "Mở đủ 4 đoạn phố", AchStat.Segments, 4, 30),
            A("collect_1m", "Thu tổng 1M xu", AchStat.CoinsCollected, 1e6, 10),
            A("collect_1b", "Thu tổng 1B xu", AchStat.CoinsCollected, 1e9, 30),
            A("decor_5", "Đặt 5 món trang trí", AchStat.Decorations, 5, 10),
            A("decor_12", "Đặt đủ 12 món trang trí", AchStat.Decorations, 12, 25),
        };

        static AchievementDef A(string id, string title, AchStat stat, double target, int gems, string param = null)
        {
            return new AchievementDef { id = id, title = title, stat = stat, target = target, gems = gems, param = param };
        }

        readonly HashSet<string> notified = new HashSet<string>();

        public void Init()
        {
            notified.Clear();
            foreach (var a in All) if (IsComplete(a)) notified.Add(a.id);
        }

        public double Progress(AchievementDef a)
        {
            var g = Game.I;
            var stats = g.Data.stats;
            switch (a.stat)
            {
                case AchStat.TotalSales: return stats.totalSales;
                case AchStat.StallSales: return stats.SalesOf(a.param);
                case AchStat.StallsBuilt: return g.Street.Stalls.Count;
                case AchStat.Level3Stalls: return g.Street.Stalls.FindAll(s => s.Level >= 3).Count;
                case AchStat.SpecialGuests: return stats.specialGuests;
                case AchStat.Segments: return g.Street.UnlockedSegments;
                case AchStat.CoinsCollected: return stats.coinsCollected;
                case AchStat.Decorations: return g.Street.DecorationCount;
                case AchStat.Performances: return stats.performances;
                case AchStat.Managers: return g.Street.Stalls.FindAll(s => s.HasManager).Count;
                default: return 0;
            }
        }

        public bool IsComplete(AchievementDef a) => Progress(a) >= a.target;
        public bool IsClaimed(AchievementDef a) => Game.I.Data.claimedAchievements.Contains(a.id);

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var a in All) if (IsComplete(a) && !IsClaimed(a)) n++;
                return n;
            }
        }

        public bool Claim(AchievementDef a)
        {
            if (!IsComplete(a) || IsClaimed(a)) return false;
            Game.I.Data.claimedAchievements.Add(a.id);
            Game.I.Economy.AddGems(a.gems);
            Game.I.Audio.Play(Sfx.Chime);
            return true;
        }

        /// <summary>Báo khi có thành tựu mới hoàn thành.</summary>
        public void Check()
        {
            foreach (var a in All)
            {
                if (notified.Contains(a.id) || !IsComplete(a)) continue;
                notified.Add(a.id);
                Game.I.UI.Toast("Thành tựu mới: " + a.title + " (+" + a.gems + " Ngọc)");
                Game.I.Audio.Play(Sfx.Chime);
            }
        }
    }
}
