using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>Các bảng gắn với vật trên phố: dựng quầy, quầy, mở rộng phố, sân khấu.</summary>
    public partial class UIManager
    {
        public void OpenBuildPanel(StallSlot slot)
        {
            var content = OpenModal("Dựng quầy mới", 1200f);
            var cfg = Game.I.config;
            var list = ScrollList(content, 900f);
            var rows = new List<(Button button, StallData data)>();

            foreach (var d in Game.I.Street.BuildableStalls())
            {
                var data = d;
                var row = Row(list, 150f);
                var info = Label(row, "<b>" + d.displayName + "</b>  <color=#A07F75>· " + CountryName(d.origin) + "</color>\n<size=30>"
                    + d.products[0] + " · mỗi lần bán " + NumberFormat.Short(d.baseIncome * cfg.dishPriceMultipliers[0] * cfg.levels[0].incomeMultiplier) + " xu</size>",
                    40, Ink, TextAnchor.MiddleLeft);
                Stretch(info.rectTransform, Vector2.zero, new Vector2(0.64f, 1f), new Vector2(28f, 0f), Vector2.zero);

                var button = Btn(row, "Dựng\n" + NumberFormat.Short(d.unlockCost) + " xu", Ok, () =>
                {
                    if (Game.I.Street.BuildStall(slot, data) != null) CloseModal();
                    else NotEnough("Chưa đủ xu để dựng " + data.displayName);
                });
                Stretch((RectTransform)button.transform, new Vector2(0.66f, 0f), Vector2.one, new Vector2(0f, 16f), new Vector2(-20f, -16f));
                rows.Add((button, d));
            }

            if (rows.Count == 0)
            {
                var empty = Label(list, "Không còn quầy nào để dựng ở các đoạn phố đã mở.\nHãy mở rộng phố để có thêm quầy mới.", 36, Muted, TextAnchor.MiddleCenter);
                Height(empty, 200f);
            }

            refreshModal = () =>
            {
                foreach (var (button, data) in rows) Look(button, Game.I.Economy.CanAfford(data.unlockCost), Ok);
            };
            refreshModal();
        }

        /// <summary>Bảng quầy: quỹ tiền và 4 nâng cấp (sạp hàng, món ăn, người bán, doanh thu).</summary>
        public void OpenStallPanel(Stall stall)
        {
            var g = Game.I;
            var cfg = g.config;
            var content = OpenModal(stall.Data.displayName + "  ·  " + CountryName(stall.Data.origin), 860f);

            // Quỹ và nút thu tiền
            var top = Row(content, 110f);
            var vault = Label(top, "", 36, Ink, TextAnchor.MiddleLeft);
            Stretch(vault.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(28f, 0f), Vector2.zero);
            var collect = Btn(top, "", Gold, () => stall.Collect(), 32);
            Stretch((RectTransform)collect.transform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0f, 12f), new Vector2(-16f, -12f));

            // 1. Sạp hàng
            var (stallInfo, stallButton) = UpgradeRow(content, () =>
            {
                if (!stall.TryUpgrade()) NotEnough(stall.IsMaxLevel ? "Sạp đã đạt cấp tối đa" : "Chưa đủ xu để nâng sạp");
            });

            // 2. Món ăn: 3 ô hình món, món chưa mở hiện mờ
            var (dishInfo, dishButton) = UpgradeRow(content, () =>
            {
                if (!stall.TryUnlockDish()) NotEnough(stall.AllDishesOpen ? "Đã mở đủ 3 món" : "Chưa đủ xu để mở món");
            });
            Stretch(dishInfo.rectTransform, Vector2.zero, new Vector2(0.24f, 1f), new Vector2(24f, 0f), Vector2.zero);
            var dishIcons = new Image[Stall.DishSlots];
            var dishPrices = new Text[Stall.DishSlots];
            for (int i = 0; i < Stall.DishSlots; i++)
            {
                var row = (RectTransform)dishInfo.transform.parent;
                float x = 0.25f + i * 0.15f;
                var icon = Img(row, stall.DishSprite(i), Color.white, "Dish" + (i + 1));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Stretch(icon.rectTransform, new Vector2(x, 0.3f), new Vector2(x + 0.14f, 1f), new Vector2(4f, 0f), new Vector2(-4f, -6f));
                var price = Label(row, "", 24, Ink, TextAnchor.MiddleCenter);
                Stretch(price.rectTransform, new Vector2(x, 0f), new Vector2(x + 0.14f, 0.32f), Vector2.zero, Vector2.zero);
                dishIcons[i] = icon;
                dishPrices[i] = price;
            }

            // 3. Nhân viên: người bán sau quầy và người chào hàng trước quầy
            var (sellerInfo, sellerButton) = UpgradeRow(content, () =>
            {
                if (!stall.HireManager()) NotEnough(stall.HasManager ? "Quầy đã có người bán" : "Chưa đủ xu để thuê người bán");
            });
            Stretch(sellerInfo.rectTransform, Vector2.zero, new Vector2(0.44f, 1f), new Vector2(24f, 0f), Vector2.zero);
            Stretch((RectTransform)sellerButton.transform, new Vector2(0.45f, 0f), new Vector2(0.71f, 1f), new Vector2(0f, 12f), new Vector2(-8f, -12f));
            var barkerButton = Btn((RectTransform)sellerButton.transform.parent, "", GemBlue, () =>
            {
                if (!stall.TryHireBarker()) NotEnough(stall.HasBarker ? "Quầy đã có người chào hàng" : "Chưa đủ xu để thuê người chào hàng");
            }, 28);
            Stretch((RectTransform)barkerButton.transform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0f, 12f), new Vector2(-16f, -12f));

            // 4. Doanh thu
            var (revenueInfo, revenueButton) = UpgradeRow(content, () =>
            {
                if (!stall.TryUpgradeRevenue()) NotEnough(stall.IsMaxRevenue ? "Doanh thu đã tối đa" : "Chưa đủ xu để nâng doanh thu");
            });

            refreshModal = () =>
            {
                vault.text = "Quỹ: <b>" + NumberFormat.Short(stall.Vault) + " / " + NumberFormat.Short(stall.VaultCapacity) + " xu</b>"
                    + (stall.VaultFull ? "  <color=#FF7A70>ĐẦY - ngừng bán</color>" : "")
                    + "\n<size=28><color=#A07F75>Phục vụ " + stall.ServiceTime.ToString("0.#") + " giây/khách · khách mua ngẫu nhiên 1 món đã mở</color></size>";
                SetText(collect, stall.Vault > 0 ? "Thu tiền\n" + NumberFormat.Short(stall.Vault) + " xu" : "Quỹ trống");
                Look(collect, stall.Vault > 0, Gold);

                stallInfo.text = "<b>Sạp hàng</b>  cấp " + stall.Level + "/" + stall.MaxLevel + "\n<size=26><color=#A07F75>"
                    + (stall.IsMaxLevel ? "Sạp đẹp nhất phố!"
                        : "Cấp " + (stall.Level + 1) + ": phục vụ " + stall.ServiceTimeAt(stall.Level + 1).ToString("0.#") + " giây, giá bán x"
                          + (cfg.levels[stall.Level].incomeMultiplier / cfg.levels[stall.Level - 1].incomeMultiplier).ToString("0.##")
                          + ", quỹ lớn hơn")
                    + "</color></size>";
                SetText(stallButton, stall.IsMaxLevel ? "Tối đa" : "Nâng sạp\n" + NumberFormat.Short(stall.NextUpgradeCost) + " xu");
                Look(stallButton, !stall.IsMaxLevel && g.Economy.CanAfford(stall.NextUpgradeCost), Ok);

                dishInfo.text = "<b>Món ăn</b>  " + stall.DishCount + "/" + Stall.DishSlots;
                for (int i = 0; i < Stall.DishSlots; i++)
                {
                    bool open = i < stall.DishCount;
                    dishIcons[i].color = open ? Color.white : new Color(0.25f, 0.22f, 0.3f, 0.6f);
                    dishPrices[i].text = open ? NumberFormat.Short(stall.DishPrice(i)) + " xu" : "<color=#B9A29A>" + stall.DishName(i) + "</color>";
                }
                SetText(dishButton, stall.AllDishesOpen ? "Đủ món" : "Mở " + stall.DishName(stall.DishCount) + "\n" + NumberFormat.Short(stall.NextDishCost) + " xu");
                Look(dishButton, !stall.AllDishesOpen && g.Economy.CanAfford(stall.NextDishCost), Ok);

                bool token = g.Data.managerTokens > 0;
                sellerInfo.text = "<b>Nhân viên</b>\n<size=24><color=#A07F75>Người bán: tự thu tiền, bán khi offline\nChào hàng: mời thêm "
                    + Mathf.RoundToInt(cfg.barkerInviteChance * 100f) + "% khách</color></size>";
                SetText(sellerButton, stall.HasManager ? "Đã có\nngười bán" : token ? "Người bán\nmiễn phí" : "Người bán\n" + NumberFormat.Short(stall.ManagerCost) + " xu");
                Look(sellerButton, !stall.HasManager && (token || g.Economy.CanAfford(stall.ManagerCost)), GemBlue);
                SetText(barkerButton, stall.HasBarker ? "Đã có\nchào hàng" : "Chào hàng\n" + NumberFormat.Short(stall.BarkerCost) + " xu");
                Look(barkerButton, !stall.HasBarker && g.Economy.CanAfford(stall.BarkerCost), GemBlue);

                int pct = Mathf.RoundToInt(cfg.revenuePerLevel * 100f);
                revenueInfo.text = "<b>Doanh thu</b>  cấp " + stall.RevenueLevel + "/" + stall.MaxRevenueLevel
                    + "\n<size=26><color=#A07F75>Giá bán mọi món +" + Mathf.RoundToInt(cfg.revenuePerLevel * stall.RevenueLevel * 100f) + "%"
                    + (stall.IsMaxRevenue ? "" : " → +" + (Mathf.RoundToInt(cfg.revenuePerLevel * stall.RevenueLevel * 100f) + pct) + "%") + "</color></size>";
                SetText(revenueButton, stall.IsMaxRevenue ? "Tối đa" : "+" + pct + "% giá bán\n" + NumberFormat.Short(stall.NextRevenueCost) + " xu");
                Look(revenueButton, !stall.IsMaxRevenue && g.Economy.CanAfford(stall.NextRevenueCost), Ok);
            };
            refreshModal();
        }

        /// <summary>Một hàng nâng cấp: mô tả bên trái, nút mua bên phải.</summary>
        (Text info, Button button) UpgradeRow(RectTransform content, UnityEngine.Events.UnityAction onClick)
        {
            var row = Row(content, 124f);
            var info = Label(row, "", 34, Ink, TextAnchor.MiddleLeft);
            Stretch(info.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), new Vector2(24f, 0f), Vector2.zero);
            var button = Btn(row, "", Ok, onClick, 28);
            Stretch((RectTransform)button.transform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(0f, 12f), new Vector2(-16f, -12f));
            return (info, button);
        }

        public void OpenExpandPanel()
        {
            var street = Game.I.Street;
            if (!street.CanExpand) return;
            var seg = street.NextSegment;
            int number = street.UnlockedSegments + 1;

            var content = OpenModal("Mở rộng con phố", 640f);
            string what = seg.stageSegment
                ? "Thêm 2 sân khấu biểu diễn, sức chứa +" + Game.I.config.customersPerSegment + " khách."
                : "Thêm " + street.segments[street.UnlockedSegments].slots.Length + " ô quầy, sức chứa +" + Game.I.config.customersPerSegment + " khách, mở thêm quầy mới.";
            var info = Label(content, "Đoạn " + number + ": <b>" + seg.name + "</b>\n" + what, 40, Ink, TextAnchor.UpperLeft);
            Height(info, 200f);

            var button = Btn(content, "", Ok, () =>
            {
                if (street.TryExpand())
                {
                    CloseModal();
                    Toast(street.ComingSoon
                        ? "Đã mở " + seg.name + "! Phần tiếp theo sẽ có trong bản cập nhật sau."
                        : "Đã mở " + seg.name + "!");
                }
                else NotEnough("Chưa đủ xu để mở rộng phố");
            });
            Height(button, 150f);

            refreshModal = () =>
            {
                SetText(button, "Mở rộng - " + NumberFormat.Short(seg.unlockCost) + " xu");
                Look(button, Game.I.Economy.CanAfford(seg.unlockCost), Ok);
            };
            refreshModal();
        }

        /// <summary>Bảng mở một khu dọc đường dưới (ăn uống, trò chơi dân gian, biểu diễn).</summary>
        public void OpenPlazaZonePanel(int zone)
        {
            var g = Game.I;
            var street = g.Street;
            if (!street.CanUnlockZone(zone)) return;
            double cost = street.ZoneCost(zone);
            var content = OpenModal("Mở " + street.ZoneName(zone), 640f);
            string what;
            switch (street.ZoneKind(zone))
            {
                case StreetManager.PlazaZoneKind.Dining:
                    what = "6 bộ bàn ghế nhựa vỉa hè, xếp 2 hàng. Khách mua xong sẽ xuống ngồi ăn tại đây.";
                    break;
                case StreetManager.PlazaZoneKind.FolkGames:
                    what = "3 trò chơi dân gian Việt - Hàn: Đập niêu, Đá cầu, Tuho. Mở khu xong, chạm vào từng trò để mở lần lượt và nghe kể về ý nghĩa. Khách chơi trả phí cho bạn; bạn chơi mini-game nhận xu và tăng thu nhập.";
                    break;
                default:
                    what = "2 sân khấu biểu diễn. Mời nghệ sĩ lên diễn để khách kéo đến đông gấp đôi.";
                    break;
            }
            var info = Label(content, "Khu " + (zone + 1) + ": <b>" + street.ZoneName(zone) + "</b>\n<size=32>" + what + "</size>", 38, Ink, TextAnchor.UpperLeft);
            Height(info, 220f);

            var button = Btn(content, "", Ok, () =>
            {
                if (street.TryUnlockZone(zone))
                {
                    CloseModal();
                    Toast(street.ZoneKind(zone) == StreetManager.PlazaZoneKind.FolkGames
                        ? "Đã mở khu trò chơi! Chạm vào trò đầu tiên để mở"
                        : "Đã mở " + street.ZoneName(zone) + "!");
                }
                else NotEnough("Chưa đủ xu để mở " + street.ZoneName(zone).ToLower());
            });
            Height(button, 150f);

            refreshModal = () =>
            {
                SetText(button, "Mở khu - " + NumberFormat.Short(cost) + " xu");
                Look(button, g.Economy.CanAfford(cost), Ok);
            };
            refreshModal();
        }

        public void OpenStagePanel(StageSpot stage)
        {
            var g = Game.I;
            var street = g.Street;
            var content = OpenModal(stage.DisplayName, 1350f);

            var status = Label(content, "", 38, Ink, TextAnchor.MiddleLeft);
            Height(status, 110f);

            var resetRow = HLayout(content, 120f, 20f);
            var resetGems = Btn(resetRow, "Hồi ngay\n" + g.config.performResetGems + " Ngọc", GemBlue, () =>
            {
                if (!stage.TryResetWithGems()) NotEnough("Chưa đủ Ngọc");
            }, 32);

            var list = ScrollList(content, 800f);
            var rows = new List<(Button button, PerformerData data)>();
            foreach (var p in g.config.performers)
            {
                var performer = p;
                var row = Row(list, 170f);
                var info = Label(row, "<b>" + p.displayName + "</b>  <color=#A07F75>· " + CountryName(p.origin) + " · " + Mathf.RoundToInt(p.duration) + " giây</color>\n<size=28>"
                    + p.description + "</size>", 36, Ink, TextAnchor.MiddleLeft);
                Stretch(info.rectTransform, Vector2.zero, new Vector2(0.66f, 1f), new Vector2(24f, 0f), Vector2.zero);

                var button = Btn(row, "", Ok, () =>
                {
                    if (!street.OwnsPerformer(performer))
                    {
                        if (!street.BuyPerformer(performer)) NotEnough("Chưa đủ xu để mời " + performer.displayName);
                    }
                    else if (stage.IsPerforming) Toast("Sân khấu đang có tiết mục");
                    else if (!stage.Ready) Toast("Sân khấu đang hồi chiêu");
                    else if (stage.Perform(performer)) CloseModal();
                }, 34);
                Stretch((RectTransform)button.transform, new Vector2(0.68f, 0f), Vector2.one, new Vector2(0f, 18f), new Vector2(-18f, -18f));
                rows.Add((button, p));
            }

            refreshModal = () =>
            {
                if (stage.IsPerforming) status.text = "Đang diễn: <b>" + stage.Current.displayName + "</b> · còn " + NumberFormat.Clock(stage.PerformLeft) + "\nKhách đến x" + g.config.performSpawnMultiplier;
                else if (stage.Ready) status.text = "Sân khấu sẵn sàng. Chọn tiết mục để biểu diễn.";
                else status.text = "Hồi chiêu còn " + NumberFormat.Clock(stage.CooldownLeft);

                resetRow.gameObject.SetActive(!stage.Ready && !stage.IsPerforming);
                Look(resetGems, g.Economy.CanAffordGems(g.config.performResetGems), GemBlue);

                foreach (var (button, data) in rows)
                {
                    bool owned = street.OwnsPerformer(data);
                    if (!owned)
                    {
                        SetText(button, "Mời\n" + NumberFormat.Short(data.coinCost) + " xu");
                        Look(button, g.Economy.CanAfford(data.coinCost), Gold);
                    }
                    else
                    {
                        SetText(button, "Biểu diễn");
                        Look(button, stage.Ready && !stage.IsPerforming, Ok);
                    }
                }
            };
            refreshModal();
        }

        void NotEnough(string message)
        {
            Toast(message);
            Game.I.Audio.Play(Sfx.Error);
        }
    }
}
