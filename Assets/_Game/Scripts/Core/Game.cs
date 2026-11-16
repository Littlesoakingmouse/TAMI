using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Tami
{
    /// <summary>Điểm khởi động: tạo các hệ thống, nạp save, tính thu nhập offline, tự lưu.</summary>
    [DefaultExecutionOrder(-100)]
    public class Game : MonoBehaviour
    {
        const float AutosaveInterval = 30f;

        public static Game I { get; private set; }
        public static bool AutoTestMode { get; private set; }

        public GameConfig config;
        public GameArt art;
        [Tooltip("Con phố đặt sẵn trong scene")]
        public StreetManager street;
        [Tooltip("Nhân vật chính đặt sẵn trong scene")]
        public PlayerController player;

        public Economy Economy { get; private set; }
        public StreetManager Street { get; private set; }
        public PlayerController Player { get; private set; }
        public NpcSpawner Spawner { get; private set; }
        public UIManager UI { get; private set; }
        public AudioManager Audio { get; private set; }
        public Tutorial Tutorial { get; private set; }
        public DecorMode Decor { get; private set; }
        public Achievements Achievements { get; private set; }
        public Camera Cam { get; private set; }
        public Font Font { get; private set; }
        public SaveData Data { get; private set; }
        public bool Started { get; private set; }

        public float OfflineCapHours
        {
            get
            {
                int level = Data != null ? Data.offlineUpgradeLevel : 0;
                if (level <= 0) return config.offlineBaseHours;
                return config.offlineUpgradeHours[Mathf.Min(level, config.offlineUpgradeHours.Length) - 1];
            }
        }

        /// <summary>Tăng thu nhập (thưởng từ trò chơi dân gian): nhân tiền mỗi lần bán của mọi quầy.</summary>
        public float IncomeBoostUntil { get; private set; } = -1f;
        public float IncomeBoostMultiplier { get; private set; } = 1f;
        public bool IncomeBoostActive => Time.time < IncomeBoostUntil;
        public float IncomeMultiplier => IncomeBoostActive ? IncomeBoostMultiplier : 1f;

        /// <summary>Bật tăng thu nhập; đang có tăng thu nhập thì giữ hệ số lớn hơn và cộng dồn thời gian còn lại.</summary>
        public void StartIncomeBoost(float multiplier, float seconds)
        {
            IncomeBoostMultiplier = IncomeBoostActive ? Mathf.Max(IncomeBoostMultiplier, multiplier) : multiplier;
            IncomeBoostUntil = Mathf.Max(IncomeBoostUntil, Time.time) + seconds;
        }

        float autosaveTimer, achievementTimer;
        bool resetting;

        void Awake()
        {
            I = this;
            SetupMobile();
            Application.runInBackground = true;

            if (!AutoTestMode && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-tamiAutotest") >= 0)
            {
                AutoTestMode = true;
                SaveSystem.PathOverride = Path.Combine(Application.persistentDataPath, "autotest_save.json");
                SaveSystem.Delete();
                var test = new GameObject("AutoTest");
                DontDestroyOnLoad(test);
                test.AddComponent<AutoTest>();
            }

            Cam = Camera.main;
            if (Cam.GetComponent<Physics2DRaycaster>() == null) Cam.gameObject.AddComponent<Physics2DRaycaster>();
            // Font chung của game (Nunito); thiếu thì dùng font có sẵn của Unity.
            Font = art.font != null ? art.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Economy = new Economy(config.startingCoins, config.startingGems);
            Achievements = new Achievements();
            Street = street != null ? street : FindAnyObjectByType<StreetManager>();
            Player = player != null ? player : FindAnyObjectByType<PlayerController>();
            Spawner = CreateSystem<NpcSpawner>("Customers");
            UI = CreateSystem<UIManager>("UI");
            Audio = CreateSystem<AudioManager>("Audio");
            Tutorial = CreateSystem<Tutorial>("Tutorial");
            Decor = CreateSystem<DecorMode>("DecorMode");
            if (Debug.isDebugBuild) CreateSystem<PerfStats>("PerfStats");
        }

        void Start()
        {
            Street.Build();
            if (Player != null)
            {
                var scroller = Cam.GetComponent<CameraScroller>();
                if (scroller != null)
                {
                    scroller.follow = Player.transform;
                    scroller.SnapToFollow();
                }
            }

            double offlineCoins = 0, offlineSeconds = 0;
            Data = SaveSystem.Load();
            bool restored = Data != null;
            if (restored) Restore(out offlineCoins, out offlineSeconds);
            else Data = new SaveData { coins = config.startingCoins, gems = config.startingGems };
            Data.EnsureValid();
            Data.layoutVersion = CurrentLayout;

            Audio.Init(Data.settings);
            UI.Build();
            Achievements.Init();
            Started = true;

            if (!Data.introSeen)
            {
                UI.ShowDialogue(config.introLines, () =>
                {
                    Data.introSeen = true;
                    Tutorial.Begin();
                });
            }
            else
            {
                Tutorial.Begin();
            }

            if (offlineCoins > 0) UI.OpenOfflinePanel(offlineCoins, offlineSeconds);
            else if (restored && offlineSeconds >= 60 && Street.Stalls.Count > 0)
                UI.Toast("Trong lúc bạn vắng, các quầy vẫn bán hàng. Đi thu tiền thôi!");
        }

        /// <summary>Thiết lập cho điện thoại: 60 FPS, không tắt màn hình khi đang chơi, chỉ xoay ngang.</summary>
        static void SetupMobile()
        {
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
            }
            else
            {
                // PC và Editor: khoá theo tần số màn hình để khung hình đều, không bị giật do lệch nhịp.
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        /// <summary>
        /// Phiên bản bố cục khi lưu: 0 = 4 ô sạp mỗi khu, 2 = 3 ô sạp mỗi khu,
        /// 3 = trang trí đặt tự do (lưu toạ độ thay vì số thứ tự chỗ trang trí).
        /// </summary>
        const int CurrentLayout = 3;
        static readonly float[] OldDecorX = { 1.45f, 4.35f, 7.25f, 10.15f, 13.05f, 15.95f };

        static int OldSlotToNew(int oldSlot)
        {
            int segment = oldSlot / 4, pos = oldSlot % 4;
            return pos < 3 ? segment * 3 + pos : -1;
        }

        void PlaceSaved(StallSlot slot, StallData data, StallSave s)
        {
            var stall = Street.PlaceStall(slot, data);
            stall.Restore(s.level, s.dishes, s.revenue, s.vault, s.manager, s.barker, s.rate);
        }

        void Restore(out double offlineCoins, out double offlineSeconds)
        {
            Data.EnsureValid();
            Economy.Set(Data.coins, Data.gems);
            for (int i = 1; i < Data.segments; i++) Street.UnlockSegmentFree();
            for (int i = 0; i < Data.plazaZones; i++) Street.UnlockZoneFree();
            int folkGames = Data.folkGamesUnlocked >= 0 ? Data.folkGamesUnlocked : Street.FolkGames.Count > 0 ? 2 : 0;
            for (int i = 0; i < folkGames; i++) Street.UnlockFolkGameFree();

            var homeless = new List<StallSave>();
            foreach (var s in Data.stalls)
            {
                var stallData = config.FindStall(s.id);
                if (stallData == null) continue; // quầy đã bỏ khỏi game (quà lưu niệm, cho thuê trang phục)
                int slot = Data.layoutVersion < 2 ? OldSlotToNew(s.slot) : s.slot;
                if (slot < 0 || slot >= Street.Slots.Count || Street.Slots[slot].Stall != null)
                {
                    homeless.Add(s);
                    continue;
                }
                PlaceSaved(Street.Slots[slot], stallData, s);
            }
            // Quầy mất chỗ khi đổi bố cục (ô thứ 4 cũ) được dựng vào ô trống đầu tiên.
            foreach (var s in homeless)
            {
                var free = Street.Slots.Find(sl => sl.Stall == null);
                if (free != null) PlaceSaved(free, config.FindStall(s.id), s);
            }

            foreach (var d in Data.decorations)
            {
                var decoration = config.FindDecoration(d.id);
                if (decoration == null) continue;
                Vector2 at = new Vector2(d.x, d.y);
                if (Data.layoutVersion < 3)
                {
                    // Save cũ đặt trang trí vào 6 chỗ cố định mỗi đoạn phố, dưới đường đi.
                    int segment = d.spot / OldDecorX.Length;
                    if (segment >= Street.UnlockedSegments) continue;
                    at = new Vector2(Street.segments[segment].LeftX + OldDecorX[d.spot % OldDecorX.Length], Street.roadBottomY - 1.7f);
                }
                Street.PlaceDecor(decoration, at);
            }

            offlineSeconds = Data.lastSaveUtcTicks > 0
                ? (System.DateTime.UtcNow.Ticks - Data.lastSaveUtcTicks) / (double)System.TimeSpan.TicksPerSecond
                : 0;
            // Giờ máy bị lùi thì không tính offline (GDD mục 9.6).
            if (offlineSeconds < 0) offlineSeconds = 0;
            offlineCoins = offlineSeconds >= 60 ? ApplyOffline(offlineSeconds) : 0;
        }

        /// <summary>Quầy không có quản lý dồn tiền vào quỹ; quầy có quản lý trả về số xu đã thu.</summary>
        double ApplyOffline(double seconds)
        {
            double capped = System.Math.Min(seconds, OfflineCapHours * 3600.0);
            double managerCoins = 0;
            foreach (var stall in Street.Stalls)
            {
                double earned = stall.OfflineRate * config.offlineRate * capped;
                if (stall.HasManager) managerCoins += earned;
                else stall.AddOfflineVault(earned);
            }
            return managerCoins;
        }

        void Update()
        {
            if (!Started) return;
            Data.playSeconds += Time.unscaledDeltaTime;

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveInterval)
            {
                autosaveTimer = 0f;
                SaveNow(true);
            }

            achievementTimer += Time.unscaledDeltaTime;
            if (achievementTimer >= 1f)
            {
                achievementTimer = 0f;
                Achievements.Check();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        /// <param name="background">Ghi file ở luồng nền (tự lưu). Khi thoát hoặc ẩn app thì ghi ngay để không mất save.</param>
        public void SaveNow(bool background = false)
        {
            if (!Started || resetting || Data == null) return;
            Data.coins = Economy.Coins;
            Data.gems = Economy.Gems;
            Data.segments = Street.UnlockedSegments;
            Data.plazaZones = Street.PlazaZonesUnlocked;
            Data.folkGamesUnlocked = Street.FolkGamesUnlocked;

            Data.stalls.Clear();
            foreach (var s in Street.Stalls)
            {
                Data.stalls.Add(new StallSave
                {
                    slot = s.Slot.Index,
                    id = s.Data.id,
                    level = s.Level,
                    dishes = s.DishCount,
                    revenue = s.RevenueLevel,
                    manager = s.HasManager,
                    barker = s.HasBarker,
                    vault = s.Vault,
                    rate = s.IncomeRate,
                });
            }

            Data.decorations.Clear();
            foreach (var d in Street.Decorations)
            {
                Vector2 p = d.transform.position;
                Data.decorations.Add(new DecorSave { spot = -1, id = d.Data.id, x = p.x, y = p.y });
            }

            if (background) SaveSystem.WriteInBackground(Data);
            else SaveSystem.Write(Data);
        }

        public void ResetProgress()
        {
            resetting = true;
            SaveSystem.Delete();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Vibrate()
        {
            if (Data == null || !Data.settings.vibration) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        T CreateSystem<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        public SpriteRenderer NewSprite(Transform parent, Sprite sprite, int order, string name = "Sprite")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (art.spriteMaterial != null) sr.sharedMaterial = art.spriteMaterial;
            return sr;
        }

        /// <summary>
        /// Bảng tên dưới chân vật trong phố: khung 9-slice viền đậm nền kem (chữ màu tối);
        /// thiếu hình thì dùng ô tối trong suốt như cũ (chữ màu sáng).
        /// </summary>
        public SpriteRenderer NewNamePlate(Transform parent, Vector2 center, Vector2 size, int order)
        {
            if (art.frameName == null)
            {
                var dark = NewSprite(parent, art.white, order, "LabelPlate");
                dark.color = new Color(0.15f, 0.09f, 0.14f, 0.8f);
                dark.transform.localPosition = new Vector3(center.x - size.x * 0.5f, center.y, 0f);
                dark.transform.localScale = new Vector3(size.x, size.y, 1f);
                return dark;
            }
            var plate = NewSprite(parent, art.frameName, order, "LabelPlate");
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.size = size;
            plate.color = Color.white;
            plate.transform.localPosition = new Vector3(center.x, center.y, 0f);
            return plate;
        }

        public TextMesh NewLabel(Transform parent, string text, int order, float size, Color color, string name = "Label")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Font.material;
            mr.sortingOrder = order;
            return tm;
        }
    }
}
