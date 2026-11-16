using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>
    /// Một khách: vào từ một đầu phố, đi một chiều sang đầu kia, ghé lần lượt các quầy đã chọn trên đường đi (GDD mục 4.5).
    /// Mua xong thì cầm món trên tay; ghé hết các quầy, khách có thể xuống khu bên dưới ngồi ăn hoặc chơi trò dân gian
    /// rồi mới quay lại đường để rời phố.
    /// </summary>
    public class Npc : MonoBehaviour, IPointerClickHandler
    {
        // EnterGate: từ sau cổng đi xuống qua lòng cổng ra đường; ExitGate: rời phố ngược lên qua cổng.
        enum State { Walking, Queue, Leaving, ToSeat, Eating, StandUp, ToGame, Playing, Returning, EnterGate, ExitGate }

        // Ngồi vào ghế: hạ người xuống một chút (mượt trong SitTime giây), mặt bàn và khăn che phía dưới.
        const float SitDrop = 0.12f;
        const float SitTime = 0.3f;
        // Một miếng ăn: đưa tay lên miệng, giữ lại nhai, hạ tay xuống, nghỉ.
        const float BiteTime = 1.4f;
        // Ngồi ghế đẩu vỉa hè (lộ chân): nâng người lên mặt ghế, chân rời thu ngắn và choãi ra đung đưa,
        // hanbok co váy lại một chút để lộ chân ghế.
        const float StoolLift = 0.15f;
        const float StoolLegScale = 0.8f;
        const float StoolLegAngle = 28f;
        const float StoolSkirtSquash = 0.92f;

        /// <summary>Bộ đồ ngồi ghế đẩu lộ chân được: có chân rời, hoặc hanbok (váy che chân).</summary>
        public bool CanSitOnStool { get; private set; }
        bool hasLegs;

        public NpcTypeData Type { get; private set; }
        public float PayMultiplier => Type.payMultiplier;
        public bool IsAtCounter => state == State.Queue && current != null && current.QueueIndexOf(this) == 0 && ReachedDest;

        // Thứ tự các ô phần hình cố định để dùng lại khách cho mọi bộ đồ.
        static readonly string[] Roles = { "leg_left", "leg_right", "arm_left", "arm_right", "body", "top", "head" };
        const int LegLeft = 0, LegRight = 1, ArmLeft = 2, ArmRight = 3, Body = 4, Head = 6;
        const float SwingLeg = 22f, SwingArm = 16f, StepRate = 2.4f;

        readonly List<Stall> targets = new List<Stall>();
        int targetIndex;
        State state;
        Stall current;
        Vector2 dest;
        float laneY, speed, waitTimer, animTime, walkBlend, bubbleUntil, speechUntil, exitX;
        bool moving, claimed;
        Transform visual;
        SpriteRenderer[] parts;
        Vector3 headBase;
        SpriteRenderer legacyBody, marker, bubble;
        // Phần hình dùng để biết khách có đang trong màn hình không.
        SpriteRenderer cullRenderer;
        SpeechBubble speech;
        BoxCollider2D clickArea;
        YSort ysort;

        // Món vừa mua, cầm ở bàn tay của cánh tay phía trước.
        SpriteRenderer heldItem;
        Sprite held;
        // Ăn ở bàn hoặc chơi trò dân gian sau khi ghé hết các quầy.
        bool extrasDecided;
        DiningTable table;
        int seat = -1;
        FolkGameStation station;
        int spot = -1;
        float activityUntil, eatStart;
        // Đường đi xuống khu dưới và quay lên (qua lối đi dọc).
        readonly List<Vector2> path = new List<Vector2>();
        float lowerLaneY;
        // 0 đứng, 1 đã ngồi hẳn; onStool: chỗ đang ngồi là ghế đẩu vỉa hè (dáng ngồi lộ chân).
        float sitBlend;
        bool onStool;

        bool ReachedDest => ((Vector2)transform.position - dest).sqrMagnitude < 0.0025f;

        /// <summary>Tạo khung một khách (dùng lại nhiều lần qua <see cref="Setup"/>).</summary>
        public static Npc Create(Transform parent)
        {
            var g = Game.I;
            var go = new GameObject("Npc");
            go.transform.SetParent(parent, false);
            var npc = go.AddComponent<Npc>();

            npc.visual = new GameObject("Visual").transform;
            npc.visual.SetParent(go.transform, false);
            npc.parts = new SpriteRenderer[Roles.Length];
            for (int i = 0; i < Roles.Length; i++) npc.parts[i] = g.NewSprite(npc.visual, null, 0, Roles[i]);
            npc.legacyBody = g.NewSprite(npc.visual, null, 0, "Body");
            // Món cầm tay: con của cánh tay trước nên vung theo tay; vẽ trên tay, ngang hàng đầu (thứ tự 5).
            npc.heldItem = g.NewSprite(npc.parts[ArmLeft].transform, null, 5, "Held");
            npc.heldItem.enabled = false;

            npc.marker = g.NewSprite(go.transform, null, 6, "Marker");
            npc.marker.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            npc.bubble = g.NewSprite(go.transform, null, 7, "Bubble");
            npc.bubble.transform.localPosition = new Vector3(0.5f, 1.45f, 0f);

            // Ô thoại tạo sẵn để YSort sắp thứ tự cùng thân khách; đuôi chĩa xuống ngay trên đầu.
            npc.speech = new SpeechBubble(go.transform, new Vector3(0f, 2.5f, 0f), 1f, 40, "Speech");

            npc.clickArea = go.AddComponent<BoxCollider2D>();
            npc.clickArea.size = new Vector2(1.1f, 1.6f);
            npc.clickArea.offset = new Vector2(0f, 0.8f);
            // Khách đi liên tục: collider gắn Rigidbody kinematic chỉ dời vị trí, không phải dựng lại như collider tĩnh.
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            npc.ysort = go.AddComponent<YSort>();
            return npc;
        }

        /// <param name="exitX">Điểm khách rời phố ở đầu bên kia; khách chỉ đi một chiều tới đó.</param>
        /// <param name="rig">Bộ đồ của khách; null thì chọn ngẫu nhiên.</param>
        /// <param name="lane">Làn đi trên đường; bỏ trống thì là độ cao của điểm xuất hiện.</param>
        /// <param name="viaGate">Xuất hiện sau cổng: đi thẳng xuống qua lòng cổng tới làn rồi mới đi ngang.</param>
        public void Setup(NpcTypeData type, List<Stall> stallTargets, Vector2 position, float exit, NpcRig rig = null,
            float lane = float.NaN, bool viaGate = false)
        {
            var g = Game.I;
            gameObject.name = "Npc_" + type.id;
            transform.position = position;
            transform.localScale = Vector3.one * g.config.npcScale;
            Type = type;
            targets.Clear();
            targets.AddRange(stallTargets);
            targetIndex = 0;
            current = null;
            claimed = false;
            laneY = float.IsNaN(lane) ? position.y : lane;
            exitX = exit;
            speed = g.config.npcWalkSpeed * Random.Range(0.85f, 1.15f);
            state = State.Walking;
            animTime = Random.value * 10f;
            walkBlend = 0f;
            extrasDecided = false;
            sitBlend = 0f;
            onStool = false;
            path.Clear();
            if (viaGate)
            {
                state = State.EnterGate;
                path.Add(new Vector2(position.x, laneY));
            }
            held = null;
            heldItem.enabled = false;
            // Khách quay mặt theo hướng đi.
            visual.localScale = new Vector3(exit < position.x ? -1f : 1f, 1f, 1f);

            ApplyLook(g.art, rig);
            marker.sprite = type.marker;
            marker.enabled = type.marker != null;
            bubble.enabled = false;
            speech.Hide();
            clickArea.enabled = type.kind == NpcKind.Mystery;
            gameObject.SetActive(true);
            ysort.Rebase();
        }

        void ApplyLook(GameArt art, NpcRig rig)
        {
            var rigs = art.npcRigs;
            bool useRig = rigs != null && rigs.Length > 0;
            legacyBody.enabled = !useRig;
            foreach (var p in parts)
            {
                p.enabled = false;
                p.transform.localRotation = Quaternion.identity;
            }
            if (!useRig)
            {
                legacyBody.sprite = art.npcBodies[Random.Range(0, art.npcBodies.Length)];
                cullRenderer = legacyBody;
                hasLegs = CanSitOnStool = false;
                return;
            }

            rig ??= rigs[Random.Range(0, rigs.Length)];
            foreach (var part in rig.parts)
            {
                int slot = System.Array.IndexOf(Roles, part.role);
                if (slot < 0) continue;
                var sr = parts[slot];
                sr.sprite = part.sprite;
                ysort.SetBaseOrder(sr, part.order);
                sr.transform.localPosition = part.position;
                sr.enabled = part.sprite != null;
            }
            headBase = parts[Head].transform.localPosition;
            cullRenderer = System.Array.Find(parts, p => p.enabled);
            // Thân hanbok (váy) rộng hơn cao; thân liền quần cao hơn rộng nên không gập chân được.
            hasLegs = parts[LegLeft].enabled && parts[LegRight].enabled;
            var body = parts[Body].sprite;
            CanSitOnStool = hasLegs || (body != null && body.bounds.size.x > body.bounds.size.y);

            // Bàn tay ở đầu dưới của hình cánh tay (gốc toạ độ cánh tay là khớp vai).
            var arm = parts[ArmLeft].sprite;
            if (arm != null)
            {
                var b = arm.bounds;
                heldItem.transform.localPosition = new Vector3(b.min.x + b.size.x * 0.3f, b.min.y - 0.02f, 0f);
                heldItem.transform.localScale = Vector3.one * 0.5f;
            }
        }

        /// <summary>Trả khách về kho để dùng lại.</summary>
        public void Release()
        {
            if (current != null) current.Leave(this);
            current = null;
            targets.Clear();
            FreeActivity();
            gameObject.SetActive(false);
        }

        /// <summary>Trả chỗ ngồi hoặc chỗ chơi đang giữ.</summary>
        void FreeActivity()
        {
            if (table != null) table.Free(seat);
            if (station != null) station.Free(spot);
            table = null;
            station = null;
            seat = spot = -1;
        }

        /// <summary>Ghé hết các quầy: có thể xuống ngồi ăn món đang cầm, hoặc ghé chơi trò dân gian.</summary>
        bool TryStartExtra()
        {
            var g = Game.I;
            var cfg = g.config;
            if (Type.kind == NpcKind.Mystery) return false;
            if (held != null && Random.value < cfg.dineChance && g.Street.TryReserveSeat(this, out table, out seat))
            {
                state = State.ToSeat;
                PathDown(table.SeatPoint(seat).x);
                return true;
            }
            if (Random.value < cfg.folkPlayChance && g.Street.TryReserveFolkGame(this, out station, out spot))
            {
                state = State.ToGame;
                PathDown(station.SpotPoint(spot).x);
                return true;
            }
            return false;
        }

        /// <summary>Đường xuống khu dưới: dọc đường trên tới lối đi dọc gần khu nhất, xuống đường dưới, dọc đường dưới tới khu.</summary>
        void PathDown(float targetX)
        {
            var street = Game.I.Street;
            float cx = street.NearestConnectorX(targetX) + Random.Range(-0.7f, 0.7f);
            lowerLaneY = street.RandomLowerLaneY;
            path.Clear();
            path.Add(new Vector2(cx, laneY));
            path.Add(new Vector2(cx, lowerLaneY));
            path.Add(new Vector2(targetX, lowerLaneY));
        }

        /// <summary>Đường về: ra đường dưới, đi tới lối đi dọc, lên lại làn của mình ở đường trên.</summary>
        void PathUp()
        {
            float cx = Game.I.Street.NearestConnectorX(transform.position.x) + Random.Range(-0.7f, 0.7f);
            path.Clear();
            path.Add(new Vector2(transform.position.x, lowerLaneY));
            path.Add(new Vector2(cx, lowerLaneY));
            path.Add(new Vector2(cx, laneY));
            state = State.Returning;
        }

        /// <summary>Đi theo các điểm trên đường; true khi đã đi hết.</summary>
        bool FollowPath(float dt)
        {
            if (path.Count == 0) return true;
            dest = path[0];
            if (MoveTo(dt)) path.RemoveAt(0);
            return false;
        }

        void UpdateExtra(float dt)
        {
            var cfg = Game.I.config;
            switch (state)
            {
                case State.ToSeat:
                    if (!FollowPath(dt)) return;
                    dest = table.SeatPoint(seat);
                    if (!MoveTo(dt)) return;
                    state = State.Eating;
                    onStool = table.OnStools;
                    // Khách ngồi quay mặt ra phía trước (lật hình về chiều gốc).
                    visual.localScale = Vector3.one;
                    eatStart = Time.time;
                    activityUntil = Time.time + cfg.eatSeconds * Random.Range(0.8f, 1.2f);
                    // Món đặt lên bàn trước mặt.
                    table.ShowDish(seat, held);
                    heldItem.enabled = false;
                    break;
                case State.Eating:
                    moving = false;
                    table.SetDishProgress(seat, Mathf.InverseLerp(eatStart, activityUntil, Time.time));
                    if (Time.time < activityUntil) return;
                    // Ăn xong: tim bay lên, đứng dậy rồi mới trả ghế.
                    held = null;
                    Game.I.Data.stats.mealsEaten++;
                    table.ShowDish(seat, null);
                    ShowBubble(Game.I.art.bubbleBuy);
                    state = State.StandUp;
                    break;
                case State.StandUp:
                    moving = false;
                    if (sitBlend > 0f) return;
                    FreeActivity();
                    PathUp();
                    break;
                case State.ToGame:
                    if (!FollowPath(dt)) return;
                    dest = station.SpotPoint(spot);
                    if (!MoveTo(dt)) return;
                    state = State.Playing;
                    activityUntil = Time.time + cfg.folkPlaySeconds * Random.Range(0.8f, 1.2f);
                    visual.localScale = new Vector3(station.SpotFacesRight(spot) ? 1f : -1f, 1f, 1f);
                    break;
                case State.Playing:
                    moving = false;
                    if (Time.time < activityUntil) return;
                    var s = station;
                    int p = spot;
                    station = null;
                    spot = -1;
                    s.FinishPlay(p);
                    PathUp();
                    break;
                case State.Returning:
                    // Lên lại đường trên rồi đi tiếp về phía cổng ra.
                    if (FollowPath(dt)) state = State.Leaving;
                    break;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (state)
            {
                case State.Walking: UpdateWalking(dt); break;
                case State.Queue: UpdateQueue(dt); break;
                case State.Leaving:
                    var street = Game.I.Street;
                    // Rời phố về đầu trái thì đi ngược lên qua lòng cổng.
                    if (street.HasGateEntry && exitX < street.GateX)
                    {
                        float gx = street.GateX + street.GateJitter;
                        path.Clear();
                        path.Add(new Vector2(gx, laneY));
                        path.Add(new Vector2(gx, street.GateBackY));
                        state = State.ExitGate;
                        break;
                    }
                    dest = new Vector2(exitX, laneY);
                    if (MoveTo(dt)) Finish();
                    break;
                case State.EnterGate:
                    // Xuống tới làn rồi mới đi ngang vào phố.
                    if (FollowPath(dt)) state = State.Walking;
                    break;
                case State.ExitGate:
                    if (FollowPath(dt)) Finish();
                    break;
                default: UpdateExtra(dt); break;
            }
            if (!gameObject.activeSelf) return;

            Animate(dt);
            if (bubble.enabled && Time.time > bubbleUntil) bubble.enabled = false;
            if (speech.Visible && Time.time > speechUntil) speech.Hide();
        }

        /// <summary>Đi: chân tay vung ngược nhau, người nhún nhẹ. Đứng: đầu nhún như đang thở.</summary>
        void Animate(float dt)
        {
            animTime += dt;
            walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1f : 0f, dt * 8f);
            // Ngồi xuống / đứng dậy mượt (tính cả khi khuất màn hình: trạng thái StandUp chờ đứng dậy xong).
            sitBlend = Mathf.MoveTowards(sitBlend, state == State.Eating ? 1f : 0f, dt / SitTime);
            // Khách ngoài màn hình không cần cử động tay chân.
            if (cullRenderer != null && !cullRenderer.isVisible) return;
            float phase = animTime * StepRate * Mathf.PI * 2f;
            float swing = Mathf.Sin(phase) * walkBlend;
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * walkBlend;
            // Độ cao cả người: ngồi ghế thì hạ xuống.
            float sit = Mathf.SmoothStep(0f, 1f, sitBlend);
            float lift = Mathf.Lerp(bob, (onStool ? StoolLift : -SitDrop) / transform.localScale.y, sit);
            // Hanbok ngồi ghế đẩu: co váy lại một chút (giữ chiều lật trái phải của hình).
            float squash = onStool && !hasLegs ? Mathf.Lerp(1f, StoolSkirtSquash, sit) : 1f;
            visual.localScale = new Vector3(visual.localScale.x, squash, 1f);
            visual.localPosition = new Vector3(0f, lift, 0f);
            if (legacyBody.enabled) return;

            // Góc xoay quanh khớp vai: tay trái (phía trước) giơ ra ngoài là góc âm, đưa vào giữa người là góc dương;
            // tay phải ngược lại.
            float armFront = -swing * SwingArm, armBack = swing * SwingArm;
            float chew = 0f;
            if (state == State.Eating && sitBlend >= 1f)
            {
                // Từng miếng: đưa tay lên miệng (0-30%), giữ lại nhai (30-60%), hạ tay (60-85%), nghỉ.
                float t = Mathf.Repeat((Time.time - eatStart - SitTime) / BiteTime, 1f);
                float raise = t < 0.3f ? Mathf.SmoothStep(0f, 1f, t / 0.3f)
                    : t < 0.6f ? 1f
                    : t < 0.85f ? 1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.25f)
                    : 0f;
                armFront = Mathf.Lerp(5f, 125f, raise);
                armBack = 8f;
                if (t >= 0.3f && t < 0.6f) chew = Mathf.Abs(Mathf.Sin((t - 0.3f) * Mathf.PI * 6f)) * 0.035f;
            }
            else if (state == State.Eating || state == State.StandUp) armFront = armBack = 8f; // tay đặt lên bàn
            else if (state == State.Playing && station.Kind == FolkGameKind.DapNieu)
                armFront = -10f - 140f * Mathf.Max(0f, Mathf.Sin(animTime * 3.5f));              // giơ gậy lên rồi đập xuống
            else if (state == State.Playing && station.Kind == FolkGameKind.Tuho)
            {
                // Giơ tay ngắm từ từ rồi vung nhanh ra trước để ném.
                float t = Mathf.Repeat(animTime / 1.6f + spot * 0.5f, 1f);
                armFront = t < 0.7f ? Mathf.Lerp(-20f, -150f, Mathf.SmoothStep(0f, 1f, t / 0.7f)) : Mathf.Lerp(-150f, -60f, (t - 0.7f) / 0.3f);
                armBack = 25f;
            }
            else if (state == State.Playing)
            {
                float balance = 60f + Mathf.Sin(animTime * 4f) * 20f;                             // giang hai tay giữ thăng bằng
                armFront = -balance;
                armBack = balance;
            }

            // Ngồi ghế đẩu: hai chân thu ngắn, choãi ra ngoài và đung đưa nhẹ.
            float stoolSit = onStool ? sit : 0f;
            float dangle = Mathf.Sin(animTime * 2.2f) * 6f;
            // Đá cầu: tâng chân trước theo nhịp quả cầu.
            float kick = state == State.Playing && station.Kind == FolkGameKind.DaCau
                ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(animTime * 5.2f + spot * Mathf.PI)), 3f) * 55f
                : 0f;
            parts[LegLeft].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(swing * SwingLeg, -StoolLegAngle + dangle, stoolSit) + kick);
            parts[LegRight].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-swing * SwingLeg, StoolLegAngle + dangle, stoolSit));
            var legScale = new Vector3(1f, Mathf.Lerp(1f, StoolLegScale, stoolSit), 1f);
            parts[LegLeft].transform.localScale = legScale;
            parts[LegRight].transform.localScale = legScale;
            parts[ArmLeft].transform.localRotation = Quaternion.Euler(0f, 0f, armFront);
            parts[ArmRight].transform.localRotation = Quaternion.Euler(0f, 0f, armBack);
            float breathe = Mathf.Sin(animTime * 2.6f) * 0.012f * (1f - walkBlend);
            // Đang ăn: đầu gật nhẹ khi nhai.
            if (state == State.Eating) breathe += chew;
            parts[Head].transform.localPosition = headBase + new Vector3(0f, bob * 0.5f + breathe, 0f);
        }

        void UpdateWalking(float dt)
        {

            while (targetIndex < targets.Count && targets[targetIndex] == null) targetIndex++;
            if (targetIndex >= targets.Count)
            {
                if (!extrasDecided)
                {
                    extrasDecided = true;
                    if (TryStartExtra()) return;
                }
                state = State.Leaving;
                return;
            }

            var stall = targets[targetIndex];
            dest = new Vector2(stall.Slot.X, laneY);
            if (!MoveTo(dt)) return;

            if (stall.CanAcceptCustomer)
            {
                current = stall;
                stall.Enqueue(this);
                waitTimer = 0f;
                state = State.Queue;
            }
            else
            {
                ShowBubble(Game.I.art.bubbleSad);
                targetIndex++;
            }
        }

        void UpdateQueue(float dt)
        {
            int index = current.QueueIndexOf(this);
            if (index < 0)
            {
                current = null;
                targetIndex++;
                state = State.Walking;
                return;
            }

            dest = current.Slot.QueuePoint(index);
            MoveTo(dt);

            bool beingServed = index == 0 && ReachedDest && !current.VaultFull;
            if (beingServed) return;

            waitTimer += dt;
            if (waitTimer > Game.I.config.npcPatience)
            {
                current.Leave(this);
                current = null;
                ShowBubble(Game.I.art.bubbleSad);
                targetIndex++;
                state = State.Walking;
            }
        }

        /// <summary>Quầy gọi khi đã bán xong cho khách này.</summary>
        /// <param name="dish">Hình món vừa mua: bay lên trên đầu khách rồi mờ dần (không có hình thì hiện tim).</param>
        public void OnServed(Sprite dish = null)
        {
            if (dish != null) FloatingIcon.Spawn(dish, transform.position + Vector3.up * (1.75f * transform.localScale.y));
            else ShowBubble(Game.I.art.bubbleBuy);
            // Cầm món vừa mua (mua nhiều quầy thì cầm món mới nhất).
            if (dish != null)
            {
                held = dish;
                heldItem.sprite = dish;
                heldItem.enabled = !legacyBody.enabled;
            }
            current = null;
            targetIndex++;
            state = State.Walking;
        }

        /// <summary>Hiện một câu thoại ngắn trên đầu khách (GDD mục 6.1).</summary>
        public void Say(string text)
        {
            speech.Show(text);
            speechUntil = Time.time + 2.5f;
            bubble.enabled = false;
        }

        bool MoveTo(float dt)
        {
            Vector2 p = transform.position;
            Vector2 next = Vector2.MoveTowards(p, dest, speed * dt);
            transform.position = next;
            moving = (next - p).sqrMagnitude > 1e-7f;
            // Quay mặt theo hướng đi ngang (đi thẳng lên xuống thì giữ hướng cũ).
            float dx = next.x - p.x;
            if (Mathf.Abs(dx) > speed * dt * 0.3f) visual.localScale = new Vector3(dx < 0f ? -1f : 1f, visual.localScale.y, 1f);
            return (next - dest).sqrMagnitude < 0.0025f;
        }

        void ShowBubble(Sprite sprite)
        {
            if (speech.Visible) return;
            bubble.sprite = sprite;
            bubble.enabled = true;
            bubbleUntil = Time.time + 1.2f;
        }

        void Finish()
        {
            if (Type.kind == NpcKind.FoodBlogger) Game.I.Spawner.OnBloggerLeft();
            Game.I.Spawner.Despawn(this);
        }

        void OnDestroy()
        {
            if (current != null) current.Leave(this);
            FreeActivity();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Type.kind != NpcKind.Mystery || claimed || CameraScroller.DraggedThisPress || DecorMode.Active) return;
            claimed = true;
            if (marker != null) marker.enabled = false;

            var g = Game.I;
            var cfg = g.config;
            if (Random.value < cfg.mysteryGemChance)
            {
                g.Economy.AddGems(cfg.mysteryGems);
                g.Audio.Play(Sfx.Chime);
                g.UI.Toast("Khách bí ẩn tặng bạn " + cfg.mysteryGems + " Ngọc!");
                FloatingText.Spawn(transform.position + Vector3.up * 1.8f, "+" + cfg.mysteryGems + " Ngọc", new Color(0.5f, 0.85f, 1f));
                return;
            }

            double best = 0;
            foreach (var s in g.Street.Stalls) best = System.Math.Max(best, s.IncomePerSale);
            double reward = System.Math.Max(cfg.mysteryRewardMin, best * cfg.mysteryRewardSales);
            g.Economy.Add(reward);
            g.Audio.Play(Sfx.Coin);
            g.UI.OnCoinsCollected(transform.position + Vector3.up * 1.6f, reward);
            g.UI.Toast("Khách bí ẩn tặng bạn " + NumberFormat.Short(reward) + " xu!");
        }
    }
}
