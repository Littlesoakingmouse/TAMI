using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tami
{
    [System.Serializable]
    public class StallSave
    {
        public int slot;
        public string id;
        public int level = 1;
        [Tooltip("Số món đã mở (1-3); 0 là save cũ, suy ra từ cấp")]
        public int dishes;
        public int revenue;
        [Tooltip("Đã có người bán sau quầy")]
        public bool manager;
        [Tooltip("Đã có người chào hàng trước quầy")]
        public bool barker;
        public double vault;
        public double rate;
    }

    [System.Serializable]
    public class DecorSave
    {
        [Tooltip("Chỗ trang trí cố định của bản cũ (-1 khi dùng toạ độ x, y)")]
        public int spot;
        public string id;
        public float x, y;
    }

    [System.Serializable]
    public class IdCount
    {
        public string id;
        public long count;
    }

    [System.Serializable]
    public class StatsData
    {
        public long totalSales;
        public List<IdCount> salesByStall = new List<IdCount>();
        public int specialGuests;
        public double coinsCollected;
        public int performances;
        public int decorationsPlaced;
        public int mealsEaten;
        public int npcGamesPlayed;
        public int folkGamesPlayed;

        public long SalesOf(string id)
        {
            foreach (var c in salesByStall) if (c.id == id) return c.count;
            return 0;
        }

        public void AddSale(string id)
        {
            totalSales++;
            foreach (var c in salesByStall)
            {
                if (c.id != id) continue;
                c.count++;
                return;
            }
            salesByStall.Add(new IdCount { id = id, count = 1 });
        }
    }

    [System.Serializable]
    public class SettingsData
    {
        // Công tắc bật/tắt của bản cũ: chỉ đọc một lần để chuyển sang âm lượng (AudioManager.Init).
        public bool music = true;
        public bool sfx = true;
        public bool vibration = true;

        // Âm lượng 0..1 theo từng loại âm thanh, nhân với âm lượng tổng.
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float effectVolume = 1f;
        public float coinVolume = 1f;
        public float uiVolume = 1f;
    }

    /// <summary>Toàn bộ tiến trình người chơi (GDD mục 9.4).</summary>
    [System.Serializable]
    public class SaveData
    {
        public int version = 1;
        public double coins;
        public int gems;
        public int segments = 1;
        /// <summary>Số khu dọc đường dưới đã mở (mở lần lượt).</summary>
        public int plazaZones;
        /// <summary>Số trò chơi dân gian đã mở (mở lần lượt). -1: save cũ chưa có trường này, mở khu là có sẵn 2 trò.</summary>
        public int folkGamesUnlocked = -1;
        /// <summary>Các trò chơi dân gian đã xem hướng dẫn lần đầu (bit theo FolkGameKind).</summary>
        public int folkTutorialSeen;
        [Tooltip("Bố cục map khi lưu: 0 = bản cũ 4 ô mỗi khu, 2 = 3 ô mỗi khu")]
        public int layoutVersion;
        public List<StallSave> stalls = new List<StallSave>();
        public List<DecorSave> decorations = new List<DecorSave>();
        public List<string> performers = new List<string>();
        public List<string> claimedAchievements = new List<string>();
        public StatsData stats = new StatsData();
        public SettingsData settings = new SettingsData();
        public int offlineUpgradeLevel;
        public int managerTokens;
        public bool introSeen;
        public bool tutorialDone;
        public long lastSaveUtcTicks;
        public double playSeconds;

        public void EnsureValid()
        {
            stalls ??= new List<StallSave>();
            decorations ??= new List<DecorSave>();
            performers ??= new List<string>();
            claimedAchievements ??= new List<string>();
            stats ??= new StatsData();
            stats.salesByStall ??= new List<IdCount>();
            settings ??= new SettingsData();
        }
    }

    /// <summary>Đọc/ghi save dạng JSON ở persistentDataPath.</summary>
    public static class SaveSystem
    {
        public static string PathOverride;

        public static string FilePath => string.IsNullOrEmpty(PathOverride)
            ? Path.Combine(Application.persistentDataPath, "save.json")
            : PathOverride;

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                data?.EnsureValid();
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Tami] Không đọc được save: " + e.Message);
                return null;
            }
        }

        /// <summary>Ghi save. stampTime = false để giữ nguyên thời điểm lưu (dùng khi chạy thử).</summary>
        public static void Write(SaveData data, bool stampTime = true)
        {
            if (stampTime) data.lastSaveUtcTicks = System.DateTime.UtcNow.Ticks;
            WriteJson(FilePath, JsonUtility.ToJson(data), ++writeSeq);
        }

        /// <summary>
        /// Ghi save ở luồng nền (dùng cho tự lưu khi đang chơi): ghi file trên điện thoại chậm có thể làm khựng
        /// khung hình. JSON vẫn tạo trên luồng chính nên không đụng dữ liệu đang chạy.
        /// </summary>
        public static void WriteInBackground(SaveData data)
        {
            data.lastSaveUtcTicks = System.DateTime.UtcNow.Ticks;
            string path = FilePath, json = JsonUtility.ToJson(data);
            long seq = ++writeSeq;
            System.Threading.Tasks.Task.Run(() => WriteJson(path, json, seq));
        }

        static readonly object WriteLock = new object();
        static long writeSeq, writtenSeq;

        static void WriteJson(string path, string json, long seq)
        {
            lock (WriteLock)
            {
                // Bản cũ hơn bản đã ghi thì bỏ qua, tránh ghi đè save mới bằng save cũ.
                if (seq <= writtenSeq) return;
                writtenSeq = seq;
                try
                {
                    string tmp = path + ".tmp";
                    File.WriteAllText(tmp, json);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Tami] Không ghi được save: " + e.Message);
                }
            }
        }

        public static void Delete()
        {
            lock (WriteLock)
            {
                // Chặn bản tự lưu còn đang chờ ghi ở luồng nền tạo lại file vừa xoá.
                writtenSeq = ++writeSeq;
                try
                {
                    if (File.Exists(FilePath)) File.Delete(FilePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Tami] Không xoá được save: " + e.Message);
                }
            }
        }
    }
}
