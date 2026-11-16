using Unity.Profiling;
using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Đo hiệu năng khi phát triển (chỉ có trong Editor và bản Development): thời gian khung hình,
    /// số khung bị giật và bộ nhớ cấp phát mỗi khung. Đọc bằng <see cref="Report"/>.
    /// </summary>
    public class PerfStats : MonoBehaviour
    {
        const int Window = 600;

        readonly float[] frames = new float[Window];
        readonly long[] allocs = new long[Window];
        int count, head;
        ProfilerRecorder gcAlloc;

        public static PerfStats Instance { get; private set; }

        void OnEnable()
        {
            Instance = this;
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        void OnDisable() => gcAlloc.Dispose();

        void Update()
        {
            frames[head] = Time.unscaledDeltaTime * 1000f;
            allocs[head] = gcAlloc.Valid ? gcAlloc.LastValue : 0;
            head = (head + 1) % Window;
            count = Mathf.Min(count + 1, Window);

            // Bộ đếm FPS góc màn hình: số trung bình và khung chậm nhất trong nửa giây vừa qua.
            labelFrames++;
            labelTime += Time.unscaledDeltaTime;
            labelWorst = Mathf.Max(labelWorst, Time.unscaledDeltaTime);
            if (labelTime >= 0.5f)
            {
                fpsLabel = Mathf.RoundToInt(labelFrames / labelTime) + " FPS  (chậm nhất " + Mathf.RoundToInt(labelWorst * 1000f) + " ms)";
                labelFrames = 0;
                labelTime = labelWorst = 0f;
            }
        }

        int labelFrames;
        float labelTime, labelWorst;
        string fpsLabel = "";
        GUIStyle fpsStyle;

        void Awake() => useGUILayout = false;

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            fpsStyle ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 40), fontStyle = FontStyle.Bold };
            // Ngay dưới thanh trên cùng (cao khoảng 10% màn hình) để không che nút.
            var r = new Rect(10f, Screen.height * 0.11f, Screen.width, fpsStyle.fontSize * 2f);
            fpsStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), fpsLabel, fpsStyle);
            fpsStyle.normal.textColor = Color.yellow;
            GUI.Label(r, fpsLabel, fpsStyle);
        }

        public void Clear() => count = 0;

        /// <summary>Tóm tắt khoảng 10 giây gần nhất.</summary>
        public string Report()
        {
            if (count == 0) return "no data";
            float sum = 0f, max = 0f;
            long allocSum = 0, allocMax = 0;
            int spikes = 0;
            for (int i = 0; i < count; i++)
            {
                sum += frames[i];
                max = Mathf.Max(max, frames[i]);
                if (frames[i] > 25f) spikes++;
                allocSum += allocs[i];
                allocMax = System.Math.Max(allocMax, allocs[i]);
            }
            return "frames=" + count + " avgMs=" + (sum / count).ToString("0.0") + " maxMs=" + max.ToString("0.0")
                + " spikes>25ms=" + spikes + " gcAvgB=" + allocSum / count + " gcMaxB=" + allocMax;
        }
    }
}
