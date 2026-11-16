using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace Tami.EditorTools
{
    /// <summary>
    /// Dựng map thành object thật trong scene Main (đoạn phố, ô sạp, chỗ trang trí, sân khấu, cổng, biển)
    /// và các prefab Slot, DecorSpot, Stage, Stall để chỉnh trực tiếp trong Unity.
    /// </summary>
    public static partial class TamiBuilder
    {
        const string PrefabDir = Root + "/Prefabs";
        const string AtlasPath = Root + "/Art/Placeholder.spriteatlasv2";

        // Bố cục mặc định (đơn vị thế giới). Sau khi tạo, chỉnh trực tiếp trong scene.
        // Tỉ lệ chuẩn theo nhân vật chính (cao ~1,6 đơn vị): khách ~1,5; đồ trang trí ~1,2-2;
        // quầy, sân khấu, cổng cao ~4 (gấp 2,5 lần nhân vật).
        const float PlayerScale = 0.85f;
        const float StallScale = 1.33f;
        const float DecorScale = 1f;
        // Quầy hình thật rộng ~4-4,5 đơn vị prefab (5,4-6 đơn vị thế giới) nên ô sạp cách nhau 5,8; mỗi khu 3 quầy.
        public const int SlotsPerSegment = 3;
        const float StallSpacing = 5.8f;
        const float SegmentWidth = StallSpacing * SlotsPerSegment;
        const float StallBottomY = 0.4f;
        // Cổng to hơn quầy 30% (rộng ~8,3 đơn vị), đặt lùi trái để mép phải không đè lên quầy đầu tiên.
        const float GateX = -4.45f;
        const float GateScale = StallScale * 1.3f;
        // Khách đầu trái giờ đi qua cổng; điểm xa nhất bên trái lùi ra ngoài cổng để camera thấy trọn cổng.
        const float StreetExitX = -9.5f;
        const float CameraY = 0.12f;
        const float CameraSize = 6.6f;

        const string FontPath = Root + "/Fonts/Nunito-Bold.ttf";
        // Font của chữ đặt sẵn trong scene và prefab: Nunito, thiếu thì dùng font có sẵn của Unity.
        static Font LegacyFont => AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Đổi font mọi chữ đặt sẵn trong scene đang mở và các prefab sang font chung của game.</summary>
        [MenuItem("Tami/Áp dụng font chung cho scene và prefab")]
        public static void ApplyFontEverywhere()
        {
            var font = LegacyFont;
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(DataDir + "/GameArt.asset");
            if (art != null)
            {
                art.font = font;
                EditorUtility.SetDirty(art);
            }

            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                int n = SetFont(root.GetComponentsInChildren<TextMesh>(true), font);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                changed += n;
            }

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects()) changed += SetFont(go.GetComponentsInChildren<TextMesh>(true), font);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Tami] Đã đổi font " + font.name + " cho " + changed + " chữ.");
        }

        static int SetFont(TextMesh[] labels, Font font)
        {
            foreach (var tm in labels)
            {
                tm.font = font;
                tm.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                EditorUtility.SetDirty(tm);
            }
            return labels.Length;
        }

        // ---------- Prefab ----------

        static void BuildPrefabs(GameArt art, bool overwrite)
        {
            MakePrefab("Slot", overwrite, root =>
            {
                AddSprite(root, art, art.slotEmpty, 0);
                var col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(4f, 2.8f);
                col.offset = new Vector2(0f, 1.5f);
                root.AddComponent<StallSlot>();
                root.AddComponent<YSort>().isStatic = true;
                // Quầy dựng làm con của ô sạp nên phóng to theo.
                root.transform.localScale = Vector3.one * StallScale;
            });

            MakePrefab("DecorSpot", overwrite, root =>
            {
                root.transform.localScale = Vector3.one * DecorScale;
                AddSprite(root, art, null, 0);
                root.AddComponent<DecorSpot>();
                root.AddComponent<YSort>().isStatic = true;
            });

            MakePrefab("Stage", overwrite, root =>
            {
                AddSprite(root, art, art.stage, 0);
                var col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(4f, 3f);
                col.offset = new Vector2(0f, 1.5f);
                var stage = root.AddComponent<StageSpot>();
                stage.performer = AddSprite(Child(root, "Performer", new Vector3(0f, 0.95f, 0f)), art, null, 2);
                stage.label = AddLabel(Child(root, "Label", new Vector3(0f, -0.35f, 0f)), "Sân khấu", 3, 0.04f, Color.white);
                root.AddComponent<YSort>().isStatic = true;
                root.transform.localScale = Vector3.one * StallScale;
            });

            MakePrefab("Stall", overwrite, root =>
            {
                var stall = root.AddComponent<Stall>();
                var manager = AddSprite(Child(root, "Manager", new Vector3(0.75f, 0.8f, 0f)), art, art.manager, -1);
                manager.transform.localScale = Vector3.one * 0.9f;
                stall.managerSprite = manager;

                var body = AddSprite(Child(root, "Body", Vector3.zero), art, null, 0);
                var col = body.gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(4f, 3f);
                col.offset = new Vector2(0f, 1.5f);
                stall.body = body;

                // Biển tên gắn trên mặt trước quầy; khách và nhân vật đứng trước sẽ che lên như vật thật.
                var plate = AddSprite(Child(root, "NamePlate", new Vector3(-1.1f, 0.24f, 0f)), art, art.white, 5);
                plate.color = new Color(0.24f, 0.14f, 0.08f, 0.85f);
                plate.transform.localScale = new Vector3(2.2f, 0.4f, 1f);
                stall.nameLabel = AddLabel(Child(root, "Name", new Vector3(0f, 0.24f, 0f)), "Tên quầy", 6, 0.036f, Color.white);

                stall.barWidth = 1.6f;
                var bg = AddSprite(Child(root, "ProgressBg", new Vector3(-0.85f, 1.85f, 0f)), art, art.white, 3);
                bg.color = new Color(0f, 0f, 0f, 0.6f);
                bg.transform.localScale = new Vector3(1.7f, 0.22f, 1f);
                stall.progressBg = bg;
                var fill = AddSprite(Child(root, "ProgressFill", new Vector3(-0.8f, 1.85f, 0f)), art, art.white, 4);
                fill.color = new Color(0.45f, 0.9f, 0.4f);
                fill.transform.localScale = new Vector3(0f, 0.14f, 1f);
                stall.progressFill = fill;

                var bagGo = Child(root, "MoneyBag", new Vector3(0f, 2.6f, 0f));
                AddSprite(bagGo, art, art.moneyBag, 1000);
                var bagCol = bagGo.AddComponent<CircleCollider2D>();
                bagCol.radius = 0.6f;
                bagCol.offset = new Vector2(0f, 0.45f);
                stall.bag = bagGo.AddComponent<MoneyBag>();

                root.AddComponent<YSort>();
            });
        }

        static void MakePrefab(string name, bool overwrite, System.Action<GameObject> build)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            if (!overwrite && File.Exists(path)) return;
            var root = new GameObject(name);
            build(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static GameObject LoadPrefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");

        // ---------- Scene ----------

        static void BuildScene(GameConfig config, GameArt art)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            // Màn hình ngang: cao 13 đơn vị, đủ dãy sạp, đường đi và hàng trang trí giữa hai thanh giao diện.
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.09f, 0.18f);
            camGo.transform.position = new Vector3(0f, CameraY, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Physics2DRaycaster>();
            camGo.AddComponent<CameraScroller>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var street = BuildStreet(config, art);
            var player = CreatePlayer(art);

            var game = new GameObject("Game").AddComponent<Game>();
            game.config = config;
            game.art = art;
            game.street = street;
            game.player = player;

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
        }

        // ---------- Màn Loading ----------

        const string LoadingScenePath = SceneDir + "/Loading.unity";

        /// <summary>Danh sách cảnh khi build: màn Loading (nếu có) chạy trước, rồi tới Main.</summary>
        static string[] GameScenes() =>
            File.Exists(LoadingScenePath) ? new[] { LoadingScenePath, ScenePath } : new[] { ScenePath };

        static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in GameScenes()) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Dựng (hoặc dựng lại) cảnh Loading: đầu nhân vật chính làm biểu tượng đang tải, món ăn của các quầy bay ở nền.
        /// Đặt cảnh này đứng đầu danh sách build để game mở vào màn Loading rồi mới nạp Main.
        /// </summary>
        [MenuItem("Tami/Tạo màn Loading")]
        public static void BuildLoadingScene()
        {
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(DataDir + "/GameArt.asset");
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(DataDir + "/GameConfig.asset");
            if (art == null || config == null)
            {
                Debug.LogError("[Tami] Chạy Tami/Cập nhật hình tạm và dữ liệu trước khi tạo màn Loading.");
                return;
            }
            string previous = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.99f, 0.91f, 0.78f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var screen = new GameObject("Loading").AddComponent<LoadingScreen>();
            screen.nextScene = System.IO.Path.GetFileNameWithoutExtension(ScenePath);
            screen.head = art.playerHead;
            screen.font = art.font;
            var foods = new List<Sprite>();
            foreach (var stall in config.stalls)
            {
                if (stall == null || stall.dishSprites == null) continue;
                foreach (var dish in stall.dishSprites) if (dish != null) foods.Add(dish);
            }
            screen.foods = foods.ToArray();

            EditorSceneManager.SaveScene(scene, LoadingScenePath);
            SetBuildScenes();
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
            Debug.Log("[Tami] Đã tạo màn Loading với " + foods.Count + " món ăn: " + LoadingScenePath);
        }

        static StreetManager BuildStreet(GameConfig config, GameArt art)
        {
            var root = new GameObject("Street");
            var street = root.AddComponent<StreetManager>();
            street.exitX = StreetExitX;
            street.stallPrefab = LoadPrefab("Stall").GetComponent<Stall>();
            float totalWidth = SegmentWidth * config.segments.Length;

            // Các lớp map từ trên xuống: trời, cỏ, gạch (dãy quầy), đường, gạch. Khi chơi tự co giãn theo
            // số đoạn đã mở; trong Editor hiện đủ cả phố để dễ chỉnh.
            BuildLayers(street, art, totalWidth);
            street.decorRoot = Child(root, "Decorations", Vector3.zero).transform;

            var gate = Child(root, "Gate", new Vector3(GateX, StallBottomY, 0f));
            AddSprite(gate, art, art.gate, 0);
            var gateLabel = AddLabel(Child(gate, "Label", new Vector3(0f, 1.85f, 0f)), "HỘI CHỢ VIỆT - HÀN", 1, 0.042f, new Color(0.45f, 0.1f, 0.08f));
            gateLabel.fontStyle = FontStyle.Bold;
            gate.AddComponent<YSort>().isStatic = true;
            gate.transform.localScale = Vector3.one * GateScale;
            street.gate = gate.transform;

            var signGo = Child(root, "ExpansionSign", new Vector3(totalWidth + 1.9f, StallBottomY, 0f));
            AddSprite(signGo, art, art.expandSign, 0);
            var signCol = signGo.AddComponent<BoxCollider2D>();
            signCol.size = new Vector2(3f, 2f);
            signCol.offset = new Vector2(0f, 1f);
            var sign = signGo.AddComponent<ExpansionSign>();
            sign.label = AddLabel(Child(signGo, "Label", new Vector3(0f, 1.3f, 0f)), "MỞ RỘNG PHỐ", 1, 0.045f, new Color(0.3f, 0.15f, 0.05f));
            sign.label.fontStyle = FontStyle.Bold;
            signGo.AddComponent<YSort>();
            signGo.transform.localScale = Vector3.one * StallScale;
            street.sign = sign;

            var slotPrefab = LoadPrefab("Slot");
            var stagePrefab = LoadPrefab("Stage");
            var segments = new List<StreetSegment>();
            for (int s = 0; s < config.segments.Length; s++)
            {
                var info = config.segments[s];
                var segGo = Child(root, "Đoạn " + (s + 1) + " - " + info.name, new Vector3(s * SegmentWidth, 0f, 0f));
                var segment = segGo.AddComponent<StreetSegment>();
                segment.width = SegmentWidth;

                if (info.stageSegment)
                {
                    Instance(stagePrefab, segGo, "Stage 1", new Vector3(SegmentWidth * 0.25f, StallBottomY, 0f));
                    Instance(stagePrefab, segGo, "Stage 2", new Vector3(SegmentWidth * 0.75f, StallBottomY, 0f));
                }
                else
                {
                    for (int i = 0; i < SlotsPerSegment; i++)
                    {
                        Instance(slotPrefab, segGo, "Slot " + (i + 1), new Vector3(StallSpacing * (i + 0.5f), StallBottomY, 0f));
                    }
                }
                segment.CollectChildren();
                segments.Add(segment);
            }
            street.segments = segments.ToArray();
            return street;
        }

        /// <summary>Dựng (hoặc dựng lại) các lớp nền: vỉa hè phủ toàn map, đường, dải cỏ giữa hai đường, lớp tối phần chưa mở.</summary>
        static void BuildLayers(StreetManager street, GameArt art, float totalWidth)
        {
            var root = street.gameObject;
            foreach (var name in new[] { "Ground", "Road", "Grass", "Sky", "Locked" })
            {
                var old = street.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            float left = -20f, far = totalWidth + 25f;
            street.ground = Tiled(root, "Ground", art, art.ground, -32000, Color.white, left, far, street.bottomY, street.mapTopY);
            street.grass = Tiled(root, "Grass", art, art.grass, -31996, Color.white, left, far, street.lowerRoadTopY, street.roadBottomY);
            street.sky = null;
            street.road = Tiled(root, "Road", art, art.road, -31990, Color.white, left, totalWidth + 0.5f, street.roadBottomY, street.roadTopY);
            street.locked = Tiled(root, "Locked", art, art.ground, -31980, Color.black, totalWidth + 0.5f, far, street.bottomY, street.mapTopY);
            AddGroundClick(street.ground);
        }

        /// <summary>
        /// Chuyển scene đang mở sang map nhiều lớp và trang trí tự do mà không đụng tới ô sạp, sân khấu đã chỉnh:
        /// dựng lại các lớp nền, bỏ các chỗ trang trí cố định cũ, nối cổng và chỗ chứa trang trí.
        /// </summary>
        [MenuItem("Tami/Cập nhật map nhiều lớp (giữ ô sạp đã chỉnh)")]
        public static void UpgradeSceneToLayeredMap()
        {
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(DataDir + "/GameArt.asset");
            var street = Object.FindAnyObjectByType<StreetManager>();
            if (art == null || street == null)
            {
                Debug.LogError("[Tami] Cần mở scene Main và chạy Tami/Cập nhật hình tạm và dữ liệu trước.");
                return;
            }
            float totalWidth = 0f;
            foreach (var seg in street.segments) totalWidth = Mathf.Max(totalWidth, seg.RightX);
            BuildLayers(street, art, totalWidth);

            foreach (var spot in street.GetComponentsInChildren<DecorSpot>(true)) Object.DestroyImmediate(spot.gameObject);
            foreach (var seg in street.segments) seg.CollectChildren();
            if (street.decorRoot == null) street.decorRoot = Child(street.gameObject, "Decorations", Vector3.zero).transform;
            if (street.gate == null) street.gate = street.transform.Find("Gate");

            EditorUtility.SetDirty(street);
            EditorSceneManager.MarkSceneDirty(street.gameObject.scene);
            EditorSceneManager.SaveScene(street.gameObject.scene);
            Debug.Log("[Tami] Đã chuyển map sang nhiều lớp: trời, cỏ, gạch, đường, gạch.");
        }

        // ---------- Nhân vật chính ----------

        /// <summary>Thêm nhân vật chính và vùng chạm nền vào scene đang mở mà không đụng tới map đã chỉnh.</summary>
        [MenuItem("Tami/Thêm nhân vật chính vào scene")]
        public static void AddPlayerToScene()
        {
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(DataDir + "/GameArt.asset");
            var street = Object.FindAnyObjectByType<StreetManager>();
            var game = Object.FindAnyObjectByType<Game>();
            if (art == null || street == null || game == null)
            {
                Debug.LogError("[Tami] Cần mở scene Main (có Street và Game) và chạy Tami/Cập nhật hình tạm và dữ liệu trước.");
                return;
            }
            AddGroundClick(street.ground);
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) player = CreatePlayer(art);
            else BuildPlayerRig(player, art);
            game.player = player;
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("[Tami] Đã thêm nhân vật chính vào scene " + game.gameObject.scene.path);
        }

        static PlayerController CreatePlayer(GameArt art)
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(1.5f, -1.1f, 0f);
            root.transform.localScale = Vector3.one * PlayerScale;
            var player = root.AddComponent<PlayerController>();
            BuildPlayerRig(player, art);
            root.AddComponent<YSort>();
            return player;
        }

        // Các phần hình nhân vật chính: tên file, pivot (khớp nối), vị trí khớp so với chân, thứ tự vẽ.
        // Số đo lấy từ khung gốc 1069x1296 của hoạ sĩ, nhân vật cao 1,9 đơn vị.
        static readonly (string file, Vector2 pivot, Vector2 pos, int order)[] PlayerParts =
        {
            ("leg_right", new Vector2(0.500f, 0.927f), new Vector2(0.165f, 0.305f), 0),
            ("leg_left", new Vector2(0.490f, 0.925f), new Vector2(-0.142f, 0.338f), 1),
            ("body", new Vector2(0.496f, 0.031f), new Vector2(0.013f, 0.190f), 3),
            // Tay phải nằm sau thân (lộ túi áo), tay trái nằm trước thân như hình gốc.
            ("arm_right", new Vector2(0.136f, 0.858f), new Vector2(0.135f, 0.734f), 2),
            ("arm_left", new Vector2(0.767f, 0.888f), new Vector2(-0.294f, 0.701f), 4),
            ("head", new Vector2(0.506f, 0.042f), new Vector2(-0.013f, 0.684f), 5),
        };
        const float PlayerPartPpu = 181.9f;
        const string PlayerArtDir = Root + "/Art/Player";

        /// <summary>Đặt pivot và tỉ lệ cho các phần hình nhân vật chính trong Art/Player, gán vào GameArt.</summary>
        static void ImportPlayerParts(GameArt art)
        {
            var sprites = new Sprite[PlayerParts.Length];
            for (int i = 0; i < PlayerParts.Length; i++)
            {
                string path = PlayerArtDir + "/player_" + PlayerParts[i].file + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[Tami] Thiếu hình " + path);
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PlayerPartPpu;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = PlayerParts[i].pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            art.playerLegRight = sprites[0];
            art.playerLegLeft = sprites[1];
            art.playerBody = sprites[2];
            art.playerArmRight = sprites[3];
            art.playerArmLeft = sprites[4];
            art.playerHead = sprites[5];
            EditorUtility.SetDirty(art);
        }

        const string StallArtDir = Root + "/Art/Stalls";
        const string FoodArtDir = Root + "/Art/Food";

        // Manifest do ArtSource/Tools/cut_stall_sheets.py sinh ra khi cắt sheet sạp và sheet món.
        [System.Serializable] class StallArtManifest { public StallArtItem[] items; }
        [System.Serializable] class StallArtItem { public string id; public float ppu; public float counterTop; public float dishSpacing; public float dishScale; public StallArtLevel[] levels; }
        [System.Serializable] class StallArtLevel { public string file; public float[] pivot; }
        [System.Serializable] class FoodArtManifest { public FoodArtItem[] items; }
        [System.Serializable] class FoodArtItem { public string id; public FoodArtDish[] dishes; }
        [System.Serializable] class FoodArtDish { public string file; public float ppu; }

        /// <summary>Gán hình sạp 4 cấp và hình 3 món (nếu đã cắt) cho một quầy, thay hình tạm.</summary>
        static void ImportStallArt(StallData d)
        {
            string stallPath = StallArtDir + "/stall_art.json";
            if (File.Exists(stallPath))
            {
                var m = JsonUtility.FromJson<StallArtManifest>(File.ReadAllText(stallPath));
                foreach (var item in m.items)
                {
                    if (item.id != d.id) continue;
                    for (int i = 0; i < item.levels.Length && i < d.levelSprites.Length; i++)
                    {
                        var lv = item.levels[i];
                        var sprite = ImportSprite(StallArtDir + "/" + lv.file, item.ppu, new Vector2(lv.pivot[0], lv.pivot[1]));
                        if (sprite != null) d.levelSprites[i] = sprite;
                    }
                    d.counterTopY = item.counterTop;
                    if (item.dishSpacing > 0f) d.dishSpacing = item.dishSpacing;
                    if (item.dishScale > 0f) d.dishScale = item.dishScale;
                }
            }

            string foodPath = FoodArtDir + "/food_art.json";
            if (File.Exists(foodPath))
            {
                var m = JsonUtility.FromJson<FoodArtManifest>(File.ReadAllText(foodPath));
                foreach (var item in m.items)
                {
                    if (item.id != d.id) continue;
                    for (int i = 0; i < item.dishes.Length && i < d.dishSprites.Length; i++)
                    {
                        // Pivot gần đáy để món đặt lên mặt quầy.
                        d.dishSprites[i] = ImportSprite(FoodArtDir + "/" + item.dishes[i].file, item.dishes[i].ppu, new Vector2(0.5f, 0.08f));
                    }
                }
            }
        }

        const string DecorArtDir = Root + "/Art/Decor";

        // Manifest do ArtSource/Tools/import_decor.py sinh ra khi cắt sheet đồ trang trí.
        [System.Serializable] class DecorArtManifest { public DecorArtItem[] items; }
        [System.Serializable] class DecorArtItem { public string id; public string file; public float ppu; }

        /// <summary>Hình đồ trang trí đã cắt (neo ở giữa đáy hình = chân món đồ); chưa cắt thì trả null.</summary>
        static Sprite ImportDecorArt(string id)
        {
            string path = DecorArtDir + "/decor_art.json";
            if (!File.Exists(path)) return null;
            var m = JsonUtility.FromJson<DecorArtManifest>(File.ReadAllText(path));
            foreach (var item in m.items)
            {
                if (item.id == id) return ImportSprite(DecorArtDir + "/" + item.file, item.ppu, new Vector2(0.5f, 0f));
            }
            return null;
        }

        const string UiArtDir = Root + "/Art/UI";

        /// <summary>Nạp các khung giao diện 9-slice trong Art/UI (thiếu file thì giữ nguyên, giao diện dùng nền màu).</summary>
        static void ImportUiFrames(GameArt art)
        {
            art.dialogBox = ImportSliced(UiArtDir + "/dialog_box.png", 80);
            // Icon và nút của hoạ sĩ (ArtSource/Tools/import_ui.py) thay hình tạm; thiếu file thì giữ hình tạm.
            art.coin = ImportIcon("icon_coin") ?? art.coin;
            art.gem = ImportIcon("icon_gem") ?? art.gem;
            art.iconTrophy = ImportIcon("icon_trophy") ?? art.iconTrophy;
            art.iconSettings = ImportIcon("icon_settings") ?? art.iconSettings;
            art.tabManager = ImportIcon("btn_managers") ?? art.tabManager;
            art.tabDecor = ImportIcon("btn_decor") ?? art.tabDecor;
            art.tabShop = ImportIcon("btn_upgrade") ?? art.tabShop;
            art.diningTableVN = ImportDining("table_plastic");
            art.diningChairVN = ImportDining("chair_plastic");
            art.diningTableKR = ImportDining("table_wood");
            art.diningChairKR = ImportDining("chair_wood");
            art.diningTableLow = ImportDining("table_low");
            art.stoolRed = ImportDining("stool_red");
            art.stoolBlue = ImportDining("stool_blue");
            art.moneyBag = ImportBag("money_bag") ?? art.moneyBag;
            art.moneyBagFull = ImportBag("money_bag_full");
            art.speechBubble = ImportSliced(UiArtDir + "/speech_bubble.png", 26, 132f);
            // Khung giao diện 9-slice (ArtSource/Tools/make_ui_frames.py).
            art.uiPanel = ImportSliced(UiArtDir + "/frame_panel.png", 40);
            art.uiButton = ImportSliced(UiArtDir + "/frame_button.png", 32);
            art.uiRow = ImportSliced(UiArtDir + "/frame_row.png", 22);
            art.uiKnob = ImportIcon("frame_knob");
            // Bảng tên, khung thông báo, biển gỗ mở khoá, hàng rào (ArtSource/Tools/make_sign_fence.py).
            art.frameName = ImportSliced(UiArtDir + "/frame_name.png", 22, 140f);
            art.frameNotice = ImportSliced(UiArtDir + "/frame_notice.png", 34);
            if (File.Exists(UiArtDir + "/sign_wood.png")) art.expandSign = ImportSprite(UiArtDir + "/sign_wood.png", 128f, new Vector2(0.5f, 0f));
            art.fence = ImportMapTile("fence", 256f, true);
            art.fencePost = ImportMapTile("fence_post", 150f, true);
            art.speechTail = File.Exists(UiArtDir + "/speech_tail.png")
                ? ImportSprite(UiArtDir + "/speech_tail.png", 132f, new Vector2(0.5f, 1f)) : null;
            EditorUtility.SetDirty(art);
        }

        /// <summary>Nạp sprite 9-slice: 4 góc rộng `border` điểm ảnh không bị kéo giãn.</summary>
        const string MapArtDir = Root + "/Art/Map";

        /// <summary>Texture nền lát kín (gạch, đá đường): lưới FullRect để SpriteRenderer lát được.</summary>
        static Sprite ImportMapTile(string name, float ppu, bool bottomPivot = false)
        {
            string path = MapArtDir + "/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        const string DiningArtDir = Root + "/Art/Dining";

        /// <summary>Bàn ghế khu ăn uống (ArtSource/Tools/make_dining_art.py): neo ở đáy, 128 điểm ảnh mỗi đơn vị.</summary>
        static Sprite ImportDining(string name)
        {
            string path = DiningArtDir + "/" + name + ".png";
            return File.Exists(path) ? ImportSprite(path, 128f, new Vector2(0.5f, 0f)) : null;
        }

        /// <summary>Túi tiền trên mái quầy: neo ở đáy túi, 128 điểm ảnh mỗi đơn vị như hình tạm cũ.</summary>
        static Sprite ImportBag(string name)
        {
            string path = UiArtDir + "/" + name + ".png";
            return File.Exists(path) ? ImportSprite(path, 128f, new Vector2(0.5f, 0f)) : null;
        }

        static Sprite ImportIcon(string name)
        {
            string path = UiArtDir + "/" + name + ".png";
            return File.Exists(path) ? ImportSprite(path, 100f, new Vector2(0.5f, 0.5f)) : null;
        }

        static Sprite ImportSliced(string path, int border, float ppu = 100f)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = new Vector4(border, border, border, border);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite ImportSprite(string path, float ppu, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Tami] Thiếu hình " + path);
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        const string NpcArtDir = Root + "/Art/Npc";

        // Manifest do script chuẩn bị asset sinh ra (Art/Npc/npc_rigs.json): pivot và vị trí ghép từng phần.
        [System.Serializable] class NpcManifest { public float ppu; public NpcManifestRig[] rigs; }
        [System.Serializable] class NpcManifestRig { public string id; public string kind; public bool staff; public NpcManifestPart[] parts; }
        [System.Serializable] class NpcManifestPart { public string role; public string file; public float[] pivot; public float[] pos; public int order; }

        /// <summary>Nhập các bộ khách ghép từ nhiều phần trong Art/Npc vào GameArt.</summary>
        static void ImportNpcRigs(GameArt art)
        {
            string manifestPath = NpcArtDir + "/npc_rigs.json";
            if (!File.Exists(manifestPath))
            {
                art.npcRigs = new NpcRig[0];
                art.staffRigs = new NpcRig[0];
                return;
            }
            var manifest = JsonUtility.FromJson<NpcManifest>(File.ReadAllText(manifestPath));
            var rigs = new List<NpcRig>();
            var staff = new List<NpcRig>();
            foreach (var r in manifest.rigs)
            {
                var parts = new List<NpcRigPart>();
                foreach (var p in r.parts)
                {
                    string path = NpcArtDir + "/" + p.file;
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        Debug.LogWarning("[Tami] Thiếu hình " + path);
                        continue;
                    }
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = manifest.ppu;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(p.pivot[0], p.pivot[1]);
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    parts.Add(new NpcRigPart
                    {
                        role = p.role,
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                        position = new Vector2(p.pos[0], p.pos[1]),
                        order = p.order,
                    });
                }
                (r.staff ? staff : rigs).Add(new NpcRig { id = r.id, parts = parts.ToArray() });
            }
            art.npcRigs = rigs.ToArray();
            art.staffRigs = staff.ToArray();
            EditorUtility.SetDirty(art);
        }

        /// <summary>Dựng lại phần hình của nhân vật chính (giữ nguyên vị trí và thông số đã chỉnh).</summary>
        static void BuildPlayerRig(PlayerController player, GameArt art)
        {
            var root = player.gameObject;
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var ring = AddSprite(Child(root, "Ring", new Vector3(0f, 0.05f, 0f)), art, art.playerRing, -1);
            ring.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var visual = Child(root, "Visual", Vector3.zero);
            Sprite[] sprites = { art.playerLegRight, art.playerLegLeft, art.playerBody, art.playerArmRight, art.playerArmLeft, art.playerHead };
            var parts = new Transform[PlayerParts.Length];
            for (int i = 0; i < PlayerParts.Length; i++)
            {
                var (file, _, pos, order) = PlayerParts[i];
                var go = Child(visual, file, pos);
                AddSprite(go, art, sprites[i], order);
                parts[i] = go.transform;
            }
            player.visual = visual.transform;
            player.legRight = parts[0];
            player.legLeft = parts[1];
            player.body = parts[2];
            player.armRight = parts[3];
            player.armLeft = parts[4];
            player.head = parts[5];
            EditorUtility.SetDirty(player);
        }

        static void AddGroundClick(SpriteRenderer ground)
        {
            if (ground == null) return;
            var col = ground.GetComponent<BoxCollider2D>();
            if (col == null) col = ground.gameObject.AddComponent<BoxCollider2D>();
            col.size = ground.size;
            if (ground.GetComponent<GroundClick>() == null) ground.gameObject.AddComponent<GroundClick>();
            EditorUtility.SetDirty(ground.gameObject);
        }

        static GameObject Instance(GameObject prefab, GameObject parent, string name, Vector3 localPos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = name;
            go.transform.localPosition = localPos;
            return go;
        }

        static SpriteRenderer Tiled(GameObject parent, string name, GameArt art, Sprite sprite, int order, Color color,
            float x0, float x1, float y0, float y1)
        {
            var sr = AddSprite(Child(parent, name, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f)), art, sprite, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(x1 - x0, y1 - y0);
            sr.color = color;
            return sr;
        }

        static GameObject Child(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static SpriteRenderer AddSprite(GameObject go, GameArt art, Sprite sprite, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (art.spriteMaterial != null) sr.sharedMaterial = art.spriteMaterial;
            return sr;
        }

        static TextMesh AddLabel(GameObject go, string text, int order, float size, Color color)
        {
            var tm = go.AddComponent<TextMesh>();
            var font = LegacyFont;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        // ---------- Tối ưu cho điện thoại ----------

        /// <summary>Gom toàn bộ hình tạm vào một Sprite Atlas để giảm draw call; nén ASTC trên Android.</summary>
        static void BuildAtlas()
        {
            var atlas = new SpriteAtlasAsset();
            atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(ArtDir), AssetDatabase.LoadAssetAtPath<DefaultAsset>(PlayerArtDir), AssetDatabase.LoadAssetAtPath<DefaultAsset>(NpcArtDir),
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(StallArtDir), AssetDatabase.LoadAssetAtPath<DefaultAsset>(FoodArtDir),
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(DecorArtDir), AssetDatabase.LoadAssetAtPath<DefaultAsset>(UiArtDir),
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(DiningArtDir) });
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
            });
            importer.SaveAndReimport();        }

        /// <summary>Tắt HDR và khử răng cưa MSAA của URP: game 2D không cần, tiết kiệm GPU và pin.</summary>
        static void ConfigureRendering()
        {
            var assets = new HashSet<RenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline != null) assets.Add(GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.count; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (asset != null) assets.Add(asset);
            }
            foreach (var asset in assets)
            {
                if (asset is not UniversalRenderPipelineAsset urp) continue;
                urp.supportsHDR = false;
                urp.msaaSampleCount = 1;
                EditorUtility.SetDirty(urp);
            }
        }
    }
}
