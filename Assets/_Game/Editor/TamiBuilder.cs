using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Tami.EditorTools
{
    /// <summary>
    /// Menu Tami/Build MVP: sinh hình tạm, dữ liệu và nội dung theo GDD, scene Main, cấu hình Player.
    /// Chạy lại được nhiều lần; asset đã có sẽ được cập nhật thay vì tạo mới.
    /// </summary>
    public static partial class TamiBuilder
    {
        const string Root = "Assets/_Game";
        const string ArtDir = Root + "/Art/Placeholder";
        const string DataDir = Root + "/Data";
        const string StallDir = DataDir + "/Stalls";
        const string NpcDir = DataDir + "/NpcTypes";
        const string DecorDir = DataDir + "/Decorations";
        const string PerformerDir = DataDir + "/Performers";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/Main.unity";
        const float Ppu = 64f;

        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // 4 khu đầu chỉ bán đồ ăn, mỗi khu 3 quầy: id, tên, nước, đoạn phố, giá mở, tiền bán món 1 ở sạp cấp 1,
        // thời gian phục vụ, 3 món. Tạm thời: giá mở tăng đều ~4,3 lần mỗi quầy (200 xu tới 1,8B như bản cũ),
        // tiền bán nội suy theo bảng đã cân bằng trước đây, chờ logic tiền mới.
        static readonly (string id, string name, Origin origin, int segment, double cost, double income, float time, string p1, string p2, string p3)[] StallTable =
        {
            ("banh_chung", "Bánh chưng", Origin.VN, 1, 200, 50, 3f, "Bánh chưng", "Bánh tét", "Bánh chưng gấc"),
            ("tteokbokki", "Tteokbokki", Origin.KR, 1, 600, 140, 3f, "Tteokbokki", "Tteokbokki phô mai", "Rabokki"),
            ("banh_mi", "Bánh mì", Origin.VN, 1, 2500, 380, 3f, "Bánh mì pate", "Bánh mì thịt nướng", "Bánh mì đặc biệt"),
            ("kimbap", "Kimbap", Origin.KR, 2, 10000, 850, 3f, "Kimbap rau", "Kimbap cá ngừ", "Kimbap bulgogi"),
            ("pho", "Phở", Origin.VN, 2, 70000, 2500, 4f, "Phở gà", "Phở bò tái", "Phở đặc biệt"),
            ("naengmyeon", "Naengmyeon", Origin.KR, 2, 300000, 8000, 4f, "Mul naengmyeon", "Bibim naengmyeon", "Naengmyeon thịt bò"),
            ("ca_phe", "Cà phê", Origin.VN, 3, 1e6, 20000, 3f, "Cà phê phin", "Cà phê sữa đá", "Cà phê trứng"),
            ("bingsu", "Bingsu", Origin.KR, 3, 5.5e6, 85000, 3f, "Bingsu đậu đỏ", "Bingsu dâu", "Bingsu xoài"),
            ("bun_cha", "Bún chả", Origin.VN, 3, 25e6, 350000, 4f, "Bún chả", "Nem rán", "Bún chả đặc biệt"),
            ("samgyeopsal", "Samgyeopsal", Origin.KR, 4, 100e6, 1e6, 4f, "Samgyeopsal", "Galbi", "Set nướng thập cẩm"),
            ("banh_xeo", "Bánh xèo", Origin.VN, 4, 400e6, 4e6, 4f, "Bánh xèo tôm", "Bánh khọt", "Bánh xèo đặc biệt"),
            ("pajeon", "Pajeon", Origin.KR, 4, 1.8e9, 15e6, 4f, "Pajeon hành", "Haemul pajeon", "Kimchijeon"),
        };

        // Nhân viên (người bán + người chào hàng) của từng quầy, lấy từ Tami_npc/npc-banhang1..5.
        static string StaffFor(int stallIndex) => "npc_banhang" + (stallIndex % 5 + 1);

        // Người giới thiệu và câu giới thiệu khi dựng quầy; câu khách nói khi mua (GDD mục 6.1).
        static readonly Dictionary<string, (string speaker, string intro, string[] lines)> StallTalk = new Dictionary<string, (string, string, string[])>
        {
            { "banh_chung", ("mayor", "Bánh chưng gói lá dong, nhân đậu xanh thịt lợn. Ngày Tết không thể thiếu!", new[] { "Ngày Tết phải có bánh chưng!", "Gói lá dong thơm quá." }) },
            { "tteokbokki", ("mayor", "Tteokbokki là bánh gạo xào sốt cay ngọt, món ăn vặt đường phố của Hàn Quốc.", new[] { "Cay mà vẫn muốn ăn tiếp!", "Bánh gạo dẻo ghê." }) },
            { "banh_mi", ("mayor", "Bánh mì Việt Nam vỏ giòn, kẹp pate, thịt và rau thơm.", new[] { "Giòn rụm luôn!", "Vừa đi vừa ăn tiện ghê." }) },
            { "kimbap", ("mayor", "Kimbap là cơm cuộn rong biển, người Hàn hay mang theo khi đi dã ngoại.", new[] { "Mang đi picnic là tuyệt.", "Cơm cuộn đẹp quá." }) },
            { "pho", ("mayor", "Phở là món nước nổi tiếng nhất Việt Nam, ăn được cả ngày.", new[] { "Một bát phở nóng nào!", "Nước dùng thơm quá!" }) },
            { "naengmyeon", ("minji", "Naengmyeon là mì lạnh, người Hàn rất thích ăn vào mùa hè.", new[] { "Mì lạnh mát lạnh!", "Ăn mùa hè là nhất." }) },
            { "ca_phe", ("mayor", "Cà phê sữa đá pha phin là thức uống quen thuộc của người Việt.", new[] { "Cà phê phin đậm đà.", "Tỉnh cả người!" }) },
            { "bingsu", ("minji", "Bingsu là đá bào phủ trái cây hoặc đậu đỏ, món tráng miệng mùa hè.", new[] { "Trời nóng phải có đá.", "Đá bào mịn ghê." }) },
            { "bun_cha", ("mayor", "Bún chả Hà Nội có thịt nướng than, bún và nước chấm chua ngọt.", new[] { "Thịt nướng thơm cả phố!", "Nước chấm vừa miệng." }) },
            { "samgyeopsal", ("minji", "Samgyeopsal là thịt ba chỉ nướng, ăn kèm rau cuốn và kimchi.", new[] { "Cuốn rau ngon tuyệt.", "Thêm kimchi nữa nhé!" }) },
            { "banh_xeo", ("mayor", "Bánh xèo đổ trong chảo nóng kêu xèo xèo, nên mới có cái tên này.", new[] { "Giòn rụm, ăn lúc mưa.", "Nghe xèo xèo vui tai." }) },
            { "pajeon", ("minji", "Pajeon là bánh hành chiên, người Hàn hay ăn vào ngày mưa.", new[] { "Ngày mưa ăn pajeon thôi!", "Bánh hành thơm quá." }) },
        };

        // GDD mục 6.3: id, tên, nước, giá xu, giá Ngọc, độ hấp dẫn. Hình cắt từ sheet hoạ sĩ bằng ArtSource/Tools/import_decor.py.
        // Các cây trong DecorTable dùng làm hàng cây trên dải cỏ phía sau dãy quầy.
        static readonly string[] BackTreeIds = { "cay_bang", "cay_sau", "cay_xa_cu", "cay_sao", "cay_dau" };

        static readonly (string id, string name, Origin origin, double coins, int gems, int attraction)[] DecorTable =
        {
            ("hu_sanh", "Hũ sành", Origin.VN, 300, 0, 1),
            ("onggi", "Chum onggi", Origin.KR, 400, 0, 1),
            ("chum_nuoc", "Chum nước gáo dừa", Origin.VN, 500, 0, 1),
            ("banh_chung", "Bánh chưng", Origin.VN, 700, 0, 1),
            ("binh_men_ngoc", "Bình men ngọc", Origin.KR, 900, 0, 1),
            ("ban_tra_thap", "Bàn trà thấp", Origin.KR, 1200, 0, 1),
            ("mam_ngu_qua", "Mâm ngũ quả", Origin.VN, 1500, 0, 1),
            ("quat_buchae", "Quạt buchae", Origin.KR, 2000, 0, 1),
            ("den_long", "Đèn lồng đỏ", Origin.VN, 2500, 0, 2),
            ("den_long_xanh_do", "Đèn lồng cheongsachorong", Origin.KR, 3000, 0, 2),
            ("dun_rom", "Đụn rơm", Origin.VN, 4000, 0, 2),
            ("cay_dao", "Bình hoa đào", Origin.VN, 5000, 0, 2),
            ("den_ong_sao", "Đèn ông sao", Origin.VN, 6500, 0, 2),
            ("jangdokdae", "Hàng chum tương", Origin.KR, 8000, 0, 2),
            ("bo_tra_da", "Bộ trà đá", Origin.VN, 10000, 0, 2),
            ("neolttwigi", "Bập bênh neolttwigi", Origin.KR, 13000, 0, 2),
            ("non_la", "Giá nón lá", Origin.VN, 16000, 0, 3),
            ("ban_tra", "Bàn trà gốm", Origin.KR, 20000, 0, 3),
            ("quang_ganh", "Quang gánh tre", Origin.VN, 25000, 0, 3),
            ("thap_da", "Tháp đá cầu nguyện", Origin.KR, 32000, 0, 3),
            ("coi_da", "Cối xay đá", Origin.VN, 40000, 0, 3),
            ("cay_bang", "Cây bàng", Origin.VN, 45000, 0, 3),
            ("chuoi_den_long", "Chuỗi đèn lồng", Origin.KR, 50000, 0, 3),
            ("cay_sau", "Cây sấu", Origin.VN, 55000, 0, 3),
            ("ban_ghe_nhua", "Bàn ghế nhựa vỉa hè", Origin.VN, 65000, 0, 3),
            ("nguoi_tuyet", "Người tuyết", Origin.KR, 80000, 0, 3),
            ("cay_xa_cu", "Cây xà cừ", Origin.VN, 90000, 0, 4),
            ("cay_quat", "Chậu quất", Origin.VN, 100000, 0, 4),
            ("cay_sao", "Cây sao", Origin.VN, 110000, 0, 4),
            ("cay_dau", "Cây dầu", Origin.VN, 120000, 0, 4),
            ("trong_buk", "Trống buk", Origin.KR, 130000, 0, 4),
            ("ganh_hang_rong", "Gánh hàng rong", Origin.VN, 170000, 0, 4),
            ("jangseung", "Cột jangseung", Origin.KR, 220000, 0, 4),
            ("xe_tra_da", "Xe trà đá", Origin.VN, 280000, 0, 4),
            ("bang_hieu", "Bảng hiệu đèn", Origin.KR, 350000, 0, 4),
            ("nghe_da", "Nghê đá", Origin.VN, 450000, 0, 4),
            ("xe_bungeoppang", "Xe bánh cá bungeoppang", Origin.KR, 600000, 0, 4),
            ("trong_dong", "Trống đồng", Origin.VN, 800000, 0, 5),
            ("may_ban_hang", "Máy bán hàng tự động", Origin.KR, 1e6, 0, 5),
            ("o_che_nang", "Ô che nắng", Origin.VN, 1.3e6, 0, 5),
            ("pojangmacha", "Xe pojangmacha", Origin.KR, 1.7e6, 0, 5),
            ("xe_banh_mi", "Xe bánh mì", Origin.VN, 2.2e6, 0, 5),
            ("tram_xe_buyt", "Trạm xe buýt", Origin.KR, 2.8e6, 0, 5),
            ("sap_hoa_qua", "Sạp hoa quả", Origin.VN, 3.5e6, 0, 6),
            ("cay_thong", "Cây thông", Origin.KR, 4.5e6, 0, 6),
            ("xich_lo", "Xích lô", Origin.VN, 6e6, 0, 6),
            ("cay_phong", "Cây phong đỏ", Origin.KR, 8e6, 0, 6),
            ("cay_anh_dao", "Cây anh đào", Origin.KR, 10e6, 0, 6),
            ("jeongja", "Đình jeongja", Origin.KR, 13e6, 0, 6),
            ("thap_co", "Tháp cổ", Origin.VN, 0, 40, 7),
            ("thuy_dinh", "Thủy đình", Origin.VN, 0, 60, 8),
            ("cong_hanok", "Cổng hanok", Origin.KR, 0, 60, 8),
            ("cua_hang_tien_loi", "Cửa hàng tiện lợi", Origin.KR, 0, 80, 9),
        };

        // GDD mục 6.3 (biểu diễn): id, tên, nước, giá xu, thời gian, mô tả.
        static readonly (string id, string name, Origin origin, double cost, float duration, string description)[] PerformerTable =
        {
            ("mua_lan", "Múa lân", Origin.VN, 2e6, 60f, "Lân vàng nhảy theo tiếng trống, mang may mắn cho phố."),
            ("samulnori", "Samulnori", Origin.KR, 3e6, 60f, "Bốn nhạc cụ gõ truyền thống Hàn Quốc hoà tấu rộn ràng."),
            ("roi_nuoc", "Múa rối nước", Origin.VN, 8e6, 75f, "Những con rối gỗ kể chuyện trên mặt nước."),
            ("buchaechum", "Múa quạt Buchaechum", Origin.KR, 12e6, 75f, "Điệu múa quạt rực rỡ sắc màu của Hàn Quốc."),
            ("quan_ho", "Quan họ Bắc Ninh", Origin.VN, 30e6, 90f, "Liền anh liền chị hát đối đáp ngọt ngào."),
            ("pansori", "Pansori", Origin.KR, 45e6, 90f, "Nghệ thuật hát kể chuyện cùng tiếng trống buk."),
        };

        /// <summary>Cập nhật hình tạm, dữ liệu, cấu hình; giữ nguyên scene và các prefab đã có (an toàn với chỉnh sửa map).</summary>
        [MenuItem("Tami/Cập nhật hình tạm và dữ liệu")]
        public static void BuildContentMenu()
        {
            BuildContent(false, out _, out _);
            Debug.Log("[Tami] Đã cập nhật hình tạm, dữ liệu và cấu hình. Scene Main giữ nguyên.");
        }

        [MenuItem("Tami/Tạo lại scene Main (xoá chỉnh sửa map)")]
        public static void RebuildSceneMenu()
        {
            if (!EditorUtility.DisplayDialog("Tạo lại scene Main",
                    "Scene Main và các prefab Slot, DecorSpot, Stage, Stall sẽ được tạo lại từ đầu. Mọi chỉnh sửa map sẽ mất.",
                    "Tạo lại", "Huỷ")) return;
            BuildAll();
        }

        /// <summary>Dựng toàn bộ: hình tạm, dữ liệu, prefab (ghi đè), scene Main.</summary>
        public static void BuildAll()
        {
            BuildContent(true, out var config, out var art);
            BuildScene(config, art);
            AssetDatabase.SaveAssets();
            Debug.Log("[Tami] Đã dựng lại toàn bộ: " + ScenePath);
        }

        static void BuildContent(bool overwritePrefabs, out GameConfig config, out GameArt art)
        {
            foreach (var dir in new[] { ArtDir, StallDir, NpcDir, DecorDir, PerformerDir, SceneDir, PrefabDir }) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();

            art = BuildArt(out var stallSprites, out var markers);
            config = BuildData(stallSprites, markers);
            BuildAtlas();
            BuildPrefabs(art, overwritePrefabs);
            ConfigurePlayer();
            ConfigureRendering();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tami/Build Windows (chạy thử)")]
        public static void BuildWindowsMenu()
        {
            BuildWindows("Builds/Windows/Tami.exe");
        }

        /// <summary>Dùng với -batchmode -executeMethod: dựng toàn bộ rồi build bản Windows để chạy thử tự động.</summary>
        public static void BatchBuild()
        {
            int code = 1;
            try
            {
                BuildAll();
                string path = ArgValue("-tamiBuildPath") ?? "Builds/Windows/Tami.exe";
                code = BuildWindows(path) ? 0 : 1;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            EditorApplication.Exit(code);
        }

        static bool BuildWindows(string path)
        {
            var options = new BuildPlayerOptions
            {
                scenes = GameScenes(),
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[Tami] Build Windows: " + report.summary.result + " -> " + path);
            return report.summary.result == BuildResult.Succeeded;
        }

        static string ArgValue(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Tami";
            PlayerSettings.productName = "Hội Chợ Việt Hàn";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.runInBackground = true;
            // Màn hình ngang, cho phép xoay giữa hai chiều ngang.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Bản PC để chơi thử: cửa sổ ngang 16:9.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            // Icon game (mặc định cho mọi nền tảng): đầu nhân vật chính (ArtSource/Sheets/ui2/logo_head.png).
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Icon/app_icon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tami.hoichovietan");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.bundleVersionCode = 1;
            // Tối ưu điện thoại: nhịp khung hình ổn định, cắt bớt code không dùng để giảm dung lượng.
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
        }

        // ---------- Hình tạm ----------

        static GameArt BuildArt(out Dictionary<Origin, Sprite[]> stallSprites, out Dictionary<NpcKind, Sprite> markers)
        {
            var art = LoadOrCreate<GameArt>(DataDir + "/GameArt.asset");
            // Texture nền của hoạ sĩ (ArtSource/Tools/import_map.py); thiếu thì vẽ hình tạm.
            art.road = ImportMapTile("road_tile", 200.78431f) ?? Save(PlaceholderArt.Road(), "road", Center, Ppu);
            art.ground = ImportMapTile("ground_brick", 213.33333f) ?? Save(PlaceholderArt.Ground(), "ground", Center, Ppu);
            // Hàng cây phía sau dãy quầy dùng chung hình với các cây trang trí.
            var trees = new List<Sprite>();
            foreach (var id in BackTreeIds)
            {
                var tree = ImportDecorArt(id);
                if (tree != null) trees.Add(tree);
            }
            art.backTrees = trees.ToArray();
            // Bộ nền cay-co/duong-co (ArtSource/Tools/import_ground_tiles.py).
            art.grass = ImportMapTile("grass_tile", 170.66667f) ?? Save(PlaceholderArt.Grass(), "grass", Center, Ppu);
            art.sky = Save(PlaceholderArt.Sky(), "sky", Center, Ppu);
            art.slotEmpty = Save(PlaceholderArt.SlotEmpty(), "slot_empty", Bottom, Ppu);
            // Cổng chào và tán lá viền map (ArtSource/Tools/make_canopy_gate.py).
            art.gate = File.Exists(MapArtDir + "/gate_arch.png")
                ? ImportSprite(MapArtDir + "/gate_arch.png", 200f, new Vector2(0.5f, 0f))
                : Save(PlaceholderArt.Gate(), "gate", Bottom, Ppu);
            if (File.Exists(MapArtDir + "/canopy.png")) art.canopy = ImportSprite(MapArtDir + "/canopy.png", 106.66667f, new Vector2(0.5f, 1f));
            art.expandSign = Save(PlaceholderArt.ExpandSign(), "expand_sign", Bottom, Ppu);
            art.stage = Save(PlaceholderArt.Stage(), "stage", Bottom, Ppu);

            art.npcBodies = new Sprite[6];
            for (int i = 0; i < art.npcBodies.Length; i++) art.npcBodies[i] = Save(PlaceholderArt.Npc(i), "npc_" + i, Bottom, Ppu);
            art.manager = Save(PlaceholderArt.Npc(6), "npc_manager", Bottom, Ppu);
            art.playerRing = Save(PlaceholderArt.PlayerRing(), "player_ring", Center, Ppu);
            ImportPlayerParts(art);
            ImportNpcRigs(art);
            art.portraitMayor = Save(PlaceholderArt.Portrait("mayor"), "portrait_mayor", Center, Ppu);
            art.portraitMinji = Save(PlaceholderArt.Portrait("minji"), "portrait_minji", Center, Ppu);
            art.portraitOngBa = Save(PlaceholderArt.Portrait("ongba"), "portrait_ongba", Center, Ppu);
            // Chân dung nhân vật chính dùng luôn phần đầu do hoạ sĩ vẽ.
            art.portraitPlayer = art.playerHead != null ? art.playerHead : Save(PlaceholderArt.Portrait("player"), "portrait_player", Center, Ppu);

            art.coin = Save(PlaceholderArt.Coin(), "coin", Center, Ppu);
            art.gem = Save(PlaceholderArt.Gem(), "gem", Center, Ppu);
            art.moneyBag = Save(PlaceholderArt.MoneyBag(), "money_bag", Bottom, Ppu);
            art.bubbleSad = Save(PlaceholderArt.BubbleSad(), "bubble_sad", Bottom, Ppu);
            art.bubbleBuy = Save(PlaceholderArt.BubbleBuy(), "bubble_buy", Bottom, Ppu);
            art.arrow = Save(PlaceholderArt.Arrow(), "arrow", Bottom, Ppu);
            art.iconTrophy = Save(PlaceholderArt.IconTrophy(), "icon_trophy", Center, Ppu);
            art.iconSettings = Save(PlaceholderArt.IconSettings(), "icon_settings", Center, Ppu);
            art.iconGift = Save(PlaceholderArt.IconGift(), "icon_gift", Center, Ppu);
            art.tabStreet = Save(PlaceholderArt.TabStreet(), "tab_street", Center, Ppu);
            art.tabManager = Save(PlaceholderArt.TabManager(), "tab_manager", Center, Ppu);
            art.tabDecor = Save(PlaceholderArt.TabDecor(), "tab_decor", Center, Ppu);
            art.tabShop = Save(PlaceholderArt.TabShop(), "tab_shop", Center, Ppu);
            art.white = Save(PlaceholderArt.White(), "white", new Vector2(0f, 0.5f), 8f);

            stallSprites = new Dictionary<Origin, Sprite[]>();
            foreach (Origin origin in System.Enum.GetValues(typeof(Origin)))
            {
                var sprites = new Sprite[3];
                for (int level = 1; level <= 3; level++)
                {
                    sprites[level - 1] = Save(PlaceholderArt.Stall(origin, level), "stall_" + origin.ToString().ToLower() + "_" + level, Bottom, Ppu);
                }
                stallSprites[origin] = sprites;
            }

            markers = new Dictionary<NpcKind, Sprite>
            {
                { NpcKind.TourGroup, Save(PlaceholderArt.MarkerFlag(), "marker_tour", Bottom, Ppu) },
                { NpcKind.FoodBlogger, Save(PlaceholderArt.MarkerCamera(), "marker_blogger", Bottom, Ppu) },
                { NpcKind.BigSpender, Save(PlaceholderArt.MarkerCrown(), "marker_big_spender", Bottom, Ppu) },
                { NpcKind.Mystery, Save(PlaceholderArt.MarkerMystery(), "marker_mystery", Bottom, Ppu) },
            };

            string matPath = Root + "/Art/SpriteUnlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            art.spriteMaterial = mat;
            art.font = LegacyFont;
            ImportUiFrames(art);

            EditorUtility.SetDirty(art);
            return art;
        }

        static Sprite Save(Px px, string name, Vector2 pivot, float ppu)
        {
            string path = ArtDir + "/" + name + ".png";
            var tex = px.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu * px.Scale;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Câu người chào hàng của từng quầy (quầy chưa có thì dùng câu chung).
        static readonly Dictionary<string, string[]> BarkerTalk = new Dictionary<string, string[]>
        {
            { "banh_chung", new[] { "Bánh chưng mới luộc, nóng hổi đây!", "Bánh tét dẻo thơm, mời ghé!", "Bánh chưng gấc đỏ may mắn nè!" } },
        };

        // ---------- Dữ liệu ----------

        static GameConfig BuildData(Dictionary<Origin, Sprite[]> stallSprites, Dictionary<NpcKind, Sprite> markers)
        {
            var stalls = new List<StallData>();
            for (int i = 0; i < StallTable.Length; i++)
            {
                var row = StallTable[i];
                var d = LoadOrCreate<StallData>(StallDir + "/" + (i + 1).ToString("00") + "_" + row.id + ".asset");
                d.id = row.id;
                d.displayName = row.name;
                d.origin = row.origin;
                d.segment = row.segment;
                d.unlockCost = row.cost;
                d.baseIncome = row.income;
                d.baseServiceTime = row.time;
                d.products = new[] { row.p1, row.p2, row.p3 };
                // Hình tạm có 3 kiểu mái; cấp 4 dùng lại kiểu cấp 3 cho tới khi có hình thật.
                var placeholder = stallSprites[row.origin];
                d.levelSprites = new[] { placeholder[0], placeholder[1], placeholder[2], placeholder[2] };
                d.dishSprites = new Sprite[3];
                d.counterTopY = 1.6f;
                d.dishSpacing = 0.72f;
                d.dishScale = 0.8f;
                ImportStallArt(d);
                var talk = StallTalk[row.id];
                d.introSpeaker = talk.speaker;
                d.introLine = talk.intro;
                d.customerLines = talk.lines;
                d.barkerLines = BarkerTalk.TryGetValue(row.id, out var barkerLines) ? barkerLines : null;
                d.staffRig = StaffFor(i);
                EditorUtility.SetDirty(d);
                stalls.Add(d);
            }

            // GDD mục 4.5
            var npcTypes = new List<NpcTypeData>
            {
                Npc("normal", "Khách thường", NpcKind.Normal, 85f, 1, 5, 1f, 1, 1, 1, null),
                Npc("tour_group", "Đoàn du lịch", NpcKind.TourGroup, 5f, 1, 3, 1f, 4, 6, 2, markers[NpcKind.TourGroup]),
                Npc("food_blogger", "Food blogger", NpcKind.FoodBlogger, 4f, 1, 99, 1f, 1, 1, 2, markers[NpcKind.FoodBlogger]),
                Npc("big_spender", "Khách sộp", NpcKind.BigSpender, 4f, 1, 2, 5f, 1, 1, 2, markers[NpcKind.BigSpender]),
                Npc("mystery", "Khách bí ẩn", NpcKind.Mystery, 2f, 0, 0, 1f, 1, 1, 2, markers[NpcKind.Mystery]),
            };

            var decorations = new List<DecorationData>();
            foreach (var row in DecorTable)
            {
                var d = LoadOrCreate<DecorationData>(DecorDir + "/" + row.id + ".asset");
                d.id = row.id;
                d.displayName = row.name;
                d.origin = row.origin;
                d.coinCost = row.coins;
                d.gemCost = row.gems;
                d.attraction = row.attraction;
                d.sprite = ImportDecorArt(row.id) ?? Save(PlaceholderArt.Decoration(row.id), "decor_" + row.id, Bottom, Ppu);
                EditorUtility.SetDirty(d);
                decorations.Add(d);
            }

            var performers = new List<PerformerData>();
            foreach (var row in PerformerTable)
            {
                var p = LoadOrCreate<PerformerData>(PerformerDir + "/" + row.id + ".asset");
                p.id = row.id;
                p.displayName = row.name;
                p.origin = row.origin;
                p.coinCost = row.cost;
                p.duration = row.duration;
                p.description = row.description;
                p.sprite = Save(PlaceholderArt.Performer(row.id), "performer_" + row.id, Bottom, Ppu);
                EditorUtility.SetDirty(p);
                performers.Add(p);
            }

            var config = LoadOrCreate<GameConfig>(DataDir + "/GameConfig.asset");
            // Đầu game kiếm tiền nhanh: vốn mở được ngay 2 quầy đầu, khách đông hơn.
            config.startingCoins = 1500;
            config.startingGems = 0;
            config.baseSpawnPerMinute = 26f;
            config.baseMaxCustomers = 10;
            // 4 nâng cấp mỗi quầy. Tạm giữ nhịp tiền của bản cũ: 3 món giá x1 / x2,5 / x6, giá mở x4 / x15;
            // sạp 4 cấp nhanh hơn, quỹ lớn hơn, giá bán nhích dần; doanh thu +25% mỗi cấp.
            config.levels = new[]
            {
                new StallLevel { upgradeCostMultiplier = 1f, incomeMultiplier = 1f, serviceTimeMultiplier = 1f, vaultCapacitySales = 30 },
                new StallLevel { upgradeCostMultiplier = 3f, incomeMultiplier = 1.25f, serviceTimeMultiplier = 0.85f, vaultCapacitySales = 45 },
                new StallLevel { upgradeCostMultiplier = 10f, incomeMultiplier = 1.6f, serviceTimeMultiplier = 0.7f, vaultCapacitySales = 70 },
                new StallLevel { upgradeCostMultiplier = 30f, incomeMultiplier = 2f, serviceTimeMultiplier = 0.55f, vaultCapacitySales = 100 },
            };
            config.dishPriceMultipliers = new[] { 1f, 2.5f, 6f };
            config.dishUnlockCostMultipliers = new[] { 0f, 4f, 15f };
            config.revenueMaxLevel = 10;
            config.revenuePerLevel = 0.25f;
            config.revenueCostMultiplier = 1.5f;
            config.revenueCostGrowth = 1.9f;
            config.barkerCostMultiplier = 3f;
            config.barkerInviteChance = 0.4f;
            config.barkerTalkInterval = 6f;
            config.segments = new[]
            {
                new SegmentInfo { name = "Cổng làng - Cổng hanok", unlockCost = 0 },
                new SegmentInfo
                {
                    name = "Phố đèn lồng", unlockCost = 8000,
                    dialogue = new[]
                    {
                        new DialogueLine("mayor", "Phố đèn lồng đã sáng! Từ giờ sẽ có cả đoàn du lịch, food blogger và khách sộp ghé thăm."),
                        new DialogueLine("minji", "Chào anh chị, em là Min-ji, du học sinh Hàn Quốc. Em sẽ giới thiệu các món Hàn ở đây nhé!"),
                    },
                },
                new SegmentInfo
                {
                    name = "Phố cà phê và tráng miệng", unlockCost = 1.2e6,
                    dialogue = new[] { new DialogueLine("mayor", "Phố cà phê và tráng miệng mở cửa rồi. Ăn no thì phải có cà phê, đá bào chứ!") },
                },
                new SegmentInfo
                {
                    name = "Phố đồ nướng", unlockCost = 20e6,
                    dialogue = new[] { new DialogueLine("minji", "Phố đồ nướng! Thịt nướng Hàn, bánh xèo Việt, bánh hành ngày mưa, thơm lừng cả phố.") },
                },
                new SegmentInfo
                {
                    name = "Quảng trường sân khấu", unlockCost = 6e9, stageSegment = true,
                    dialogue = new[]
                    {
                        new DialogueLine("mayor", "Phố đã đủ 5 đoạn! Chúng ta tổ chức lễ khai trương toàn phố nào!"),
                        new DialogueLine("ongba", "Chào cháu, ta là Ông Ba. Quảng trường này cần những tiết mục biểu diễn."),
                        new DialogueLine("ongba", "Hãy mời nghệ sĩ rồi bấm vào sân khấu để biểu diễn, khách sẽ kéo đến đông gấp đôi!"),
                    },
                },
            };
            // Mở đầu: nhân vật chính kể về mình, xin phép trưởng phố mở hội chợ và được đồng ý.
            config.introLines = new[]
            {
                new DialogueLine("", "Một buổi chiều, ở đầu một con phố nhỏ còn vắng vẻ..."),
                new DialogueLine("player", "Xin chào! Mình sinh ra ở Việt Nam, lớn lên giữa mùi bánh chưng ngày Tết và tiếng rao ngoài chợ."),
                new DialogueLine("player", "Lớn hơn một chút, mình đeo ba lô chu du khắp đất nước: từ phố cổ Hà Nội, phố Hội An tới chợ nổi miền Tây."),
                new DialogueLine("player", "Rồi mình có cơ hội sang Hàn Quốc làm việc. Ở đó mình mê tteokbokki, kimbap và những khu chợ đêm rộn ràng."),
                new DialogueLine("player", "Giờ mình trở về Việt Nam, mang theo tình yêu với văn hoá của cả hai đất nước."),
                new DialogueLine("player", "Mình muốn mở một hội chợ, nơi văn hoá Việt Nam và Hàn Quốc gặp nhau!"),
                new DialogueLine("player", "Chào bác Trưởng phố! Cháu xin phép được mở một hội chợ văn hoá Việt - Hàn ở con phố này ạ."),
                new DialogueLine("mayor", "Hội chợ Việt - Hàn à? Nghe hay đấy! Con phố này vắng vẻ lâu rồi, có hội chợ thì vui biết mấy."),
                new DialogueLine("mayor", "Được, bác đồng ý! Đây là giấy phép mở hội chợ của cháu."),
                new DialogueLine("mayor", "Cháu có 500 xu, đủ dựng quầy đầu tiên. Cứ bắt đầu từ từ rồi phát triển cả khu phố nhé!"),
                new DialogueLine("player", "Cảm ơn bác! Hội chợ Việt - Hàn, bắt đầu thôi!"),
            };
            config.stalls = stalls.ToArray();
            config.npcTypes = npcTypes.ToArray();
            config.decorations = decorations.ToArray();
            config.performers = performers.ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        static NpcTypeData Npc(string id, string name, NpcKind kind, float weight, int minStalls, int maxStalls,
            float pay, int groupMin, int groupMax, int unlockSegment, Sprite marker)
        {
            var t = LoadOrCreate<NpcTypeData>(NpcDir + "/" + id + ".asset");
            t.id = id;
            t.displayName = name;
            t.kind = kind;
            t.spawnWeight = weight;
            t.minStalls = minStalls;
            t.maxStalls = maxStalls;
            t.payMultiplier = pay;
            t.groupSizeMin = groupMin;
            t.groupSizeMax = groupMax;
            t.unlockSegment = unlockSegment;
            t.marker = marker;
            EditorUtility.SetDirty(t);
            return t;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
    }
}
