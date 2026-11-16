using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tami
{
    /// <summary>
    /// Màn Loading (cảnh Loading, chạy trước cảnh Main): đầu nhân vật chính nhún nhảy làm biểu tượng đang tải,
    /// các món ăn bay lên ở nền, thanh tiến trình theo tiến độ nạp cảnh Main.
    /// Chỉ giữ tham chiếu tới vài hình cần dùng (không kéo cả GameArt) để màn này mở thật nhanh.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("Cảnh nạp sau màn Loading")]
        public string nextScene = "Main";
        [Tooltip("Đầu nhân vật chính (biểu tượng đang tải)")]
        public Sprite head;
        [Tooltip("Hình các món ăn bay ở nền")]
        public Sprite[] foods;
        public Font font;
        [Tooltip("Hiện màn Loading ít nhất bấy nhiêu giây để kịp nhìn thấy")]
        public float minSeconds = 2f;

        static readonly Color Background = new Color(0.99f, 0.91f, 0.78f);
        static readonly Color Ink = new Color(0.24f, 0.14f, 0.08f);
        static readonly Color BarBack = new Color(0.24f, 0.14f, 0.08f, 0.18f);
        static readonly Color BarFill = new Color(0.86f, 0.33f, 0.24f);

        RectTransform canvasRect, foodLayer, headRect, barFill;
        Text percent, tip;
        float shownProgress, spawnTimer;

        sealed class Floater
        {
            public RectTransform rt;
            public Image img;
            public float speed, sway, phase, spin, life, age;
        }

        readonly List<Floater> floaters = new List<Floater>();

        static readonly string[] Tips =
        {
            "Đang bày hàng ra sạp...",
            "Đang luộc bánh chưng...",
            "Đang xào tteokbokki...",
            "Đang treo đèn lồng...",
            "Đang mời khách vào hội chợ...",
        };

        void Start()
        {
            Application.targetFrameRate = 60;
            BuildUI();
            StartCoroutine(Load());
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)canvasGo.transform;

            var bg = NewImage(canvasRect, null, Background, "Background");
            Fill(bg.rectTransform);

            // Lớp món ăn bay nằm dưới đầu nhân vật và chữ.
            foodLayer = NewRect(canvasRect, "Foods");
            Fill(foodLayer);

            var headImg = NewImage(canvasRect, head, Color.white, "Head");
            headImg.preserveAspect = true;
            headRect = headImg.rectTransform;
            headRect.anchorMin = headRect.anchorMax = new Vector2(0.5f, 0.56f);
            headRect.pivot = new Vector2(0.5f, 0f);
            headRect.sizeDelta = new Vector2(360f, 270f);
            headRect.anchoredPosition = new Vector2(0f, -110f);

            var title = NewText(canvasRect, "Hội chợ Việt - Hàn", 72, Ink);
            Place(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(1200f, 110f));

            var barBack = NewImage(canvasRect, null, BarBack, "BarBack");
            Place(barBack.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(760f, 30f));
            var fill = NewImage(barBack.rectTransform, null, BarFill, "BarFill");
            barFill = fill.rectTransform;
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(0f, 1f);
            barFill.offsetMin = barFill.offsetMax = Vector2.zero;

            percent = NewText(canvasRect, "0%", 40, Ink);
            Place(percent.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(400f, 60f));
            tip = NewText(canvasRect, Tips[Random.Range(0, Tips.Length)], 34, new Color(Ink.r, Ink.g, Ink.b, 0.7f));
            Place(tip.rectTransform, new Vector2(0.5f, 0.11f), new Vector2(1200f, 60f));

            // Có sẵn vài món đang bay khi màn vừa hiện.
            for (int i = 0; i < 10; i++) SpawnFood(Random.value);
        }

        IEnumerator Load()
        {
            // Đợi một khung hình để màn Loading hiện ra trước khi bắt đầu nạp.
            yield return null;
            // Nạp ngầm ở mức ưu tiên thấp để màn Loading vẫn chạy mượt (món ăn bay, đầu nhún) trong lúc nạp.
            var priority = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            float start = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(nextScene);
            op.allowSceneActivation = false;
            float tipTimer = 0f;
            while (true)
            {
                // Unity báo 0..0,9 khi nạp xong phần dữ liệu; phần còn lại là bật cảnh.
                float loaded = Mathf.Clamp01(op.progress / 0.9f);
                float timed = Mathf.Clamp01((Time.unscaledTime - start) / minSeconds);
                float target = Mathf.Min(loaded, timed);
                shownProgress = Mathf.MoveTowards(shownProgress, target, Time.unscaledDeltaTime * 1.5f);
                SetProgress(shownProgress);

                tipTimer += Time.unscaledDeltaTime;
                if (tipTimer > 1.2f)
                {
                    tipTimer = 0f;
                    tip.text = Tips[Random.Range(0, Tips.Length)];
                }
                if (shownProgress >= 1f) break;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            Application.backgroundLoadingPriority = priority;
            op.allowSceneActivation = true;
        }

        void SetProgress(float p)
        {
            barFill.anchorMax = new Vector2(p, 1f);
            percent.text = Mathf.RoundToInt(p * 100f) + "%";
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            // Đầu nhân vật nhún nhảy và nghiêng qua lại như đang chờ.
            float bounce = Mathf.Abs(Mathf.Sin(t * 4f));
            headRect.anchoredPosition = new Vector2(0f, -110f + bounce * 34f);
            headRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 8f);
            float squash = 1f - (1f - bounce) * 0.08f;
            headRect.localScale = new Vector3(2f - squash, squash, 1f);

            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Random.Range(0.18f, 0.35f);
                SpawnFood(0f);
            }

            float height = canvasRect.rect.height;
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.age += dt;
                var pos = f.rt.anchoredPosition;
                pos.y += f.speed * dt;
                pos.x += Mathf.Sin(f.age * 1.6f + f.phase) * f.sway * dt;
                f.rt.anchoredPosition = pos;
                f.rt.localRotation = Quaternion.Euler(0f, 0f, f.rt.localEulerAngles.z + f.spin * dt);
                // Hiện dần lúc mới bay lên, mờ dần khi gần tới đỉnh màn hình.
                float k = pos.y / Mathf.Max(1f, height);
                float alpha = Mathf.Clamp01(f.age * 2f) * Mathf.Clamp01((1.05f - k) * 3f) * 0.85f;
                f.img.color = new Color(1f, 1f, 1f, alpha);
                if (k > 1.1f)
                {
                    Destroy(f.rt.gameObject);
                    floaters.RemoveAt(i);
                }
            }
        }

        /// <summary>Thêm một món bay lên từ đáy màn hình; startHeight 0..1 để có sẵn món ở giữa màn khi vừa mở.</summary>
        void SpawnFood(float startHeight)
        {
            if (foods == null || foods.Length == 0) return;
            var sprite = foods[Random.Range(0, foods.Length)];
            if (sprite == null) return;
            var img = NewImage(foodLayer, sprite, new Color(1f, 1f, 1f, 0f), "Food");
            img.preserveAspect = true;
            var rt = img.rectTransform;
            // Chỉ bay ở hai bên, chừa cột giữa cho tiêu đề, đầu nhân vật và thanh tải.
            float side = Random.Range(0.03f, 0.3f);
            rt.anchorMin = rt.anchorMax = new Vector2(Random.value < 0.5f ? side : 1f - side, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float size = Random.Range(90f, 170f);
            rt.sizeDelta = new Vector2(size, size);
            float height = canvasRect.rect.height > 0f ? canvasRect.rect.height : 1080f;
            rt.anchoredPosition = new Vector2(0f, startHeight > 0f ? startHeight * height : -size);
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
            floaters.Add(new Floater
            {
                rt = rt,
                img = img,
                speed = Random.Range(140f, 260f),
                sway = Random.Range(20f, 60f),
                phase = Random.value * 6f,
                spin = Random.Range(-30f, 30f),
                age = startHeight > 0f ? 1f : 0f,
            });
        }

        // ---------- Dựng giao diện (không dùng UIKit để màn này không phụ thuộc Game) ----------

        static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image NewImage(Transform parent, Sprite sprite, Color color, string name)
        {
            var img = NewRect(parent, name).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(Transform parent, string text, int size, Color color)
        {
            var t = NewRect(parent, "Text").gameObject.AddComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }
    }
}
