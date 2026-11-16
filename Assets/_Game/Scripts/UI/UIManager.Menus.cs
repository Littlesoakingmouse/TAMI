using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>Các bảng mở từ thanh tab và nút trên HUD: quản lý, trang trí, cửa hàng, thành tựu, cài đặt, offline.</summary>
    public partial class UIManager
    {
        public double PendingOfflineCoins { get; private set; }
        public bool OfflinePanelOpen => modal != null && PendingOfflineCoins > 0;

        public void OpenManagersPanel()
        {
            var g = Game.I;
            var content = OpenModal("Người bán và sạp hàng", 1400f);
            var hint = Label(content, "Người bán đứng sau quầy, tự thu tiền mỗi " + Mathf.RoundToInt(g.config.managerCollectInterval)
                + " giây và kiếm tiền cả khi bạn offline. Món ăn và doanh thu nâng trong bảng của từng quầy.", 30, Muted, TextAnchor.MiddleLeft);
            Height(hint, 90f);

            var list = ScrollList(content, 1000f);
            var rows = new List<(Stall stall, Text info, Button hire, Button upgrade)>();
            foreach (var s in g.Street.Stalls)
            {
                var stall = s;
                var row = Row(list, 150f);
                var info = Label(row, "", 34, Ink, TextAnchor.MiddleLeft);
                Stretch(info.rectTransform, Vector2.zero, new Vector2(0.44f, 1f), new Vector2(24f, 0f), Vector2.zero);
                var hire = Btn(row, "", GemBlue, () =>
                {
                    if (!stall.HireManager()) NotEnough(stall.HasManager ? "Quầy đã có người bán" : "Chưa đủ xu để thuê người bán");
                }, 30);
                Stretch((RectTransform)hire.transform, new Vector2(0.45f, 0f), new Vector2(0.72f, 1f), new Vector2(0f, 16f), new Vector2(-8f, -16f));
                var upgrade = Btn(row, "", Ok, () =>
                {
                    if (!stall.TryUpgrade()) NotEnough(stall.IsMaxLevel ? "Sạp đã đạt cấp tối đa" : "Chưa đủ xu để nâng sạp");
                }, 30);
                Stretch((RectTransform)upgrade.transform, new Vector2(0.73f, 0f), Vector2.one, new Vector2(0f, 16f), new Vector2(-16f, -16f));
                rows.Add((stall, info, hire, upgrade));
            }

            if (rows.Count == 0) Height(Label(list, "Chưa có quầy nào. Hãy dựng quầy đầu tiên!", 36, Muted, TextAnchor.MiddleCenter), 200f);

            refreshModal = () =>
            {
                bool token = g.Data.managerTokens > 0;
                foreach (var (stall, info, hire, upgrade) in rows)
                {
                    info.text = "<b>" + stall.Data.displayName + "</b>\n<size=28>Sạp " + stall.Level + "/" + stall.MaxLevel
                        + " · " + stall.DishCount + " món" + (stall.HasManager ? " · <color=#3D8ED8>có người bán</color>" : "") + "</size>";
                    SetText(hire, stall.HasManager ? "Đã có\nngười bán" : token ? "Thuê\nmiễn phí" : "Thuê người bán\n" + NumberFormat.Short(stall.ManagerCost) + " xu");
                    Look(hire, !stall.HasManager && (token || g.Economy.CanAfford(stall.ManagerCost)), GemBlue);
                    SetText(upgrade, stall.IsMaxLevel ? "Sạp tối đa" : "Nâng sạp\n" + NumberFormat.Short(stall.NextUpgradeCost) + " xu");
                    Look(upgrade, !stall.IsMaxLevel && g.Economy.CanAfford(stall.NextUpgradeCost), Ok);
                }
            };
            refreshModal();
        }

        /// <summary>Thông số khu phố (nút xanh bên phải): khách, thu nhập, độ hấp dẫn, tiến độ con phố.</summary>
        public void OpenStatsPanel()
        {
            var g = Game.I;
            var street = g.Street;
            var spawner = g.Spawner;
            var stats = g.Data.stats;
            var content = OpenModal("Thông số khu phố", 1100f);
            var list = ScrollList(content, 640f);

            var rows = new List<(Text value, System.Func<string> read)>();
            void Stat(string title, System.Func<string> read)
            {
                var row = Row(list, 84f);
                var label = Label(row, title, 34, Muted, TextAnchor.MiddleLeft);
                Stretch(label.rectTransform, Vector2.zero, new Vector2(0.55f, 1f), new Vector2(28f, 0f), Vector2.zero);
                var value = Label(row, "", 36, Ink, TextAnchor.MiddleRight);
                value.fontStyle = FontStyle.Bold;
                Stretch(value.rectTransform, new Vector2(0.55f, 0f), Vector2.one, Vector2.zero, new Vector2(-28f, 0f));
                rows.Add((value, read));
            }

            Stat("Khách đang trong phố", () => spawner.ActiveCount + " / " + street.MaxCustomers);
            Stat("Khách đến mỗi phút", () => spawner.SpawnPerMinute.ToString("0.#")
                + (spawner.BloggerBoostActive ? "  <color=#FF6FB5>(Blogger +" + Mathf.RoundToInt(g.config.bloggerBoost * 100f) + "%)</color>" : ""));
            Stat("Độ hấp dẫn", () => street.Attraction + "  <size=28><color=#A07F75>(" + street.DecorationCount + " món trang trí)</color></size>");
            Stat("Thu nhập hiện tại", () =>
            {
                double perSecond = 0;
                foreach (var s in street.Stalls) perSecond += s.IncomeRate;
                return NumberFormat.Short(perSecond * 60) + " xu/phút";
            });
            Stat("Tổng xu đã thu", () => NumberFormat.Short(stats.coinsCollected) + " xu");
            Stat("Tổng lượt bán", () => NumberFormat.Short(stats.totalSales));
            Stat("Quầy đang mở", () => street.Stalls.Count + " / " + street.Slots.Count);
            Stat("Đoạn phố đã mở", () => street.UnlockedSegments + " / " + street.PlayableSegments);
            Stat("Khách đặc biệt đã đón", () => stats.specialGuests.ToString());
            Stat("Khách đã ngồi ăn", () => NumberFormat.Short(stats.mealsEaten));
            Stat("Lượt khách chơi trò dân gian", () => NumberFormat.Short(stats.npcGamesPlayed));
            Stat("Lượt bạn chơi trò dân gian", () => stats.folkGamesPlayed.ToString());
            Stat("Tăng thu nhập", () => g.IncomeBoostActive
                ? "x" + g.IncomeBoostMultiplier.ToString("0.#") + " · còn " + NumberFormat.Clock(g.IncomeBoostUntil - Time.time)
                : "<color=#A07F75>Chưa có</color>");
            Stat("Thời gian chơi", () => NumberFormat.Duration(g.Data.playSeconds));

            refreshModal = () =>
            {
                foreach (var (value, read) in rows) value.text = read();
            };
            refreshModal();
        }

        /// <summary>Hàng nâng giới hạn thu nhập offline bằng Ngọc (nằm trong bảng Cài đặt).</summary>
        System.Action OfflineUpgradeRow(RectTransform list)
        {
            var g = Game.I;
            var cfg = g.config;
            var offline = ShopRow(list, "Giới hạn offline", "", GemBlue, () =>
            {
                int level = g.Data.offlineUpgradeLevel;
                if (level >= cfg.offlineUpgradeGems.Length) return;
                if (!g.Economy.TrySpendGems(cfg.offlineUpgradeGems[level])) { NotEnough("Chưa đủ Ngọc"); return; }
                g.Data.offlineUpgradeLevel++;
                Toast("Giới hạn offline tăng lên " + g.OfflineCapHours + " giờ");
                g.Audio.Play(Sfx.Chime);
            });
            var offlineButton = offline.GetComponentInChildren<Button>();
            var offlineText = offline.GetComponentInChildren<Text>();
            return () =>
            {
                int level = g.Data.offlineUpgradeLevel;
                bool maxed = level >= cfg.offlineUpgradeGems.Length;
                offlineText.text = "<b>Giới hạn offline</b>\n<size=28>Hiện tại " + g.OfflineCapHours + " giờ"
                    + (maxed ? " (tối đa)" : ", nâng lên " + cfg.offlineUpgradeHours[level] + " giờ") + "</size>";
                SetText(offlineButton, maxed ? "Tối đa" : cfg.offlineUpgradeGems[level] + " Ngọc");
                Look(offlineButton, !maxed && g.Economy.CanAffordGems(cfg.offlineUpgradeGems[level]), GemBlue);
            };
        }

        RectTransform ShopRow(RectTransform list, string title, string description, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var row = Row(list, 140f);
            var info = Label(row, "<b>" + title + "</b>\n<size=28>" + description + "</size>", 34, Ink, TextAnchor.MiddleLeft);
            Stretch(info.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(24f, 0f), Vector2.zero);
            var button = Btn(row, "", color, onClick, 32);
            Stretch((RectTransform)button.transform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0f, 16f), new Vector2(-16f, -16f));
            return row;
        }

        public void OpenAchievementsPanel()
        {
            var g = Game.I;
            var content = OpenModal("Thành tựu", 1450f);
            var list = ScrollList(content, 1200f);
            var rows = new List<(AchievementDef def, Text info, Button button)>();
            foreach (var a in Achievements.All)
            {
                var def = a;
                var row = Row(list, 130f);
                var info = Label(row, "", 34, Ink, TextAnchor.MiddleLeft);
                Stretch(info.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(24f, 0f), Vector2.zero);
                var button = Btn(row, "", Gold, () =>
                {
                    if (g.Achievements.Claim(def)) Toast("Nhận " + def.gems + " Ngọc!");
                }, 30);
                Stretch((RectTransform)button.transform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0f, 16f), new Vector2(-16f, -16f));
                rows.Add((a, info, button));
            }

            refreshModal = () =>
            {
                foreach (var (def, info, button) in rows)
                {
                    double progress = System.Math.Min(g.Achievements.Progress(def), def.target);
                    bool done = g.Achievements.IsComplete(def);
                    bool claimed = g.Achievements.IsClaimed(def);
                    info.text = "<b>" + def.title + "</b>\n<size=28>" + NumberFormat.Short(progress) + " / " + NumberFormat.Short(def.target)
                        + " · thưởng " + def.gems + " Ngọc</size>";
                    SetText(button, claimed ? "Đã nhận" : done ? "Nhận" : "Chưa xong");
                    Look(button, done && !claimed, Gold);
                }
            };
            refreshModal();
        }

        // Tab đang mở của bảng Cài đặt (giữ khi đóng rồi mở lại).
        int settingsTab;

        public void OpenSettingsPanel() => OpenSettingsPanel(settingsTab);

        /// <summary>Bảng Cài đặt chia tab: Âm thanh, Chung và (bản Development) Thử nghiệm.</summary>
        void OpenSettingsPanel(int tab)
        {
            var g = Game.I;
            var s = g.Data.settings;
            string[] titles = Debug.isDebugBuild ? new[] { "Âm thanh", "Chung", "Thử nghiệm" } : new[] { "Âm thanh", "Chung" };
            // Mỗi tab một màu theo chức năng; tab đang mở tô màu, tab khác xám.
            Color[] tabColors = { Lavender, Ok, Gold };
            settingsTab = tab = Mathf.Clamp(tab, 0, titles.Length - 1);
            var content = OpenModal("Cài đặt", 1100f);

            var tabBar = HLayout(content, 90f, 12f);
            for (int i = 0; i < titles.Length; i++)
            {
                int index = i;
                Btn(tabBar, titles[i], i == tab ? tabColors[i] : Off, () => { if (index != settingsTab) OpenSettingsPanel(index); }, 34);
            }
            var list = ScrollList(content, 560f);
            System.Action refresh = null;

            switch (tab)
            {
                case 0:
                    SliderRow(list, "Âm lượng tổng", s.masterVolume, v => { s.masterVolume = v; g.Audio.Apply(); }, Lavender);
                    SliderRow(list, "Nhạc nền", s.musicVolume, v => { s.musicVolume = v; g.Audio.Apply(); }, GemBlue);
                    SliderRow(list, "Hiệu ứng", s.effectVolume, v => s.effectVolume = v, Peach);
                    SliderRow(list, "Tiếng xu", s.coinVolume, v => s.coinVolume = v, Gold);
                    SliderRow(list, "Giao diện, thông báo", s.uiVolume, v => s.uiVolume = v, Ok);
                    var hint = Label(list, "Hiệu ứng: dựng quầy, nâng cấp, chiêng. Giao diện: bấm nút, thông báo, báo lỗi.", 28, Muted, TextAnchor.MiddleLeft);
                    Height(hint, 70f);
                    break;

                case 1:
                    var vibration = Btn(list, "", Ok, () => s.vibration = !s.vibration);
                    Height(vibration, 100f);
                    var refreshOffline = OfflineUpgradeRow(list);
                    var reset = Btn(list, "Xoá dữ liệu, chơi lại từ đầu", CloseColor, () =>
                        Confirm("Xoá dữ liệu?", "Toàn bộ tiến trình sẽ bị xoá và không khôi phục được.", "Xoá", () => g.ResetProgress()));
                    Height(reset, 100f);
                    var version = Label(list, "Hội Chợ Việt Hàn · bản thử nghiệm " + Application.version, 28, Muted, TextAnchor.MiddleCenter);
                    Height(version, 60f);
                    refresh = () =>
                    {
                        SetText(vibration, "Rung: " + (s.vibration ? "Bật" : "Tắt"));
                        Look(vibration, s.vibration, Ok);
                        refreshOffline();
                    };
                    break;

                default:
                    // Nút lấy tiền để thử game: chỉ có trong Editor và bản Development, bản phát hành không hiện.
                    var cheats = HLayout(list, 100f, 16f);
                    Btn(cheats, "+1M xu", Gold, () => GiveTestCoins(1e6), 32);
                    Btn(cheats, "+1B xu", Gold, () => GiveTestCoins(1e9), 32);
                    Btn(cheats, "x10 xu", Gold, () => GiveTestCoins(System.Math.Max(1e3, g.Economy.Coins * 9)), 32);
                    Btn(cheats, "+100 Ngọc", GemBlue, () => { g.Economy.AddGems(100); Toast("Thử nghiệm: +100 Ngọc"); }, 32);
                    break;
            }

            refreshModal = refresh;
            refresh?.Invoke();
        }

        void GiveTestCoins(double amount)
        {
            Game.I.Economy.Add(amount);
            Game.I.Audio.Play(Sfx.Coin);
            Toast("Thử nghiệm: +" + NumberFormat.Short(amount) + " xu");
        }

        /// <summary>Bảng "Trong lúc bạn vắng" (GDD mục 4.11).</summary>
        public void OpenOfflinePanel(double coins, double seconds)
        {
            var g = Game.I;
            PendingOfflineCoins = coins;
            var content = OpenModal("Trong lúc bạn vắng", 720f);
            var info = Label(content, "Bạn đã vắng " + NumberFormat.Duration(seconds) + ".\nCác quản lý đã thu được <b>" + NumberFormat.Short(coins)
                + " xu</b>.\n<size=30><color=#A07F75>Tính tối đa " + g.OfflineCapHours + " giờ. Các quầy chưa có quản lý đã dồn tiền vào quỹ.</color></size>",
                38, Ink, TextAnchor.UpperLeft);
            Height(info, 260f);

            var buttons = HLayout(content, 150f, 20f);
            Btn(buttons, "Nhận\n" + NumberFormat.Short(coins) + " xu", Ok, ClaimOffline);
        }

        void ClaimOffline()
        {
            if (PendingOfflineCoins <= 0) return;
            double amount = PendingOfflineCoins;
            PendingOfflineCoins = 0;
            Game.I.Economy.Add(amount);
            Game.I.Data.stats.coinsCollected += amount;
            CloseModal();
            OnCoinsCollected(Game.I.Cam.transform.position + new Vector3(0f, 0f, 10f), amount);
            Game.I.Audio.Play(Sfx.Coin);
        }

        /// <summary>Hộp xác nhận đơn giản.</summary>
        public void Confirm(string title, string message, string okLabel, System.Action onOk)
        {
            var content = OpenModal(title, 620f);
            var text = Label(content, message, 38, Ink, TextAnchor.UpperLeft);
            Height(text, 230f);
            var buttons = HLayout(content, 140f, 20f);
            Btn(buttons, "Huỷ", Off, CloseModal);
            Btn(buttons, okLabel, Ok, () =>
            {
                CloseModal();
                onOk?.Invoke();
            });
        }
    }
}
