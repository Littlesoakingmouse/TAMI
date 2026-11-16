using System.Collections.Generic;
using UnityEngine;

namespace Tami
{
    /// <summary>Sinh khách theo lưu lượng, chọn loại khách và danh sách quầy sẽ mua.</summary>
    public class NpcSpawner : MonoBehaviour
    {
        readonly List<Npc> active = new List<Npc>();
        // Khách rời phố được cất lại để dùng tiếp, tránh tạo/xoá object liên tục gây giật.
        readonly Stack<Npc> pool = new Stack<Npc>();
        readonly List<NpcRig> groupRigs = new List<NpcRig>();
        float timer = 2f;
        float bloggerUntil = -1f;

        GameConfig Cfg => Game.I.config;

        public int ActiveCount => active.Count;
        public bool BloggerBoostActive => Time.time < bloggerUntil;
        public float BloggerBoostLeft => Mathf.Max(0f, bloggerUntil - Time.time);

        public float SpawnPerMinute
        {
            get
            {
                var street = Game.I.Street;
                float perMinute = Cfg.baseSpawnPerMinute * (1f + street.Attraction / 100f);
                if (BloggerBoostActive) perMinute *= 1f + Cfg.bloggerBoost;
                if (street.PerformActive) perMinute *= Cfg.performSpawnMultiplier;
                return perMinute;
            }
        }

        void Start()
        {
            // Tạo sẵn một ít khách lúc vào game để lần sinh khách đầu không bị khựng.
            for (int i = 0; i < 12; i++)
            {
                var npc = Npc.Create(transform);
                npc.Release();
                pool.Push(npc);
            }
        }

        void Update()
        {
            var street = Game.I.Street;
            if (street == null || street.Stalls.Count == 0) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 60f / SpawnPerMinute * Random.Range(0.7f, 1.3f);
            if (active.Count >= street.MaxCustomers) return;

            SpawnGroup(PickType(street.UnlockedSegments));
        }

        int SpawnGroup(NpcTypeData type)
        {
            var street = Game.I.Street;
            int group = Random.Range(type.groupSizeMin, type.groupSizeMax + 1);
            // Khách vào từ đầu trái (đi từ sau cổng xuống qua lòng cổng) hoặc đầu phải (cuối phố) và đi thẳng sang đầu kia.
            int dir = Random.value < 0.5f ? 1 : -1;
            float leftX = street.exitX;
            float rightX = street.RightX + 3f;
            float startX = dir > 0 ? leftX : rightX;
            float exitX = dir > 0 ? rightX : leftX;
            bool viaGate = dir > 0 && street.HasGateEntry;
            PickGroupRigs();
            for (int i = 0; i < group; i++)
            {
                float lane = Random.Range(street.laneMinY, street.laneMaxY);
                // Qua cổng: cả đoàn nối đuôi nhau theo chiều dọc phía sau cổng.
                var start = viaGate
                    ? new Vector2(street.GateX + street.GateJitter, street.GateBackY + i * 0.7f)
                    : new Vector2(startX - dir * i * 0.7f, lane);
                var npc = pool.Count > 0 ? pool.Pop() : Npc.Create(transform);
                var rig = groupRigs.Count > 0 ? groupRigs[i % groupRigs.Count] : null;
                npc.Setup(type, PickTargets(type, dir), start, exitX, rig, lane, viaGate);
                active.Add(npc);
            }
            if (type.kind != NpcKind.Normal) Game.I.Data.stats.specialGuests++;
            return group;
        }

        /// <summary>
        /// Cả đoàn khách là cùng một loài: lấy các bộ đồ của một loài ngẫu nhiên, xáo thứ tự để
        /// mỗi người trong đoàn mặc một bộ khác nhau (đoàn đông hơn số bộ đồ thì mới lặp lại).
        /// </summary>
        void PickGroupRigs()
        {
            groupRigs.Clear();
            var rigs = Game.I.art.npcRigs;
            if (rigs == null || rigs.Length == 0) return;
            string species = rigs[Random.Range(0, rigs.Length)].Species;
            foreach (var r in rigs) if (r.Species == species) groupRigs.Add(r);
            for (int i = groupRigs.Count - 1; i > 0; i--)
            {
                int k = Random.Range(0, i + 1);
                (groupRigs[i], groupRigs[k]) = (groupRigs[k], groupRigs[i]);
            }
        }

        /// <summary>Sinh ngay một nhóm khách thuộc loại cho trước (dùng khi chạy thử).</summary>
        public int SpawnForTest(NpcTypeData type) => SpawnGroup(type);

        public Npc LastSpawned => active.Count > 0 ? active[active.Count - 1] : null;

        NpcTypeData PickType(int unlockedSegments)
        {
            float total = 0f;
            foreach (var t in Cfg.npcTypes)
            {
                if (t.unlockSegment <= unlockedSegments) total += t.spawnWeight;
            }

            float roll = Random.value * total;
            foreach (var t in Cfg.npcTypes)
            {
                if (t.unlockSegment > unlockedSegments) continue;
                roll -= t.spawnWeight;
                if (roll <= 0f) return t;
            }
            return Cfg.npcTypes[0];
        }

        List<Stall> PickTargets(NpcTypeData type, int dir)
        {
            var stalls = Game.I.Street.Stalls;
            var picked = new List<Stall>();
            if (type.kind == NpcKind.Mystery) return picked;

            if (type.kind == NpcKind.FoodBlogger)
            {
                picked.AddRange(stalls);
            }
            else
            {
                int max = Mathf.Min(type.maxStalls, stalls.Count);
                int min = Mathf.Min(type.minStalls, max);
                int count = Random.Range(min, max + 1);
                var pool = new List<Stall>(stalls);
                for (int i = 0; i < count; i++)
                {
                    int k = Random.Range(0, pool.Count);
                    picked.Add(pool[k]);
                    pool.RemoveAt(k);
                }
            }

            // Người chào hàng mời thêm khách vốn không định ghé quầy của mình.
            foreach (var s in stalls)
            {
                if (s.HasBarker && !picked.Contains(s) && Random.value < Cfg.barkerInviteChance) picked.Add(s);
            }

            // Ghé quầy theo đúng chiều đi, không quay lại.
            picked.Sort((a, b) => dir * a.Slot.X.CompareTo(b.Slot.X));
            return picked;
        }

        public void Despawn(Npc npc)
        {
            active.Remove(npc);
            npc.Release();
            pool.Push(npc);
        }

        public void OnBloggerLeft()
        {
            bloggerUntil = Time.time + Cfg.bloggerBoostDuration;
            Game.I.UI.Toast("Food blogger vừa review con phố! Khách +"
                + Mathf.RoundToInt(Cfg.bloggerBoost * 100f) + "% trong "
                + Mathf.RoundToInt(Cfg.bloggerBoostDuration / 60f) + " phút");
        }
    }
}
