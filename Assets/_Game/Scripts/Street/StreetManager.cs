using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Con phố đặt sẵn trong scene (object "Street"): các đoạn phố, ô sạp, sân khấu, cổng và biển mở rộng
    /// đều chỉnh trực tiếp trong Unity. Khi chơi, đoạn chưa mở bị ẩn cho tới khi người chơi mở rộng phố.
    /// Map chia lớp từ trên xuống: trời, cỏ/cây, gạch (dãy quầy), đường đi (khách đi), gạch.
    /// Nhân vật chính đi tự do trên cỏ, gạch và đường; đồ trang trí đặt tự do trên hai vùng gạch.
    /// </summary>
    public class StreetManager : MonoBehaviour
    {
        [Header("Đoạn phố (theo thứ tự mở, khớp GameConfig.segments)")]
        public StreetSegment[] segments = new StreetSegment[0];

        [Header("Các lớp map (tự co giãn theo số đoạn đã mở)")]
        [Tooltip("Vỉa hè đá phủ từ đáy tới đỉnh map; nhận chạm để nhân vật đi tới")]
        public SpriteRenderer ground;
        public SpriteRenderer road;
        [Tooltip("Dải cỏ giữa đường trên và đường dưới")]
        public SpriteRenderer grass;
        [Tooltip("Lớp trời cũ phía trên map: không dùng nữa, tự ẩn khi chơi")]
        public SpriteRenderer sky;
        [Tooltip("Lớp tối phủ phần phố chưa mở")]
        public SpriteRenderer locked;
        public ExpansionSign sign;
        [Tooltip("Cổng đầu phố (không đặt trang trí đè lên)")]
        public Transform gate;
        [Tooltip("Nơi chứa đồ trang trí người chơi đặt")]
        public Transform decorRoot;

        [Header("Ranh giới các lớp theo chiều dọc")]
        [Tooltip("Đáy map: phía dưới vỉa hè còn khu ăn uống và khu trò chơi dân gian")]
        public float bottomY = -13f;
        [Tooltip("Mép trên vùng đặt trang trí phía sau dãy quầy")]
        public float brickTopY = 5f;
        [Tooltip("Đỉnh map (vỉa hè phủ tới đây, vừa qua nóc quầy cao nhất); không còn cỏ, cây, trời phía trên")]
        public float mapTopY = 6.6f;

        [Header("Đường đi của khách")]
        public float laneMinY = -2.0f;
        public float laneMaxY = -1.3f;
        [Tooltip("Điểm khách xuất hiện và rời phố (bên trái cổng)")]
        public float exitX = -9.5f;
        public float roadTopY = 0.15f;
        public float roadBottomY = -2.4f;
        /// <summary>Chỗ nhân vật chính đứng trước quầy để tương tác.</summary>
        public float StandY => roadTopY - 0.75f;

        [Header("Đường dưới và các khu dọc đường dưới")]
        [Tooltip("Mép trên đường dưới; giữa hai đường là vỉa hè đặt trang trí")]
        public float lowerRoadTopY = -4.95f;
        public float lowerRoadBottomY = -7.5f;
        [Tooltip("Chân bàn ăn và trò chơi dân gian, dưới đường dưới")]
        public float zoneBaseY = -9.6f;
        [Tooltip("Chân sân khấu ở khu biểu diễn")]
        public float stageBaseY = -11.6f;

        [Header("Prefab")]
        public Stall stallPrefab;

        // Lớp đen phủ phần chưa mở vẽ trên mọi thứ của con phố (cả khách đi ra vào ở mép phải),
        // chỉ dưới chữ và icon bay lên (29000+).
        const int LockedOrder = 20000;
        // Biển mở rộng đứng ở right + 1,9 và rộng 3 đơn vị: lớp đen bắt đầu ngay sau biển.
        const float LockedStartOffset = 3.6f;

        // Lối đi dọc nối đường trên với đường dưới: rộng đúng 2 viên đá (mỗi viên 0,6375 đơn vị).
        const float ConnectorWidth = 4 * Stone;
        // Một viên đá lát đường (texture 8 viên mỗi 5,1 đơn vị): lối đi dọc đặt khớp lưới viên của hai đường.
        const float Stone = 0.6375f;
        SpriteRenderer lowerRoad;
        // Lối đi dọc ở cuối mỗi đoạn phố (sau mỗi 3 gian hàng), kể cả đoạn chưa mở (bị lớp tối che).
        readonly List<SpriteRenderer> connectors = new List<SpriteRenderer>();
        // Lối đá xám từ mép trên map xuống qua lòng cổng tới đường trên (khách đi vào/ra phố theo lối này).
        SpriteRenderer gatePath;
        const float GatePathWidth = 6 * Stone;
        Transform plazaRoot;

        /// <summary>Các khu dọc đường dưới theo thứ tự mở; khu thứ i nằm dưới đoạn phố thứ i.</summary>
        public enum PlazaZoneKind { Dining, FolkGames, Stage }
        static readonly PlazaZoneKind[] ZoneKinds = { PlazaZoneKind.Dining, PlazaZoneKind.FolkGames, PlazaZoneKind.Dining, PlazaZoneKind.Stage };
        readonly List<PlazaZoneSign> zoneSigns = new List<PlazaZoneSign>();
        // Sân khấu đặt sẵn ở đoạn phố chưa chơi được (quảng trường cũ) được chuyển xuống khu biểu diễn.
        readonly List<StageSpot> spareStages = new List<StageSpot>();

        public int PlazaZonesUnlocked { get; private set; }
        public int PlazaZoneCount => Mathf.Min(ZoneKinds.Length, Mathf.Min(Cfg.plazaZoneCosts.Length, segments.Length));
        public PlazaZoneKind ZoneKind(int zone) => ZoneKinds[zone];
        public double ZoneCost(int zone) => Cfg.plazaZoneCosts[zone];
        public string ZoneName(int zone) =>
            ZoneKinds[zone] == PlazaZoneKind.Dining ? "Khu ăn uống" : ZoneKinds[zone] == PlazaZoneKind.FolkGames ? "Khu trò chơi dân gian" : "Khu biểu diễn";
        /// <summary>Khu mở lần lượt, và chỉ mở được khi đoạn phố ngay phía trên đã mở.</summary>
        public bool CanUnlockZone(int zone) => zone == PlazaZonesUnlocked && zone < PlazaZoneCount && zone < UnlockedSegments;
        /// <summary>Lối đi dọc ở cuối đoạn phố đầu.</summary>
        /// <summary>Hai con đường chạy hết map (cả phần chưa mở) nên khung cố định, lưới viên đá không xê dịch khi mở phố.</summary>
        float RoadLeft => exitX - 12f;
        // Bề dài đường là bội số của 2 viên đá: lát hình tính từ tâm hay từ mép vùng thì lưới viên vẫn trùng nhau,
        // nên lối đi dọc (rộng 4 viên, cao 4 viên) luôn khớp với hai con đường.
        float RoadRight
        {
            get
            {
                float raw = (segments.Length > 0 ? segments[segments.Length - 1].RightX : 60f) + 25f;
                return RoadLeft + Mathf.Ceil((raw - RoadLeft) / (2f * Stone)) * (2f * Stone);
            }
        }

        /// <summary>Lối đi dọc ở cuối đoạn phố thứ i, dời về đường kẻ gần nhất của lưới viên đá.</summary>
        float ConnectorXAt(int segment)
        {
            float origin = (RoadLeft + RoadRight) * 0.5f;
            return origin + Mathf.Round((segments[segment].RightX - origin) / Stone) * Stone;
        }

        /// <summary>Lối đi dọc gần điểm x nhất trong phần phố đã mở.</summary>
        public float NearestConnectorX(float x)
        {
            float best = ConnectorXAt(0);
            for (int i = 1; i < UnlockedSegments; i++)
            {
                float c = ConnectorXAt(i);
                if (Mathf.Abs(c - x) < Mathf.Abs(best - x)) best = c;
            }
            return best;
        }
        /// <summary>Làn đi của khách trên đường dưới (mỗi khách một độ cao để không đi chồng lên nhau).</summary>
        public float RandomLowerLaneY => Random.Range(lowerRoadBottomY + 0.8f, lowerRoadTopY - 0.9f);

        // Phần map bị thanh giao diện trên/dưới che (đơn vị thế giới) để camera vẫn thấy được mép map.
        const float TopUiCover = 1.35f;
        const float BottomUiCover = 2.3f;

        public int UnlockedSegments { get; private set; }
        public readonly List<StallSlot> Slots = new List<StallSlot>();
        public readonly List<Stall> Stalls = new List<Stall>();
        public readonly List<PlacedDecor> Decorations = new List<PlacedDecor>();
        public readonly List<StageSpot> Stages = new List<StageSpot>();
        public readonly List<DiningTable> Tables = new List<DiningTable>();
        public readonly List<FolkGameStation> FolkGames = new List<FolkGameStation>();

        /// <summary>Thu nhập hiện tại của cả phố (xu mỗi phút), tính từ tốc độ bán của các quầy.</summary>
        public double IncomePerMinute
        {
            get
            {
                double perSecond = 0;
                foreach (var s in Stalls) perSecond += s.IncomeRate;
                return perSecond * 60;
            }
        }

        // Vùng nhân vật chính đi được: từ đáy gạch dưới tới hết lớp cỏ, trong phần phố đã mở.
        public float WalkMinX => exitX + 1f;
        public float WalkMaxX => RightX + 1f;
        public float WalkMinY => bottomY + 0.4f;
        public float WalkMaxY => brickTopY;

        public event System.Action OnChanged;
        public event System.Action<Stall> OnStallBuilt;
        public event System.Action<SegmentInfo> OnSegmentUnlocked;

        int SegmentCount => Mathf.Min(segments.Length, Cfg.segments.Length);
        /// <summary>Số đoạn phố mở được trong bản này; các đoạn sau để dành cho bản cập nhật.</summary>
        public int PlayableSegments => Mathf.Clamp(Cfg.playableSegments, 1, SegmentCount);
        /// <summary>Mép phải của đoạn phố cuối cùng đã mở.</summary>
        public float RightX => UnlockedSegments > 0 ? segments[UnlockedSegments - 1].RightX : 0f;
        public bool CanExpand => UnlockedSegments < PlayableSegments;
        /// <summary>Đã mở hết phần chơi được nhưng phố còn đoạn sau (sẽ có trong bản cập nhật).</summary>
        public bool ComingSoon => !CanExpand && UnlockedSegments < SegmentCount;
        public SegmentInfo NextSegment => CanExpand ? Cfg.segments[UnlockedSegments] : null;
        public bool HasEmptySlot => Slots.Any(s => s.Stall == null);
        public int DecorationCount => Decorations.Count;
        /// <summary>Số đồ trang trí tối đa: tăng theo số đoạn phố đã mở.</summary>
        public int DecorCapacity => Cfg.decorPerSegment * UnlockedSegments;
        public int DecorSlotsLeft => Mathf.Max(0, DecorCapacity - Decorations.Count);
        public bool PerformActive => Stages.Any(s => s.IsPerforming);

        public int MaxCustomers
        {
            get
            {
                int max = Cfg.baseMaxCustomers + (UnlockedSegments - 1) * Cfg.customersPerSegment;
                return PerformActive ? Mathf.RoundToInt(max * (1f + Cfg.performMaxCustomersBonus)) : max;
            }
        }

        public int Attraction
        {
            get
            {
                int total = Stages.Count * Cfg.attractionPerStage;
                foreach (var s in Stalls) total += Cfg.attractionPerStall + Cfg.attractionPerLevel * (s.Level - 1);
                foreach (var d in Decorations) total += d.Data.attraction;
                return total;
            }
        }

        GameConfig Cfg => Game.I.config;
        CameraScroller scroller;

        public void Build()
        {
            var cam = Game.I.Cam;
            scroller = cam.GetComponent<CameraScroller>();
            if (scroller == null) scroller = cam.gameObject.AddComponent<CameraScroller>();

            if (decorRoot == null) decorRoot = new GameObject("Decorations").transform;
            if (locked != null)
            {
                locked.color = Color.black;
                locked.sortingOrder = LockedOrder;
            }
            // Bỏ cỏ, cây, trời phía trên map: vỉa hè phủ tới đỉnh, cỏ chuyển xuống dải giữa hai đường.
            if (sky != null) sky.gameObject.SetActive(false);
            if (grass != null)
            {
                if (Game.I.art.grass != null) grass.sprite = Game.I.art.grass;
                grass.drawMode = SpriteDrawMode.Tiled;
                grass.transform.localScale = Vector3.one;
            }
            UseNewGate();
            BuildLowerStreet();
            BuildFences();
            BuildCanopy();
            foreach (var s in segments) s.gameObject.SetActive(false);
            UnlockedSegments = 0;
            AddSegment();

            var p = cam.transform.position;
            cam.transform.position = new Vector3(scroller.MinCameraX, p.y, p.z);
        }

        /// <summary>
        /// Trồng hàng cây dọc dải cỏ cho cả con phố (kể cả phần chưa mở, lớp tối sẽ phủ lên).
        /// Dùng hạt ngẫu nhiên cố định để lần chơi nào hàng cây cũng giống nhau.
        /// </summary>
        const float FenceGap = 0.1f;
        // Cột dọc hai bên lối đi thò quá đỉnh rào trên và chân rào dưới.
        const float PostOverhangTop = 0.3f, PostOverhangBottom = 0.15f;

        /// <summary>
        /// Hàng rào dọc mép ngoài hai con đường, tức phía dải gạch nằm giữa hai đường (không rào phía gian hàng hay các khu),
        /// ngắt ở mỗi lối đi dọc; hai đầu mỗi đoạn rào có cột gỗ.
        /// </summary>
        void BuildFences()
        {
            var art = Game.I.art;
            if (art.fence == null) return;
            var root = new GameObject("Fences").transform;
            root.SetParent(transform, false);
            // Rào đường trên đứng ngay dưới mép đường; rào đường dưới đứng trên cỏ sát mép trên của đường.
            float upperY = roadBottomY - 0.05f, lowerY = lowerRoadTopY + 0.02f;
            float height = art.fence.bounds.size.y;
            foreach (float y in new[] { upperY, lowerY })
            {
                float x = RoadLeft;
                for (int i = 0; i < segments.Length; i++)
                {
                    float cx = ConnectorXAt(i);
                    AddFence(root, x, cx - ConnectorWidth * 0.5f - FenceGap, y);
                    x = cx + ConnectorWidth * 0.5f + FenceGap;
                }
                AddFence(root, x, RoadRight, y);
                // Hai đầu con đường: cột ngắn ở đầu mỗi hàng rào.
                AddPost(root, RoadLeft, y - 0.05f, height + 0.3f);
                AddPost(root, RoadRight, y - 0.05f, height + 0.3f);
            }
            // Hai bên mỗi lối đi dọc: một cột liền từ dưới rào đường dưới lên quá đỉnh rào đường trên.
            float bottom = lowerY - PostOverhangBottom, top = upperY + height + PostOverhangTop;
            for (int i = 0; i < segments.Length; i++)
            {
                float cx = ConnectorXAt(i);
                AddPost(root, cx - ConnectorWidth * 0.5f - FenceGap, bottom, top - bottom);
                AddPost(root, cx + ConnectorWidth * 0.5f + FenceGap, bottom, top - bottom);
            }
        }

        // Tán lá viền trên, dưới vẽ trên mọi thứ của phố (kể cả lớp đen phần chưa mở), dưới chữ bay lên.
        const int CanopyOrder = 21000;

        /// <summary>
        /// Dải tán lá che mép trên và mép dưới map. Các tấm đặt sát nhau, tấm xen kẽ lật ngang nên mép tiếp giáp trùng
        /// khít; viền dưới là tán lá lật ngược mọc từ dưới lên.
        /// </summary>
        void BuildCanopy()
        {
            var sprite = Game.I.art.canopy;
            if (sprite == null) return;
            var root = new GameObject("Canopy").transform;
            root.SetParent(transform, false);
            float w = sprite.bounds.size.x;
            float x0 = RoadLeft - 6f, x1 = RoadRight + 30f;
            float top = mapTopY + TopUiCover + 0.3f, bottom = bottomY - BottomUiCover - 0.3f;
            int i = 0;
            for (float x = x0; x < x1; x += w, i++)
            {
                bool mirror = i % 2 == 1;
                var t = Game.I.NewSprite(root, sprite, CanopyOrder, "CanopyTop");
                t.transform.position = new Vector3(x + w * 0.5f, top, 0f);
                t.flipX = mirror;
                var b = Game.I.NewSprite(root, sprite, CanopyOrder, "CanopyBottom");
                b.transform.position = new Vector3(x + w * 0.5f, bottom, 0f);
                b.transform.localScale = new Vector3(1f, -1f, 1f);
                b.flipX = mirror;
            }
        }

        /// <summary>Cổng chào mới đã vẽ sẵn chữ trên biển: thay hình và ẩn dòng chữ cũ.</summary>
        void UseNewGate()
        {
            if (gate == null || Game.I.art.gate == null) return;
            if (gate.TryGetComponent<SpriteRenderer>(out var sr)) sr.sprite = Game.I.art.gate;
            var label = gate.Find("Label");
            if (label != null) label.gameObject.SetActive(false);
        }

        void AddFence(Transform root, float x0, float x1, float y)
        {
            if (x1 - x0 < 0.5f) return;
            var g = Game.I;
            var fence = g.NewSprite(root, g.art.fence, 0, "Fence");
            fence.drawMode = SpriteDrawMode.Tiled;
            fence.size = new Vector2(x1 - x0, g.art.fence.bounds.size.y);
            fence.transform.position = new Vector3((x0 + x1) * 0.5f, y, 0f);
            fence.gameObject.AddComponent<YSort>().isStatic = true;
        }

        /// <summary>Cột gỗ đứng (neo ở chân), kéo dài kiểu 9-slice nên hai đầu bo tròn giữ nguyên.</summary>
        void AddPost(Transform root, float x, float baseY, float height)
        {
            var art = Game.I.art;
            if (art.fencePost == null) return;
            var post = Game.I.NewSprite(root, art.fencePost, 0, "FencePost");
            post.drawMode = SpriteDrawMode.Sliced;
            post.size = new Vector2(art.fencePost.bounds.size.x, height);
            post.transform.position = new Vector3(x, baseY, 0f);
            post.gameObject.AddComponent<YSort>().isStatic = true;
        }

        // Khu ăn uống: hai hàng bàn (lệch so với chân khu), mỗi hàng 3 bộ chia đều bề ngang đoạn phố.
        static readonly float[] DiningRowY = { 0.2f, -1.9f };
        static readonly float[] DiningColumns = { 0.2f, 0.5f, 0.8f };
        // Góc ghế đẩu: ghế hai bên bàn thấp rộng 1 đơn vị, mặt bàn cao 0,52.
        static readonly float[] StoolSeatX = { -0.8f, 0.8f };
        const float LowDishY = 0.5f;

        /// <summary>
        /// Đường dưới song song đường trên, lối đi dọc nối hai đường ở cuối đoạn phố đầu, biển mở từng khu.
        /// Sân khấu đặt sẵn ở đoạn phố chưa chơi được được cất lại để dùng cho khu biểu diễn.
        /// </summary>
        void BuildLowerStreet()
        {
            var g = Game.I;
            plazaRoot = new GameObject("LowerStreet").transform;
            plazaRoot.SetParent(transform, false);
            if (g.art.road != null)
            {
                int order = road != null ? road.sortingOrder : -31990;
                lowerRoad = g.NewSprite(plazaRoot, g.art.road, order, "LowerRoad");
                lowerRoad.drawMode = SpriteDrawMode.Tiled;
                for (int i = 0; i < segments.Length; i++)
                {
                    var c = g.NewSprite(plazaRoot, g.art.road, order, "Connector_" + (i + 1));
                    c.drawMode = SpriteDrawMode.Tiled;
                    connectors.Add(c);
                }
                if (gate != null)
                {
                    gatePath = g.NewSprite(plazaRoot, g.art.road, order, "GatePath");
                    gatePath.drawMode = SpriteDrawMode.Tiled;
                }
            }
            for (int i = PlayableSegments; i < segments.Length; i++)
            {
                foreach (var stage in segments[i].stages)
                {
                    if (stage == null) continue;
                    stage.transform.SetParent(plazaRoot, true);
                    stage.gameObject.SetActive(false);
                    spareStages.Add(stage);
                }
            }
            for (int i = 0; i < PlazaZoneCount; i++)
            {
                var seg = segments[i];
                zoneSigns.Add(PlazaZoneSign.Create(plazaRoot, i, new Vector2(seg.LeftX + seg.width * 0.5f, zoneBaseY - 0.3f)));
            }
        }

        /// <summary>Dựng nội dung một khu dọc đường dưới (khu thứ i nằm dưới đoạn phố thứ i).</summary>
        void BuildZone(int zone)
        {
            var art = Game.I.art;
            var seg = segments[zone];
            float left = seg.LeftX, w = seg.width;
            switch (ZoneKinds[zone])
            {
                case PlazaZoneKind.Dining:
                    // Toàn bộ là góc ghế nhựa vỉa hè: 2 hàng, mỗi hàng 3 bộ bàn thấp với ghế đẩu đỏ, xanh.
                    if (art.diningTableLow == null) break;
                    foreach (float y in DiningRowY)
                        foreach (float fx in DiningColumns)
                            Tables.Add(DiningTable.Create(plazaRoot, art.diningTableLow, new[] { art.stoolRed, art.stoolBlue },
                                new Vector2(left + w * fx, zoneBaseY + y), StoolSeatX, LowDishY, true));
                    break;
                case PlazaZoneKind.FolkGames:
                    // Ba trò đều chưa mở: người chơi mở lần lượt từ trái sang phải.
                    for (int i = 0; i < FolkGameSlots.Length; i++)
                        FolkGames.Add(FolkGameStation.Create(plazaRoot, (FolkGameKind)i, i, new Vector2(left + w * FolkGameSlots[i], zoneBaseY)));
                    break;
                case PlazaZoneKind.Stage:
                    for (int i = 0; i < spareStages.Count && i < 2; i++)
                    {
                        var stage = spareStages[i];
                        stage.transform.position = new Vector3(left + w * (i == 0 ? 0.28f : 0.72f), stageBaseY, 0f);
                        stage.gameObject.SetActive(true);
                        // YSort tĩnh đã sắp thứ tự vẽ theo chỗ cũ trong scene: sắp lại theo chỗ mới.
                        foreach (var ys in stage.GetComponentsInChildren<YSort>(true)) ys.Apply();
                        stage.Init(Stages.Count);
                        Stages.Add(stage);
                    }
                    break;
            }
        }

        static readonly float[] FolkGameSlots = { 0.2f, 0.5f, 0.8f };

        public int FolkGamesUnlocked { get; private set; }
        public double FolkGameCost(int index) => index < Cfg.folkGameCosts.Length ? Cfg.folkGameCosts[index] : Cfg.folkGameCosts[Cfg.folkGameCosts.Length - 1];
        /// <summary>Trò chơi mở lần lượt: chỉ mở được trò kế tiếp.</summary>
        public bool CanUnlockFolkGame(int index) => index == FolkGamesUnlocked && index < FolkGames.Count;

        public bool TryUnlockFolkGame(int index)
        {
            if (!CanUnlockFolkGame(index) || !Game.I.Economy.TrySpend(FolkGameCost(index))) return false;
            FolkGames[FolkGamesUnlocked++].Unlock();
            Game.I.Audio.Play(Sfx.Build);
            NotifyChanged();
            return true;
        }

        /// <summary>Mở trò kế tiếp không mất tiền, dùng khi nạp save.</summary>
        public void UnlockFolkGameFree()
        {
            if (CanUnlockFolkGame(FolkGamesUnlocked)) FolkGames[FolkGamesUnlocked++].Unlock();
        }

        public bool TryUnlockZone(int zone)
        {
            if (!CanUnlockZone(zone) || !Game.I.Economy.TrySpend(ZoneCost(zone))) return false;
            UnlockNextZone();
            Game.I.Audio.Play(Sfx.Build);
            NotifyChanged();
            return true;
        }

        /// <summary>Mở khu kế tiếp không mất tiền, dùng khi nạp save.</summary>
        public void UnlockZoneFree()
        {
            if (CanUnlockZone(PlazaZonesUnlocked)) UnlockNextZone();
        }

        void UnlockNextZone()
        {
            BuildZone(PlazaZonesUnlocked);
            PlazaZonesUnlocked++;
            RefreshZoneSigns();
        }

        /// <summary>Chỉ hiện biển của khu mở được tiếp theo.</summary>
        void RefreshZoneSigns()
        {
            for (int i = 0; i < zoneSigns.Count; i++) zoneSigns[i].Refresh(CanUnlockZone(i));
        }

        /// <summary>
        /// Giữ chỗ ở bàn ăn còn trống gần khách nhất (theo chiều ngang). Khách ngồi lộ chân được (chân rời, hanbok)
        /// ưu tiên góc ghế đẩu; khách thân dài liền quần chỉ ngồi bàn phủ khăn.
        /// </summary>
        public bool TryReserveSeat(Npc npc, out DiningTable table, out int seat)
        {
            seat = -1;
            table = npc.CanSitOnStool ? NearestFreeTable(npc, true) : null;
            if (table == null) table = NearestFreeTable(npc, false);
            return table != null && table.TryReserve(npc, out seat);
        }

        DiningTable NearestFreeTable(Npc npc, bool stools)
        {
            DiningTable best = null;
            float bestDistance = float.MaxValue;
            foreach (var t in Tables)
            {
                if (t.OnStools != stools || t.FreeSeats == 0) continue;
                float d = Mathf.Abs(t.transform.position.x - npc.transform.position.x);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = t;
                }
            }
            return best;
        }

        /// <summary>Giữ chỗ ở một trò chơi dân gian còn trống, chọn ngẫu nhiên giữa các trò.</summary>
        public bool TryReserveFolkGame(Npc npc, out FolkGameStation station, out int spot)
        {
            spot = -1;
            station = null;
            int start = Random.Range(0, Mathf.Max(1, FolkGames.Count));
            for (int i = 0; i < FolkGames.Count; i++)
            {
                var s = FolkGames[(start + i) % FolkGames.Count];
                if (s.TryReserve(npc, out spot))
                {
                    station = s;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Giữ một điểm trong vùng nhân vật chính đi được.</summary>
        public Vector2 ClampToWalk(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, WalkMinX, WalkMaxX), Mathf.Clamp(p.y, WalkMinY, WalkMaxY));

        void AddSegment()
        {
            var segment = segments[UnlockedSegments];
            segment.gameObject.SetActive(true);
            foreach (var slot in segment.slots)
            {
                slot.Init(Slots.Count);
                Slots.Add(slot);
            }
            foreach (var stage in segment.stages)
            {
                stage.Init(Stages.Count);
                Stages.Add(stage);
            }
            UnlockedSegments++;
            RefreshZoneSigns();
            UpdateLayout();
        }

        void UpdateLayout()
        {
            float right = RightX;
            float left = exitX - 12f;
            float far = right + 25f;
            // Vỉa hè phủ quá cả phần sau hàng icon trên và dưới để giữa các bụi tán lá không lộ nền trống.
            SetArea(ground, left, far, bottomY - BottomUiCover - 1f, mapTopY + TopUiCover + 1f);
            if (ground != null && ground.TryGetComponent<BoxCollider2D>(out var groundCol)) groundCol.size = ground.size;
            // Hai con đường chạy tới cuối map, kể cả phần chưa mở. Đường trên trong scene từng bị kéo giãn dọc,
            // làm hàng viên đá lệch với lối đi: luôn giữ tỉ lệ 1.
            if (road != null) road.transform.localScale = Vector3.one;
            SetArea(road, RoadLeft, RoadRight, roadBottomY, roadTopY);
            SetArea(lowerRoad, RoadLeft, RoadRight, lowerRoadBottomY, lowerRoadTopY);
            for (int i = 0; i < connectors.Count; i++)
            {
                float cx = ConnectorXAt(i);
                SetArea(connectors[i], cx - ConnectorWidth * 0.5f, cx + ConnectorWidth * 0.5f, lowerRoadTopY, roadBottomY);
            }
            SetArea(grass, RoadLeft, RoadRight, lowerRoadTopY, roadBottomY);
            if (gatePath != null)
            {
                // Tâm lối đặt khớp lưới viên đá của đường; chiều cao là bội số 2 viên tính từ mép trên đường.
                float origin = (RoadLeft + RoadRight) * 0.5f;
                float gx = origin + Mathf.Round((GateX - origin) / Stone) * Stone;
                float top = mapTopY + TopUiCover + 1f;
                top = roadTopY + Mathf.Ceil((top - roadTopY) / (2f * Stone)) * (2f * Stone);
                SetArea(gatePath, gx - GatePathWidth * 0.5f, gx + GatePathWidth * 0.5f, roadTopY, top);
            }
            // Phần chưa mở tối đen hoàn toàn, phủ kín cả chiều cao camera nhìn tới; bắt đầu ngay sau biển mở rộng.
            SetArea(locked, right + LockedStartOffset, far, bottomY - BottomUiCover - 1f, mapTopY + TopUiCover + 1f);

            if (sign != null)
            {
                sign.transform.position = new Vector3(right + 1.9f, sign.transform.position.y, sign.transform.position.z);
                sign.Refresh();
            }

            scroller.contentMinX = exitX - 0.5f;
            scroller.contentMaxX = right + 4.5f;
            scroller.contentMinY = bottomY - BottomUiCover;
            scroller.contentMaxY = mapTopY + TopUiCover;
        }

        static void SetArea(SpriteRenderer sr, float x0, float x1, float y0, float y1)
        {
            if (sr == null) return;
            sr.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            sr.size = new Vector2(x1 - x0, y1 - y0);
        }

        /// <summary>Mở đoạn phố không mất tiền, dùng khi nạp save.</summary>
        public void UnlockSegmentFree()
        {
            if (CanExpand) AddSegment();
        }

        public bool TryExpand()
        {
            if (!CanExpand) return false;
            var info = NextSegment;
            if (!Game.I.Economy.TrySpend(info.unlockCost)) return false;
            AddSegment();
            Game.I.Audio.Play(Sfx.Gong);
            NotifyChanged();
            OnSegmentUnlocked?.Invoke(info);
            return true;
        }

        /// <summary>Các quầy chưa dựng thuộc những đoạn phố đã mở, rẻ nhất trước.</summary>
        public IEnumerable<StallData> BuildableStalls()
        {
            return Cfg.stalls
                .Where(d => d != null && d.segment <= UnlockedSegments && Stalls.All(s => s.Data != d))
                .OrderBy(d => d.unlockCost);
        }

        /// <summary>Đặt quầy không mất tiền, dùng khi nạp save.</summary>
        public Stall PlaceStall(StallSlot slot, StallData data)
        {
            var stall = Stall.Create(stallPrefab, data, slot);
            slot.SetStall(stall);
            Stalls.Add(stall);
            stall.OnChanged += NotifyChanged;
            return stall;
        }

        public Stall BuildStall(StallSlot slot, StallData data)
        {
            if (slot.Stall != null || Stalls.Any(s => s.Data == data)) return null;
            if (!Game.I.Economy.TrySpend(data.unlockCost)) return null;
            var stall = PlaceStall(slot, data);
            Game.I.Audio.Play(Sfx.Build);
            NotifyChanged();
            OnStallBuilt?.Invoke(stall);
            return stall;
        }

        // ---------- Trang trí (đặt tự do trên gạch) ----------

        // Trang trí ở vỉa hè giữa đường trên và đường dưới.
        // ---------- Lối vào qua cổng ----------

        /// <summary>Có cổng thì khách đầu trái đi từ phía sau cổng xuống qua lòng cổng (và rời phố ngược lại).</summary>
        public bool HasGateEntry => gate != null;
        /// <summary>Giữa lòng cổng theo trục x (cổng neo ở giữa chân).</summary>
        public float GateX => gate != null ? gate.position.x : exitX;
        /// <summary>Chỗ khách xuất hiện/biến mất phía sau cổng: khuất sau phần mái cổng.</summary>
        public float GateBackY => gate != null ? gate.position.y + 4.5f : laneMaxY;
        float GateHalfWidth => gate != null && gate.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null
            ? sr.bounds.extents.x : 3.2f;

        /// <summary>Lệch ngang ngẫu nhiên khi đi qua lòng cổng để cả đoàn không đi chồng một hàng.</summary>
        public float GateJitter => Random.Range(-0.6f, 0.6f);

        public bool InLowerBrick(float y) => y >= lowerRoadTopY + 0.2f && y <= roadBottomY - 0.05f;
        public bool InUpperBrick(float y) => y >= roadTopY + 0.02f && y <= brickTopY - 0.6f;

        /// <summary>
        /// Vị trí (chân món đồ) đặt được không: nằm trên một trong hai vùng gạch của phần phố đã mở,
        /// không che quầy, ô sạp, sân khấu, cổng, biển và không chồng lên món khác.
        /// </summary>
        public bool CanPlaceDecor(Vector2 p, PlacedDecor ignore = null)
        {
            if (p.x < exitX + 1f || p.x > RightX - 0.3f) return false;
            bool lower = InLowerBrick(p.y), upper = InUpperBrick(p.y);
            if (!lower && !upper) return false;
            // Không đặt chắn lối đi dọc nối hai đường.
            if (lower)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    if (Mathf.Abs(p.x - ConnectorXAt(i)) < ConnectorWidth * 0.5f + 0.6f) return false;
                }
            }
            if (upper)
            {
                foreach (var slot in Slots) if (Mathf.Abs(p.x - slot.X) < 2.4f) return false;
                if (sign != null && Mathf.Abs(p.x - sign.transform.position.x) < 1.8f) return false;
                // Không đặt đè lên cổng (tính theo bề ngang thật của hình cổng, vì cổng có thể phóng to).
                if (gate != null && Mathf.Abs(p.x - gate.position.x) < GateHalfWidth + 0.3f) return false;
            }
            foreach (var d in Decorations)
            {
                if (d != ignore && ((Vector2)d.transform.position - p).sqrMagnitude < 0.9f * 0.9f) return false;
            }
            return true;
        }

        public bool CanAffordDecor(DecorationData d) =>
            d.gemCost > 0 ? Game.I.Economy.CanAffordGems(d.gemCost) : Game.I.Economy.CanAfford(d.coinCost);

        /// <summary>Mua và đặt một món trang trí (mỗi món mua được nhiều lần, tổng số giới hạn theo số đoạn phố).</summary>
        public PlacedDecor BuyDecor(DecorationData d, Vector2 p)
        {
            if (d == null || DecorSlotsLeft <= 0 || !CanPlaceDecor(p)) return null;
            bool paid = d.gemCost > 0 ? Game.I.Economy.TrySpendGems(d.gemCost) : Game.I.Economy.TrySpend(d.coinCost);
            if (!paid) return null;
            var placed = PlaceDecor(d, p);
            placed.Pop();
            Game.I.Data.stats.decorationsPlaced++;
            Game.I.Audio.Play(Sfx.Build);
            NotifyChanged();
            return placed;
        }

        /// <summary>Đặt trang trí không mất tiền, dùng khi nạp save.</summary>
        public PlacedDecor PlaceDecor(DecorationData d, Vector2 p)
        {
            var placed = PlacedDecor.Create(decorRoot, d, p);
            Decorations.Add(placed);
            return placed;
        }

        /// <summary>Gỡ một món trang trí, hoàn lại một phần tiền.</summary>
        public void RemoveDecor(PlacedDecor placed, float refundRatio)
        {
            if (placed == null || !Decorations.Remove(placed)) return;
            var d = placed.Data;
            if (d.gemCost > 0) Game.I.Economy.AddGems(Mathf.FloorToInt(d.gemCost * refundRatio));
            else Game.I.Economy.Add(d.coinCost * refundRatio);
            Destroy(placed.gameObject);
            NotifyChanged();
        }

        // ---------- Nghệ sĩ biểu diễn ----------

        public bool OwnsPerformer(PerformerData p) => p != null && Game.I.Data.performers.Contains(p.id);

        public bool BuyPerformer(PerformerData p)
        {
            if (p == null || OwnsPerformer(p) || !Game.I.Economy.TrySpend(p.coinCost)) return false;
            Game.I.Data.performers.Add(p.id);
            Game.I.Audio.Play(Sfx.Chime);
            NotifyChanged();
            return true;
        }

        public void NotifyChanged() => OnChanged?.Invoke();

        void OnDrawGizmos()
        {
            // Làn đi của khách và điểm vào/ra.
            Gizmos.color = Color.yellow;
            float right = segments.Length > 0 && segments[segments.Length - 1] != null ? segments[segments.Length - 1].RightX : 60f;
            Gizmos.DrawLine(new Vector3(exitX, laneMinY), new Vector3(right, laneMinY));
            Gizmos.DrawLine(new Vector3(exitX, laneMaxY), new Vector3(right, laneMaxY));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(exitX, (laneMinY + laneMaxY) * 0.5f), 0.4f);
        }
    }
}
