using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tami
{
    /// <summary>
    /// Tự chơi thử các tính năng chính khi chạy game với tham số -tamiAutotest, ghi kết quả vào log
    /// (dòng bắt đầu bằng [AUTOTEST]) và file autotest_result.txt, rồi thoát với mã 0 nếu mọi thứ đạt.
    /// Chạy được cả ở chế độ -batchmode -nographics nên không cần cửa sổ game.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        public static AutoTest Instance { get; private set; }

        readonly StringBuilder report = new StringBuilder();
        int passed, failed, logErrors;

        static Game G => Game.I;

        void Awake()
        {
            Instance = this;
            Application.logMessageReceived += OnLog;
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            logErrors++;
            report.AppendLine("  LOG " + type + ": " + message + "\n" + stack);
        }

        void Check(string name, bool ok, string detail = "")
        {
            if (ok) passed++;
            else failed++;
            string line = (ok ? "PASS " : "FAIL ") + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")");
            report.AppendLine(line);
            Debug.Log("[AUTOTEST] " + line);
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float gameSeconds)
        {
            float end = Time.time + gameSeconds;
            while (!condition() && Time.time < end)
            {
                G.UI.DismissAllDialogues();
                yield return null;
            }
        }

        static IEnumerator Settle(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                G.UI.DismissAllDialogues();
                yield return null;
            }
        }

        static string ArgValue(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        /// <summary>
        /// Chụp ảnh 1920x1080 cả thế giới lẫn giao diện vào thư mục của tham số -tamiCapture.
        /// Chạy được khi không có cửa sổ vì render thẳng vào RenderTexture.
        /// </summary>
        static IEnumerator Capture(string name)
        {
            string dir = ArgValue("-tamiCapture");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            yield return null;
            yield return null;

            const int w = 1920, h = 1080;
            var cam = G.Cam;
            var canvas = G.UI.GetComponentInChildren<Canvas>();
            var rt = new RenderTexture(w, h, 24);
            var mode = canvas.renderMode;
            int order = canvas.sortingOrder;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = short.MaxValue;
            cam.targetTexture = rt;
            cam.ResetAspect();
            // Chờ để giao diện tính lại bố cục theo kích thước 1920x1080.
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            cam.ResetAspect();
            canvas.renderMode = mode;
            canvas.sortingOrder = order;
            Destroy(rt);
            Destroy(tex);
        }

        static float SegmentCenter(int index)
        {
            var segment = G.Street.segments[index];
            return segment.LeftX + segment.width * 0.5f;
        }

        static void LookAt(float x)
        {
            var p = G.Cam.transform.position;
            p.x = x;
            G.Cam.transform.position = p;
        }

        IEnumerator Start()
        {
            Time.timeScale = 8f;
            while (G == null || !G.Started) yield return null;
            yield return null;

            var cfg = G.config;
            var st = G.Street;

            // ---- Lần chơi đầu: intro, tutorial ----
            Check("Intro hiện khi chơi lần đầu", G.UI.DialogueOpen);
            yield return Capture("01_intro");
            G.UI.DismissAllDialogues();
            yield return null;
            Check("Tutorial bắt đầu ở bước 0", G.Tutorial.Active && G.Tutorial.Step == 0, "bước " + G.Tutorial.Step);
            Check("Bắt đầu với 500 xu và 4 ô trống", G.Economy.Coins == 500 && st.Slots.Count == 4 && st.Stalls.Count == 0,
                "xu " + G.Economy.Coins + ", ô " + st.Slots.Count);

            var bc = st.BuildStall(st.Slots[0], cfg.FindStall("banh_chung"));
            var tb = st.BuildStall(st.Slots[1], cfg.FindStall("tteokbokki"));
            yield return Settle(2);
            Check("Dựng Bánh chưng và Tteokbokki", bc != null && tb != null && G.Economy.Coins == 0, "xu " + G.Economy.Coins);

            yield return WaitUntil(() => bc.Vault > 0 || tb.Vault > 0, 150f);
            Check("Khách vào mua, tiền dồn vào quỹ quầy", bc.Vault > 0 || tb.Vault > 0, "quỹ " + bc.Vault + " / " + tb.Vault);
            Check("Thống kê ghi nhận lượt bán", G.Data.stats.totalSales > 0, "lượt " + G.Data.stats.totalSales);

            G.UI.DismissAllDialogues();
            yield return Capture("02_street_first_sale");
            G.UI.OpenStallPanel(bc);
            yield return Capture("03_stall_panel");
            G.UI.CloseModal();

            var withMoney = bc.Vault > 0 ? bc : tb;
            double before = G.Economy.Coins, vault = withMoney.Vault;
            withMoney.Collect();
            Check("Thu tiền chuyển quỹ quầy về quỹ chung", G.Economy.Coins == before + vault && withMoney.Vault == 0);
            yield return Settle(3);

            // ---- Quỹ đầy ----
            bc.SetVaultForTest(bc.VaultCapacity);
            Check("Quỹ đầy thì quầy không nhận khách", !bc.CanAcceptCustomer);
            double full = bc.Vault;
            yield return new WaitForSeconds(15f);
            Check("Quỹ đầy thì không tăng thêm", bc.Vault == full);
            bc.Collect();

            // ---- 4 nâng cấp: sạp, món, doanh thu ----
            G.Economy.Add(1e6);
            bool d2 = bc.TryUnlockDish(), d3 = bc.TryUnlockDish(), d4 = bc.TryUnlockDish();
            Check("Mở đúng 3 món, món 3 giá x6", d2 && d3 && !d4 && bc.DishCount == 3
                && System.Math.Abs(bc.DishPrice(2) / bc.Data.baseIncome - 6) < 1e-6, "món " + bc.DishCount);
            bool up2 = bc.TryUpgrade(), up3 = bc.TryUpgrade(), up4 = bc.TryUpgrade(), up5 = bc.TryUpgrade();
            Check("Nâng sạp đúng 4 cấp", up2 && up3 && up4 && !up5 && bc.Level == 4, "cấp " + bc.Level);
            double priceBefore = bc.DishPrice(0);
            Check("Nâng doanh thu tăng giá bán", bc.TryUpgradeRevenue() && bc.DishPrice(0) > priceBefore);
            yield return WaitUntil(() => G.Data.tutorialDone, 5f);
            Check("Tutorial hoàn thành", G.Data.tutorialDone, "bước " + G.Tutorial.Step);

            // ---- Đủ 4 quầy, mở rộng phố ----
            st.BuildStall(st.Slots[2], cfg.FindStall("banh_mi"));
            Check("Dựng đủ 3 quầy đoạn 1", st.Stalls.Count == 3 && !st.HasEmptySlot);
            G.Economy.Add(1e7);
            bool expanded = st.TryExpand();
            yield return Settle(3);
            Check("Mở rộng đoạn 2: thêm 3 ô, sức chứa trang trí tăng", expanded && st.UnlockedSegments == 2 && st.Slots.Count == 6 && st.DecorCapacity == 2 * cfg.decorPerSegment);

            // ---- Quản lý ----
            bool hired = tb.HireManager();
            tb.SetVaultForTest(50);
            double coinsBeforeManager = G.Economy.Coins;
            yield return WaitUntil(() => G.Economy.Coins >= coinsBeforeManager + 50, 15f);
            Check("Quản lý tự thu tiền", hired && tb.HasManager && G.Economy.Coins >= coinsBeforeManager + 50);

            // ---- Khách đặc biệt ----
            int guestsBefore = G.Data.stats.specialGuests;
            var mystery = cfg.npcTypes.First(t => t.kind == NpcKind.Mystery);
            G.Spawner.SpawnForTest(mystery);
            var mysteryNpc = G.Spawner.LastSpawned;
            double c0 = G.Economy.Coins;
            int g0 = G.Economy.Gems;
            mysteryNpc.OnPointerClick(null);
            Check("Khách bí ẩn tặng thưởng", G.Economy.Coins > c0 || G.Economy.Gems > g0);

            int tourCount = G.Spawner.SpawnForTest(cfg.npcTypes.First(t => t.kind == NpcKind.TourGroup));
            Check("Đoàn du lịch đi theo nhóm 4-6 người", tourCount >= 4 && tourCount <= 6, tourCount + " người");

            G.Spawner.SpawnForTest(cfg.npcTypes.First(t => t.kind == NpcKind.FoodBlogger));
            yield return WaitUntil(() => G.Spawner.BloggerBoostActive, 400f);
            Check("Food blogger rời phố thì khách tăng", G.Spawner.BloggerBoostActive);
            Check("Đếm khách đặc biệt", G.Data.stats.specialGuests >= guestsBefore + 3, G.Data.stats.specialGuests.ToString());

            // ---- Trang trí ----
            int attraction = st.Attraction;
            var coinDecor = cfg.decorations.First(d => d.gemCost == 0);
            Vector2 lowerBrick = new Vector2(3f, st.roadBottomY - 1.5f);
            Check("Không đặt trang trí lên đường đi", !st.CanPlaceDecor(new Vector2(3f, (st.roadTopY + st.roadBottomY) * 0.5f)));
            var placed = st.BuyDecor(coinDecor, lowerBrick);
            Check("Mua trang trí bằng xu, đặt trên gạch, tăng độ hấp dẫn", placed != null && st.Attraction == attraction + coinDecor.attraction);
            Check("Không đặt chồng lên món khác", !st.CanPlaceDecor(lowerBrick));
            G.Economy.AddGems(100);
            int gemsBefore = G.Economy.Gems;
            var gemDecor = cfg.decorations.First(d => d.gemCost > 0);
            Check("Mua trang trí bằng Ngọc", st.BuyDecor(gemDecor, lowerBrick + new Vector2(2f, 0f)) != null && G.Economy.Gems == gemsBefore - gemDecor.gemCost);

            G.UI.DismissAllDialogues();
            LookAt(SegmentCenter(1));
            yield return Capture("04_street_segment2");
            G.UI.OpenBuildPanel(st.Slots.First(s => s.Stall == null));
            yield return Capture("05_build_panel");
            G.UI.OpenManagersPanel();
            yield return Capture("06_managers");
            G.UI.CloseModal();
            G.Decor.Enter();
            G.Decor.StartPlacing(coinDecor);
            yield return Capture("07_decor");
            G.Decor.Exit();
            G.UI.OpenStatsPanel();
            yield return Capture("08_stats");
            G.UI.CloseModal();
            G.UI.ShowDialogue(cfg.segments[1].dialogue);
            yield return Capture("09_dialogue_minji");
            G.UI.DismissAllDialogues();

            // ---- Mở hết phần chơi được (4 đoạn phố ăn uống), 12 quầy cấp 4 ----
            G.Economy.Add(1e13);
            while (st.CanExpand) st.TryExpand();
            yield return Settle(3);
            Check("Mở hết " + st.PlayableSegments + " đoạn phố chơi được, đủ 12 ô quầy",
                st.UnlockedSegments == st.PlayableSegments && st.Slots.Count == 12);
            Check("Đoạn sau báo cập nhật sau", st.ComingSoon && !st.TryExpand());
            foreach (var data in cfg.stalls)
            {
                if (st.Stalls.Any(s => s.Data == data)) continue;
                var empty = st.Slots.First(s => s.Stall == null);
                st.BuildStall(empty, data);
            }
            foreach (var s in st.Stalls)
            {
                while (s.TryUpgrade()) { }
                while (s.TryUnlockDish()) { }
            }
            yield return Settle(3);
            Check("Đủ 12 quầy, tất cả sạp cấp 4, đủ 3 món", st.Stalls.Count == 12 && st.Stalls.All(s => s.Level == 4 && s.DishCount == 3));

            // ---- Biểu diễn (chỉ khi đã mở quảng trường sân khấu) ----
            if (st.Stages.Count > 0)
            {
                var performer = cfg.performers[0];
                float spawnBefore = G.Spawner.SpawnPerMinute;
                bool bought = st.BuyPerformer(performer);
                bool performed = st.Stages[0].Perform(performer);
                Check("Mời nghệ sĩ và biểu diễn: khách đến x2", bought && performed && st.PerformActive
                    && Mathf.Abs(G.Spawner.SpawnPerMinute - spawnBefore * cfg.performSpawnMultiplier) < 0.01f);
                Check("Sân khấu vào hồi chiêu", !st.Stages[0].Ready);
                G.UI.DismissAllDialogues();
                LookAt(SegmentCenter(4));
                yield return new WaitForSeconds(3f);
                yield return Capture("10_stage_performing");
                G.UI.OpenStagePanel(st.Stages[0]);
                yield return Capture("11_stage_panel");
                G.UI.CloseModal();
                LookAt(G.Street.segments[1].RightX);
                yield return Capture("12_street_full");
                G.Economy.AddGems(cfg.performResetGems);
                Check("Hồi chiêu ngay bằng Ngọc", st.Stages[0].TryResetWithGems() && st.Stages[0].Ready);
            }

            // ---- Thành tựu ----
            G.Achievements.Check();
            int claimable = G.Achievements.ClaimableCount;
            int gemsBeforeClaim = G.Economy.Gems;
            foreach (var a in Achievements.All) G.Achievements.Claim(a);
            Check("Nhận thưởng thành tựu", claimable > 0 && G.Economy.Gems > gemsBeforeClaim, claimable + " thành tựu");
            yield return Settle(3);
            G.UI.OpenAchievementsPanel();
            yield return Capture("13_achievements");
            G.UI.OpenSettingsPanel();
            yield return Capture("14_settings");
            G.UI.CloseModal();

            // ---- Lưu, tải lại, offline ----
            var bcSlot = bc.Slot.Index;
            bc.Collect();
            double savedCoins = G.Economy.Coins;
            int savedGems = G.Economy.Gems;
            int savedDecor = st.DecorationCount;
            int managers = st.Stalls.Count(s => s.HasManager);
            G.SaveNow();
            var save = SaveSystem.Load();
            save.lastSaveUtcTicks -= System.TimeSpan.FromHours(2).Ticks;
            SaveSystem.Write(save, false);

            var oldGame = G;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            while (G == null || G == oldGame || !G.Started) yield return null;
            yield return null;
            st = G.Street;

            Check("Tải lại: giữ xu, Ngọc, 5 đoạn phố, 12 quầy sạp cấp 4 đủ món",
                System.Math.Abs(G.Economy.Coins - savedCoins) <= 1e-6 * System.Math.Max(1, savedCoins)
                && G.Economy.Gems == savedGems && st.UnlockedSegments == 5
                && st.Stalls.Count == 12 && st.Stalls.All(s => s.Level == 4 && s.DishCount == 3),
                "xu " + G.Economy.Coins + "/" + savedCoins + ", Ngọc " + G.Economy.Gems + "/" + savedGems);
            Check("Tải lại: giữ trang trí và quản lý", st.DecorationCount == savedDecor && st.Stalls.Count(s => s.HasManager) == managers);
            Check("Tải lại: không hiện lại intro và tutorial", G.Data.introSeen && G.Data.tutorialDone && !G.Tutorial.Active);
            Check("Offline: quản lý thu tiền trong lúc vắng", G.UI.OfflinePanelOpen && G.UI.PendingOfflineCoins > 0,
                NumberFormat.Short(G.UI.PendingOfflineCoins) + " xu");
            var bcRestored = st.Slots[bcSlot].Stall;
            Check("Offline: quầy chưa có quản lý dồn tiền vào quỹ", bcRestored != null && bcRestored.Vault > 0);
            yield return Capture("15_offline_panel");

            yield return Settle(10);
            Check("Không có lỗi hoặc exception trong log", logErrors == 0, logErrors + " lỗi");

            Finish();
        }

        void Finish()
        {
            string summary = "[AUTOTEST] Kết quả: " + passed + " đạt, " + failed + " lỗi";
            report.Insert(0, summary + "\n");
            Debug.Log(summary);
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "autotest_result.txt"), report.ToString());
            }
            catch (System.Exception e)
            {
                Debug.Log("[AUTOTEST] Không ghi được file kết quả: " + e.Message);
            }
            Application.Quit(failed == 0 ? 0 : 1);
        }
    }
}
