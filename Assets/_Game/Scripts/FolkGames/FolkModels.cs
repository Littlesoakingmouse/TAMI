using System;
using System.Collections.Generic;

namespace Tami
{
    /// <summary>Chữ phản hồi bay lên tại một điểm trong sân chơi (toạ độ mô hình).</summary>
    public struct FolkPop
    {
        public string text;
        public bool good;
        public float x, y;
    }

    /// <summary>
    /// Logic thuần của một mini-game trò chơi dân gian, không dùng Unity: giao diện đọc trạng thái để vẽ, còn công cụ
    /// mô phỏng (ArtSource/Tools/folk_sim) cho bot chơi hàng nghìn ván để cân độ khó.
    /// Sân chơi: x chạy 0..Aspect (trái sang phải), y chạy 0..1 (dưới lên trên).
    /// </summary>
    public abstract class FolkModel
    {
        public readonly float Aspect;
        protected readonly Random rng;
        public float Time { get; private set; }
        public bool Finished { get; private set; }
        public float Score01 { get; private set; }
        public int Points { get; protected set; }
        public string Summary { get; private set; } = "";
        public string Status = "";
        public string ActionLabel = "";
        public bool ActionEnabled = true;
        /// <summary>Gợi ý nhỏ dưới sân chơi (cách điều khiển lúc này).</summary>
        public string Hint = "";
        public readonly List<FolkPop> Pops = new List<FolkPop>();
        /// <summary>Âm thanh cần phát: 1 được điểm, -1 trượt, 2 ăn mừng.</summary>
        public readonly List<int> Sounds = new List<int>();

        protected FolkModel(float aspect, int seed)
        {
            Aspect = aspect;
            rng = new Random(seed);
        }

        public void Update(float dt)
        {
            if (Finished) return;
            Time += dt;
            Step(dt);
        }

        protected abstract void Step(float dt);
        public virtual void Action() { }
        public virtual void PointerDown(float x, float y) { }
        public virtual void PointerDrag(float x, float y) { }
        public virtual void PointerUp(float x, float y) { }

        protected void Pop(string text, bool good, float x, float y, int sound = 0)
        {
            Pops.Add(new FolkPop { text = text, good = good, x = x, y = y });
            Sounds.Add(sound != 0 ? sound : good ? 1 : -1);
        }

        /// <summary>Kết thúc ván: điểm đạt fullPoints trở lên là trọn điểm.</summary>
        protected void End(float fullPoints, string summary)
        {
            if (Finished) return;
            Finished = true;
            Score01 = Clamp01(Math.Max(0, Points) / fullPoints);
            Summary = summary;
            ActionEnabled = true;
        }

        protected float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        protected bool Chance(float p) => rng.NextDouble() < p;
        protected static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        protected static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Đập niêu: ba chiếc niêu treo trên dây (một niêu quà to, một niêu bánh kẹo, một niêu nước để trêu).
    /// Nhìn niêu quà trong chốc lát, bị bịt mắt và quay ba vòng (chóng mặt, người cứ trôi lệch), rồi bước từng bước
    /// theo tiếng hô của cả xóm (chỉ hướng) và tiếng reo (càng gần niêu quà reo càng to) - cẩn thận Cu Tí hay trêu
    /// hô sai. Có 3 lần đập, đập vỡ tan đúng giữa niêu quà mới trọn điểm.
    /// </summary>
    public sealed class PotSmashModel : FolkModel
    {
        public enum PotKind { Prize, Candy, Water }
        public enum Phase { Peek, Spin, Blind }

        public sealed class Pot
        {
            public float baseX;
            public PotKind kind;
            public bool broken;
            public float phase;
        }

        public const int Swings = 3;
        public const float TimeLimit = 32f, FullPoints = 6f;
        public const float PeekTime = 3f, SpinTime = 1.2f;
        public const float StepSize = 0.13f, WalkSpeed = 0.5f;
        public const float HitRange = 0.14f, PerfectRange = 0.065f, SwayAmp = 0.04f;
        public const float HintEvery = 0.7f, PrankChance = 0.18f;

        public readonly List<Pot> Pots = new List<Pot>();
        public Phase State { get; private set; } = Phase.Peek;
        public float WalkerX { get; private set; }
        public int SwingsLeft { get; private set; } = Swings;
        public float SwingAnim { get; private set; }
        /// <summary>Tiếng hô gần nhất: người hô, lời hô, có phải cậu bé trêu không.</summary>
        public string HintSpeaker { get; private set; } = "";
        public string HintText { get; private set; } = "";
        public bool HintIsPrank { get; private set; }
        public float HintAge { get; private set; }
        public float Dizzy { get; private set; }
        /// <summary>Tiếng reo của cả xóm 0..1: càng gần niêu quà (hoặc niêu kẹo khi niêu quà đã vỡ) càng to, hơi trễ và nhiễu.</summary>
        public float Cheer { get; private set; }

        float phaseTime, targetX, driftV, driftTimer, hintTimer, delayedX, endTimer = -1f;

        public PotSmashModel(float aspect, int seed) : base(aspect, seed)
        {
            // Ba niêu cách nhau đủ xa, thứ tự nội dung xáo ngẫu nhiên.
            var kinds = new List<PotKind> { PotKind.Prize, PotKind.Candy, PotKind.Water };
            float slot = (aspect - 1.0f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                int k = rng.Next(kinds.Count);
                Pots.Add(new Pot { baseX = 0.5f + slot * (i + 0.5f) + Range(-slot * 0.25f, slot * 0.25f), kind = kinds[k], phase = Range(0f, 6.28f) });
                kinds.RemoveAt(k);
            }
            WalkerX = targetX = delayedX = aspect * 0.5f;
            Status = "Nhớ kỹ niêu có QUÀ TO!";
            ActionLabel = "ĐẬP!";
            ActionEnabled = false;
            Hint = "Bấm ‹ TRÁI / PHẢI › để bước, nghe tiếng reo rồi ĐẬP!";
        }

        public float PotX(Pot p) => p.baseX + (float)Math.Sin(Time * 1.4f + p.phase) * SwayAmp;

        Pot HintTarget()
        {
            Pot best = null;
            foreach (var p in Pots)
            {
                if (p.broken || p.kind == PotKind.Water) continue;
                if (best == null || p.kind == PotKind.Prize) best = p;
            }
            return best;
        }

        protected override void Step(float dt)
        {
            phaseTime += dt;
            SwingAnim = Math.Max(0f, SwingAnim - dt * 2.5f);
            HintAge += dt;
            switch (State)
            {
                case Phase.Peek:
                    if (phaseTime >= PeekTime) { State = Phase.Spin; phaseTime = 0f; Status = "Bịt mắt... quay 3 vòng!"; }
                    return;
                case Phase.Spin:
                    if (phaseTime >= SpinTime)
                    {
                        State = Phase.Blind;
                        phaseTime = 0f;
                        ActionEnabled = true;
                        // Quay xong bị lệch khỏi chỗ đứng một đoạn.
                        WalkerX = targetX = Clamp(WalkerX + Range(-0.4f, 0.4f), 0.3f, Aspect - 0.3f);
                        Status = "Còn " + SwingsLeft + " lần đập";
                    }
                    return;
            }

            // Chóng mặt: người tự trôi lệch, giảm dần theo thời gian.
            Dizzy = Math.Max(0.35f, 1f - phaseTime / 10f);
            driftTimer -= dt;
            if (driftTimer <= 0f)
            {
                driftTimer = Range(0.5f, 0.9f);
                driftV = Range(-0.06f, 0.06f);
            }
            targetX = Clamp(targetX + driftV * Dizzy * dt, 0.25f, Aspect - 0.25f);
            float d = targetX - WalkerX;
            float move = WalkSpeed * dt;
            WalkerX += Math.Abs(d) <= move ? d : Math.Sign(d) * move;

            // Tiếng reo đuổi theo độ gần thật (trễ khoảng 0,3 giây), lẫn tiếng ồn.
            var near = HintTarget();
            float closeness = near == null ? 0f : Clamp01(1f - Math.Abs(PotX(near) - WalkerX) / 0.7f);
            float want = Clamp01(closeness + Range(-0.05f, 0.05f));
            Cheer += (want - Cheer) * Math.Min(1f, dt * 3.5f);

            // Tiếng hô theo từng nhịp.
            hintTimer -= dt;
            if (hintTimer <= 0f)
            {
                hintTimer = HintEvery;
                delayedX = WalkerX;
                Shout();
            }

            if (endTimer >= 0f)
            {
                endTimer -= dt;
                if (endTimer < 0f) Finish();
                return;
            }
            if (phaseTime >= TimeLimit) { Pop("Hết giờ!", false, Aspect * 0.5f, 0.7f); Finish(); }
        }

        void Shout()
        {
            var target = HintTarget();
            HintAge = 0f;
            if (target == null)
            {
                HintSpeaker = "Cả xóm";
                HintText = "Hết quà rồi!";
                HintIsPrank = false;
                return;
            }
            float dx = PotX(target) - delayedX;
            HintIsPrank = Chance(PrankChance);
            if (HintIsPrank)
            {
                HintSpeaker = "Cu Tí (trêu)";
                // Cu Tí hô ngược hướng, có lúc hô bừa "ĐẬP ĐI!" khi còn xa.
                if (Math.Abs(dx) >= 0.25f && Chance(0.35f)) HintText = "ĐẬP ĐI!";
                else HintText = Math.Abs(dx) < 0.1f ? (Chance(0.5f) ? "Sang trái!" : "Sang phải!") : dx > 0 ? "Sang trái!" : "Sang phải!";
                return;
            }
            // Cả xóm chỉ hô hướng; tới gần thì chỉ biết reo, phải tự dò theo tiếng reo.
            HintSpeaker = "Cả xóm";
            HintText = Math.Abs(dx) < 0.3f ? "Gần lắm rồi!" : dx > 0 ? "Sang phải!" : "Sang trái!";
        }

        /// <summary>Bước một bước sang trái (-1) hoặc phải (1).</summary>
        public void Walk(int dir)
        {
            if (State != Phase.Blind || endTimer >= 0f) return;
            targetX = Clamp(targetX + Math.Sign(dir) * StepSize, 0.25f, Aspect - 0.25f);
        }

        public override void Action()
        {
            if (State != Phase.Blind || SwingAnim > 0.3f || endTimer >= 0f || SwingsLeft <= 0) return;
            SwingsLeft--;
            SwingAnim = 1f;
            Pot hit = null;
            float best = float.MaxValue;
            foreach (var p in Pots)
            {
                if (p.broken) continue;
                float dist = Math.Abs(PotX(p) - WalkerX);
                if (dist < best) { best = dist; hit = p; }
            }
            if (hit != null && best <= HitRange)
            {
                hit.broken = true;
                switch (hit.kind)
                {
                    case PotKind.Prize:
                        // Đập đúng giữa thì niêu vỡ tan, lệch thì chỉ sứt một mảng (ít quà rơi ra hơn).
                        bool perfect = best <= PerfectRange;
                        Points += perfect ? 5 : 3;
                        Pop(perfect ? "Vỡ tan niêu quà! +5" : "Sứt niêu quà! +3", true, PotX(hit), 0.75f, 2);
                        break;
                    case PotKind.Candy:
                        Points += 2;
                        Pop("Bánh kẹo! +2", true, PotX(hit), 0.75f);
                        break;
                    default:
                        Points -= 1;
                        Pop("Niêu nước! Ướt sũng -1", false, PotX(hit), 0.75f);
                        break;
                }
            }
            else Pop("Đập hụt!", false, WalkerX, 0.55f);

            Status = SwingsLeft > 0 ? "Còn " + SwingsLeft + " lần đập" : "Hết lượt đập";
            bool anyGift = HintTarget() != null;
            if (SwingsLeft <= 0 || !anyGift) { endTimer = 0.9f; ActionEnabled = false; }
        }

        void Finish() => End(FullPoints, Points + " điểm");
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Đá cầu: tâng quả cầu lông. Cầu rơi chậm, lắc lư theo gió; chạm vào quả cầu khi nó xuống tới vùng chân để tâng.
    /// Chạm lệch về một bên thì cầu bay về bên kia (giữ cầu ở giữa sân). Càng tâng cầu rơi càng nhanh;
    /// tâng liên tục không rơi thì được thưởng chuỗi. Có 2 lượt rơi.
    /// </summary>
    public sealed class ShuttlecockModel : FolkModel
    {
        public const float TimeLimit = 40f, FullPoints = 36f;
        public const int Lives = 2;
        public const float Gravity = 1.05f, Drag = 0.9f;
        // Cầu rơi nhanh dần: mức tăng tốc 0..1 tăng theo số lần tâng (đủ sau KicksToMax lần) và theo thời gian chơi
        // (đủ sau SecondsToMax giây). Ở mức tối đa trọng lực x1,8 và lực cản còn 0,75 nên tốc độ rơi gấp ~2,4 lần lúc đầu.
        public const float KicksToMax = 25f, SecondsToMax = 90f, MaxGravityBoost = 0.8f, MaxDragCut = 0.25f;
        public const float KickTop = 0.44f, SweetLow = 0.1f, SweetHigh = 0.26f, TapRadius = 0.42f;
        public const float StartDelay = 1f;

        public float X { get; private set; }
        public float Y { get; private set; } = 1.05f;
        public float VX { get; private set; }
        public float VY { get; private set; }
        public int Kicks { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int LivesLeft { get; private set; } = Lives;
        public float Stun { get; private set; }
        public float Wind { get; private set; }
        public float KickAnim { get; private set; }

        float startLeft = StartDelay, gustTimer = 8f, gustLeft, gravity = Gravity, drag = Drag;
        int speedTier;

        /// <summary>Mức tăng tốc rơi hiện tại 0..1 (0 = chậm như lúc đầu).</summary>
        public float SpeedUp => Clamp01(Kicks / KicksToMax + Math.Max(0f, Time - StartDelay) / SecondsToMax);
        /// <summary>Tốc độ rơi so với lúc đầu (1 = như lúc đầu).</summary>
        public float FallSpeedFactor => (Gravity * (1f + MaxGravityBoost * SpeedUp) / (Drag * (1f - MaxDragCut * SpeedUp))) / (Gravity / Drag);

        public ShuttlecockModel(float aspect, int seed) : base(aspect, seed)
        {
            X = aspect * 0.5f;
            ActionLabel = "ĐÁ!";
            Status = "Chuẩn bị... cầu đang rơi!";
            Hint = "Chạm vào quả cầu khi nó xuống vùng xanh · chạm lệch để đẩy cầu về giữa";
        }

        public float TimeLeft => Math.Max(0f, TimeLimit - Time);

        protected override void Step(float dt)
        {
            if (startLeft > 0f)
            {
                startLeft -= dt;
                return;
            }
            Stun = Math.Max(0f, Stun - dt);
            KickAnim = Math.Max(0f, KickAnim - dt * 3f);

            // Gió nhẹ thổi từng đợt.
            gustTimer -= dt;
            if (gustLeft > 0f)
            {
                gustLeft -= dt;
                if (gustLeft <= 0f) Wind = 0f;
            }
            else if (gustTimer <= 0f)
            {
                gustTimer = Range(7f, 10f);
                gustLeft = 1.5f;
                Wind = Range(0.25f, 0.45f) * (Chance(0.5f) ? 1f : -1f);
            }

            // Cầu lông: lên nhanh, rơi chậm (lực cản), bị gió đẩy ngang; càng chơi lâu, tâng nhiều càng rơi nhanh.
            float speedUp = SpeedUp;
            gravity = Gravity * (1f + MaxGravityBoost * speedUp);
            drag = Drag * (1f - MaxDragCut * speedUp);
            int tier = (int)(speedUp * 4f);
            if (tier > speedTier)
            {
                speedTier = tier;
                Pop(tier >= 4 ? "Cầu rơi nhanh nhất!" : "Cầu rơi nhanh hơn!", false, Aspect * 0.5f, 0.85f, 2);
            }
            VY -= gravity * dt;
            if (VY < 0f) VY -= VY * drag * dt;
            VX += Wind * dt;
            VX -= VX * 0.4f * dt;
            X += VX * dt;
            Y += VY * dt;
            if (X < 0.15f) { X = 0.15f; VX = Math.Abs(VX) * 0.5f; }
            if (X > Aspect - 0.15f) { X = Aspect - 0.15f; VX = -Math.Abs(VX) * 0.5f; }

            if (Y <= 0.02f)
            {
                LivesLeft--;
                Streak = 0;
                if (LivesLeft <= 0)
                {
                    Pop("Cầu rơi rồi!", false, X, 0.3f);
                    Finish();
                    return;
                }
                // Còn lượt: nhặt cầu tung lại từ trên cao.
                Pop("Rơi rồi! Còn " + LivesLeft + " lượt", false, X, 0.3f);
                X = Aspect * 0.5f;
                Y = 1.05f;
                VX = VY = 0f;
                startLeft = 0.8f;
                return;
            }
            if (Time >= TimeLimit + StartDelay)
            {
                Points += 3;
                Pop("Giữ cầu tới cuối! +3", true, Aspect * 0.5f, 0.6f, 2);
                Finish();
                return;
            }
            Status = "Tâng " + Kicks + "  ·  " + Points + " điểm  ·  còn " + (int)Math.Ceiling(TimeLeft) + " giây  ·  " + LivesLeft + " lượt"
                + "  ·  tốc độ rơi x" + FallSpeedFactor.ToString("0.0")
                + (Wind > 0f ? "  ·  gió →" : Wind < 0f ? "  ·  ← gió" : "");
        }

        void Finish() => End(FullPoints, "Tâng " + Kicks + " lần  ·  chuỗi dài nhất " + BestStreak + "  ·  " + Points + " điểm");

        public override void PointerDown(float x, float y)
        {
            if (startLeft > 0f || Stun > 0f) return;
            float dx = x - X, dy = y - Y;
            if (Y > KickTop || dx * dx + dy * dy > TapRadius * TapRadius)
            {
                Whiff();
                return;
            }
            // Chạm bên trái quả cầu thì cầu bay sang phải và ngược lại.
            Kick(-dx * 3f + Range(-0.08f, 0.08f));
        }

        public override void Action()
        {
            if (startLeft > 0f || Stun > 0f) return;
            if (Y > KickTop) { Whiff(); return; }
            Kick((Aspect * 0.5f - X) * 0.45f + Range(-0.25f, 0.25f));
        }

        void Kick(float vx)
        {
            bool sweet = Y >= SweetLow && Y <= SweetHigh;
            int gained = sweet ? 2 : 1;
            Kicks++;
            Streak++;
            if (Streak > BestStreak) BestStreak = Streak;
            // Mỗi 10 quả liên tục không rơi được thưởng thêm.
            if (Streak % 10 == 0)
            {
                gained += 3;
                Pop("Chuỗi " + Streak + " quả! +3", true, Aspect * 0.5f, 0.75f, 2);
            }
            Points += gained;
            KickAnim = 1f;
            VY =(float)Math.Sqrt(2f * gravity * Math.Max(0.15f, Range(0.72f, 0.9f) - Y));
            VX = Clamp(vx, -1.2f, 1.2f);
            Pop(sweet ? "Đẹp! +2" : "+1", true, X, Y + 0.15f);
        }

        void Whiff()
        {
            Stun = 0.35f;
            Pop("Hụt chân!", false, X, Math.Min(Y, 0.6f));
        }
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>
    /// Tuho (nhìn ngang): kéo ngược như kéo ná rồi thả để ném mũi tên theo đường vòng cung vào bình.
    /// Vào miệng bình 2 điểm (giữa miệng thêm 1), lọt vào tai bình hai bên 3 điểm. Có gió, bình lùi xa dần.
    /// </summary>
    public sealed class TuhoModel : FolkModel
    {
        public const int Arrows = 6;
        public const float FullPoints = 10f;
        public const float Gravity = 3f, PowerScale = 4f, MaxSpeed = 4.4f, MinPull = 0.06f;
        // Đo theo hình bình (game_tuho.png, cao JarHeight đơn vị, chân ở mặt đất): miệng loe, cổ hẹp, thân phình, hai tai hai bên cổ.
        public const float JarHeight = 0.5f, GroundY = 0.1f;
        public const float MouthY = GroundY + 0.712f * JarHeight, MouthHalf = 0.058f, NeckHalf = 0.036f;
        public const float ShoulderY = GroundY + 0.444f * JarHeight, BodyHalf = 0.115f;
        public const float EarOffset = 0.066f, EarY = GroundY + 0.596f * JarHeight, EarRadius = 0.032f;
        /// <summary>Đường chấm ngắm vẽ tới khi mũi tên đã đi được phần này quãng đường tới bình.</summary>
        public const float PreviewReach = 0.6f;

        public readonly float ThrowX = 0.5f, ThrowY = 0.32f;
        public float JarX { get; private set; }
        public float Wind { get; private set; }
        public int Thrown { get; private set; }
        public bool Aiming { get; private set; }
        public float AimVX { get; private set; }
        public float AimVY { get; private set; }
        public bool Flying { get; private set; }
        public float ArrowX { get; private set; }
        public float ArrowY { get; private set; }
        public float ArrowVX { get; private set; }
        public float ArrowVY { get; private set; }
        /// <summary>Mũi tên đã cắm: x, y, góc (độ) và vị trí bình lúc cắm (bình lùi xa thì mũi tên đi theo).</summary>
        public readonly List<(float x, float y, float angle, float jarX)> Stuck = new List<(float, float, float, float)>();
        public bool Landed { get; private set; }

        float downX, downY, waitLeft;

        public TuhoModel(float aspect, int seed) : base(aspect, seed)
        {
            JarX = aspect * 0.68f;
            NewArrow();
            ActionEnabled = false;
            ActionLabel = "Kéo thả để ném";
            Hint = "Chạm, kéo ngược về phía sau rồi thả (như kéo ná)";
        }

        void NewArrow()
        {
            Wind = Range(-0.5f, 0.5f);
            if (Math.Abs(Wind) < 0.12f) Wind = 0f;
            Flying = false;
            Landed = false;
            Status = "Mũi tên " + (Thrown + 1) + "/" + Arrows + "  ·  " + Points + " điểm  ·  "
                + (Wind == 0f ? "lặng gió" : Wind > 0f ? "gió thổi →" : "← gió thổi");
        }

        public void PathPoint(float vx, float vy, float t, out float x, out float y)
        {
            x = ThrowX + vx * t + 0.5f * Wind * t * t;
            y = ThrowY + vy * t - 0.5f * Gravity * t * t;
        }

        /// <summary>Thời gian bay tới lúc đi được PreviewReach quãng đường tới bình (hoặc chạm đất trước đó).</summary>
        public float PreviewDuration(float vx, float vy)
        {
            float reachX = ThrowX + PreviewReach * (JarX - ThrowX);
            float t = 0f;
            for (int i = 0; i < 300; i++)
            {
                t += 0.01f;
                PathPoint(vx, vy, t, out float x, out float y);
                if (x >= reachX || y <= GroundY) break;
            }
            return t;
        }

        public override void PointerDown(float x, float y)
        {
            if (Flying || waitLeft > 0f || Thrown >= Arrows) return;
            Aiming = true;
            downX = x;
            downY = y;
            AimVX = AimVY = 0f;
        }

        public override void PointerDrag(float x, float y)
        {
            if (!Aiming) return;
            float px = (downX - x) * PowerScale, py = (downY - y) * PowerScale;
            float m = (float)Math.Sqrt(px * px + py * py);
            if (m > MaxSpeed) { px *= MaxSpeed / m; py *= MaxSpeed / m; }
            AimVX = px;
            AimVY = py;
        }

        public override void PointerUp(float x, float y)
        {
            if (!Aiming) return;
            PointerDrag(x, y);
            Aiming = false;
            if (Math.Sqrt(AimVX * AimVX + AimVY * AimVY) < MinPull * PowerScale) return;
            Throw(AimVX, AimVY);
        }

        /// <summary>Ném với vận tốc cho trước (bot mô phỏng gọi thẳng).</summary>
        public void Throw(float vx, float vy)
        {
            Flying = true;
            ArrowX = ThrowX;
            ArrowY = ThrowY;
            ArrowVX = vx;
            ArrowVY = vy;
        }

        protected override void Step(float dt)
        {
            if (waitLeft > 0f)
            {
                waitLeft -= dt;
                if (waitLeft <= 0f)
                {
                    Thrown++;
                    if (Thrown >= Arrows)
                    {
                        End(FullPoints, Points + "/" + Arrows * 3 + " điểm");
                        return;
                    }
                    JarX = Math.Min(Aspect - 0.4f, JarX + 0.1f);
                    NewArrow();
                }
                return;
            }
            if (!Flying) return;
            // Chia nhỏ bước để không bay xuyên qua miệng bình.
            for (int i = 0; i < 4 && Flying; i++) Sub(dt / 4f);
        }

        void Sub(float dt)
        {
            float prevY = ArrowY;
            ArrowVX += Wind * dt;
            ArrowVY -= Gravity * dt;
            ArrowX += ArrowVX * dt;
            ArrowY += ArrowVY * dt;

            // Tai bình: hai vòng nhỏ hai bên cổ bình.
            foreach (float side in new[] { -1f, 1f })
            {
                float ex = JarX + side * EarOffset, dx = ArrowX - ex, dy = ArrowY - EarY;
                if (dx * dx + dy * dy <= EarRadius * EarRadius && ArrowVY < 0f)
                {
                    Score(3, "Lọt tai bình! +3", ex, EarY, 2);
                    return;
                }
            }
            // Miệng bình: cắt ngang mép miệng khi đang rơi xuống.
            if (ArrowVY < 0f && prevY >= MouthY && ArrowY < MouthY)
            {
                float off = Math.Abs(ArrowX - JarX);
                if (off <= MouthHalf)
                {
                    bool center = off <= MouthHalf * 0.4f;
                    Score(center ? 3 : 2, center ? "Giữa miệng bình! +3" : "Vào bình! +2", ArrowX, MouthY, center ? 2 : 1);
                    return;
                }
            }
            // Cổ và thân bình: chạm thì bật ra.
            float half = ArrowY >= ShoulderY ? NeckHalf : BodyHalf;
            if (ArrowY < MouthY && ArrowY > GroundY && Math.Abs(ArrowX - JarX) <= half)
            {
                Miss("Chạm thân bình!");
                return;
            }
            if (ArrowY <= GroundY) { Miss(ArrowX < JarX ? "Ngắn quá!" : "Xa quá!"); return; }
            if (ArrowX > Aspect + 0.3f) Miss("Xa quá!");
        }

        void Score(int points, string text, float x, float y, int sound)
        {
            Points += points;
            Flying = false;
            Landed = true;
            float angle = (float)(Math.Atan2(ArrowVY, ArrowVX) * 180.0 / Math.PI);
            Stuck.Add((x, y, angle, JarX));
            Pop(text, true, x, y + 0.2f, sound);
            waitLeft = 0.9f;
        }

        void Miss(string text)
        {
            Flying = false;
            Landed = false;
            Pop(text, false, Clamp(ArrowX, 0.3f, Aspect - 0.3f), 0.35f);
            waitLeft = 0.9f;
        }
    }
}
