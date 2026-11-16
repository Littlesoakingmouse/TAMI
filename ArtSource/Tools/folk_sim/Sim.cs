// Bot chơi thử 4 mini-game trò chơi dân gian (logic trong Assets/_Game/Scripts/FolkGames/FolkModels.cs) để cân độ khó.
// Ba trình độ: mới chơi, trung bình, giỏi. Bot chỉ dùng những gì người chơi nhìn thấy trên màn hình, kèm sai số
// phản xạ, sai số ước lượng như người thật.
//   py -3 run_sim.py   (biên dịch bằng csc của Unity rồi chạy bằng dotnet đi kèm Unity)
using System;
using System.Collections.Generic;
using Tami;

static class Sim
{
    const float Dt = 1f / 60f;
    const float Aspect = 3.15f; // khớp FolkView.Aspect (sân 1208 x 384)
    static readonly string[] Skills = { "mới chơi", "trung bình", "giỏi" };

    static float Gauss(Random r, float sigma)
    {
        double u1 = 1.0 - r.NextDouble(), u2 = r.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2)) * sigma;
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int games = args.Length > 0 ? int.Parse(args[0]) : 2000;
        Report("Đập niêu", games, PlayPot);
        Report("Đá cầu", games, PlayShuttle);
        Report("Tuho", games, PlayTuho);
    }

    static void Report(string name, int games, Func<int, int, FolkModel> play)
    {
        Console.WriteLine("== " + name);
        for (int s = 0; s < 3; s++)
        {
            double sum = 0, pts = 0, time = 0;
            int half = 0, full = 0;
            for (int i = 0; i < games; i++)
            {
                var m = play(s, i * 7919 + s);
                sum += m.Score01;
                pts += m.Points;
                time += m.Time;
                if (m.Score01 >= 0.5f) half++;
                if (m.Score01 >= 0.999f) full++;
            }
            Console.WriteLine($"  {Skills[s],-11} điểm TB {pts / games,5:0.0}  tỉ lệ TB {sum / games * 100,3:0}%  >=nửa {half * 100.0 / games,3:0}%  trọn {full * 100.0 / games,3:0}%  thời gian {time / games,4:0.0}s");
        }
    }

    // ---------------- Đập niêu ----------------
    // Bot đi theo hướng cả xóm hô (người mới hay nghe cả Cu Tí), tới "Gần lắm rồi" thì dò theo tiếng reo:
    // bước một bước, chờ tiếng reo ổn định rồi so; reo nhỏ đi thì lùi lại một bước và đập.
    static FolkModel PlayPot(int skill, int seed)
    {
        var r = new Random(seed ^ 0x5151);
        var m = new PotSmashModel(Aspect, seed);
        float memErr = new[] { 0.4f, 0.22f, 0.1f }[skill];
        float trustPrank = new[] { 0.5f, 0.2f, 0.04f }[skill];
        float settle = new[] { 0.25f, 0.4f, 0.5f }[skill];
        float hear = new[] { 0.1f, 0.06f, 0.03f }[skill];
        float react = new[] { 0.4f, 0.28f, 0.16f }[skill];
        float impatient = new[] { 0.62f, 0.8f, 2f }[skill]; // đập luôn khi tiếng reo vượt ngưỡng này
        float belief = 0f, timer = 0f, lastHintAge = 99f, lastCheer = -1f;
        bool beliefSet = false, searching = false;
        int dir = 1;
        float swingAt = -1f;
        while (!m.Finished && m.Time < 60f)
        {
            if (m.State == PotSmashModel.Phase.Blind)
            {
                if (!beliefSet)
                {
                    foreach (var p in m.Pots) if (p.kind == PotSmashModel.PotKind.Prize) belief = p.baseX + Gauss(r, memErr);
                    beliefSet = true;
                }
                if (m.HintAge < lastHintAge && swingAt < 0f)
                {
                    bool follow = !m.HintIsPrank || r.NextDouble() < trustPrank;
                    string h = m.HintText;
                    if (follow)
                    {
                        if (h == "ĐẬP ĐI!") swingAt = m.Time + react;
                        else if (h == "Gần lắm rồi!") { if (!searching) { searching = true; lastCheer = -1f; dir = belief >= m.WalkerX ? 1 : -1; } }
                        else if (h.Contains("phải")) { belief = Math.Max(belief, m.WalkerX + 0.4f); searching = false; }
                        else if (h.Contains("trái")) { belief = Math.Min(belief, m.WalkerX - 0.4f); searching = false; }
                    }
                }
                lastHintAge = m.HintAge;
                timer -= Dt;
                if (swingAt >= 0f)
                {
                    if (m.Time >= swingAt) { m.Action(); swingAt = -1f; searching = false; lastCheer = -1f; timer = 0.6f; }
                }
                else if (timer <= 0f)
                {
                    float heard = m.Cheer + Gauss(r, hear);
                    if (searching)
                    {
                        if (heard >= impatient) swingAt = m.Time + react;
                        else if (lastCheer >= 0f && heard < lastCheer)
                        {
                            // Đi quá: lùi một bước rồi đập.
                            dir = -dir;
                            m.Walk(dir);
                            swingAt = m.Time + settle + react;
                        }
                        else
                        {
                            lastCheer = heard;
                            m.Walk(dir);
                        }
                        timer = settle;
                    }
                    else
                    {
                        float d = belief - m.WalkerX;
                        if (Math.Abs(d) > 0.08f) m.Walk(d < 0 ? -1 : 1);
                        timer = 0.22f;
                    }
                }
            }
            m.Update(Dt);
        }
        return m;
    }

    // ---------------- Đá cầu ----------------
    static FolkModel PlayShuttle(int skill, int seed)
    {
        var r = new Random(seed ^ 0x7777);
        var m = new ShuttlecockModel(Aspect, seed);
        float posErr = new[] { 0.13f, 0.085f, 0.05f }[skill];
        float lag = new[] { 0.14f, 0.09f, 0.05f }[skill];
        float plan = float.NaN;
        float prevVY = 0f;
        while (!m.Finished && m.Time < 90f)
        {
            // Cầu vừa bắt đầu rơi: chọn độ cao định đá.
            if (prevVY >= 0f && m.VY < 0f) plan = Range(r, 0.1f, 0.3f) + Gauss(r, posErr);
            prevVY = m.VY;
            if (!float.IsNaN(plan) && m.VY < 0f && m.Y <= plan)
            {
                plan = float.NaN;
                // Thấy vị trí hơi cũ (phản xạ chậm); ngắm đẩy cầu về giữa sân.
                float seenX = m.X - m.VX * lag, seenY = m.Y - m.VY * lag;
                float wantVX = (Aspect * 0.5f - m.X) * 0.5f;
                m.PointerDown(seenX - wantVX / 3f + Gauss(r, posErr), seenY + Gauss(r, posErr));
            }
            m.Update(Dt);
        }
        return m;
    }

    static float Range(Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // ---------------- Tuho ----------------
    static FolkModel PlayTuho(int skill, int seed)
    {
        var r = new Random(seed ^ 0x3131);
        var m = new TuhoModel(Aspect, seed);
        float angErr = new[] { 4f, 2.4f, 1.4f }[skill];
        float powErr = new[] { 0.05f, 0.03f, 0.017f }[skill];
        float windRead = new[] { 0.6f, 0.85f, 0.95f }[skill]; // mức tính tới gió (đường chấm đã gồm gió)
        int lastThrown = -1;
        while (!m.Finished && m.Time < 90f)
        {
            if (!m.Flying && m.Thrown != lastThrown && !m.Finished)
            {
                lastThrown = m.Thrown;
                var (vx, vy) = BestThrow(m, m.Wind * windRead);
                double ang = Math.Atan2(vy, vx) + Gauss(r, angErr) * Math.PI / 180.0;
                double sp = Math.Sqrt(vx * vx + vy * vy) * (1 + Gauss(r, powErr));
                sp = Math.Min(sp, TuhoModel.MaxSpeed);
                m.Throw((float)(Math.Cos(ang) * sp), (float)(Math.Sin(ang) * sp));
            }
            m.Update(Dt);
        }
        return m;
    }

    // Tìm cú ném rơi đúng giữa miệng bình (với mức gió bot đoán được), góc ném 40-65 độ.
    static (float, float) BestThrow(TuhoModel m, float wind)
    {
        float bestErr = float.MaxValue, bvx = 2, bvy = 2;
        for (float a = 40; a <= 65; a += 1f)
            for (float sp = 1.5f; sp <= TuhoModel.MaxSpeed; sp += 0.02f)
            {
                float vx = sp * (float)Math.Cos(a * Math.PI / 180), vy = sp * (float)Math.Sin(a * Math.PI / 180);
                // Thời điểm rơi qua độ cao miệng bình (nghiệm lớn).
                float A = 0.5f * TuhoModel.Gravity, B = -vy, C = TuhoModel.MouthY - m.ThrowY;
                float disc = B * B - 4 * A * C;
                if (disc < 0) continue;
                float t = (-B + (float)Math.Sqrt(disc)) / (2 * A);
                float x = m.ThrowX + vx * t + 0.5f * wind * t * t;
                float err = Math.Abs(x - m.JarX);
                if (err < bestErr) { bestErr = err; bvx = vx; bvy = vy; }
            }
        return (bvx, bvy);
    }
}
