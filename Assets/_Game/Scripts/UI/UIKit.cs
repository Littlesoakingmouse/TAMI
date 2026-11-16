using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tami
{
    /// <summary>Các hàm dựng uGUI bằng code và bảng màu chung.</summary>
    public static class UIKit
    {
        public static Font UIFont;
        // Khung 9-slice theo nét của game (GameArt.uiPanel...); thiếu hình thì dùng màu trơn như cũ.
        public static Sprite PanelSprite, ButtonSprite, RowSprite, KnobSprite, NameSprite, NoticeSprite;

        // Nền trung tính nhẹ (bảng trắng ngà, dòng xám rất nhạt); chữ nâu ấm, tiêu đề san hô.
        public static readonly Color BarColor = new Color(0.99f, 0.99f, 0.98f, 0.96f);
        public static readonly Color PanelColor = new Color(0.99f, 0.99f, 0.98f, 1f);
        public static readonly Color RowColor = new Color(0.95f, 0.95f, 0.94f, 1f);
        public static readonly Color Ink = new Color(0.36f, 0.22f, 0.18f);
        public static readonly Color Accent = new Color(0.93f, 0.45f, 0.35f);
        public static readonly Color Muted = new Color(0.62f, 0.48f, 0.44f);
        /// <summary>Rãnh thanh trượt, thanh canh thời điểm trong mini-game.</summary>
        public static readonly Color TrackColor = new Color(0.88f, 0.88f, 0.87f);

        // Nút pastel trung tính, mỗi màu một chức năng (viền nút nhuộm theo màu nút).
        /// <summary>Xác nhận, mua, bật: xanh lá xô thơm.</summary>
        public static readonly Color Ok = new Color(0.56f, 0.78f, 0.65f);
        /// <summary>Không bấm được, huỷ, tắt: xám nhạt.</summary>
        public static readonly Color Off = new Color(0.80f, 0.80f, 0.82f);
        /// <summary>Nhận thưởng, xu: vàng bơ.</summary>
        public static readonly Color Gold = new Color(0.95f, 0.77f, 0.50f);
        /// <summary>Ngọc, thông tin: xanh da trời nhạt.</summary>
        public static readonly Color GemBlue = new Color(0.56f, 0.72f, 0.90f);
        /// <summary>Đóng, xoá, nguy hiểm: hồng đất.</summary>
        public static readonly Color CloseColor = new Color(0.91f, 0.58f, 0.60f);
        /// <summary>Âm thanh, nhạc: tím oải hương.</summary>
        public static readonly Color Lavender = new Color(0.72f, 0.65f, 0.88f);
        /// <summary>Hiệu ứng, chuyển động: cam đào.</summary>
        public static readonly Color Peach = new Color(0.96f, 0.69f, 0.56f);

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(Transform parent, Sprite sprite, Color color, string name = "Image")
        {
            var img = NewRect(parent, name).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = sprite != null;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var t = NewRect(parent, "Text").gameObject.AddComponent<Text>();
            t.font = UIFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        /// <summary>Gắn khung 9-slice cho ảnh (kéo giãn giữ nguyên góc bo và viền).</summary>
        public static Image Framed(Image img, Sprite frame)
        {
            if (frame == null) return img;
            img.sprite = frame;
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            return img;
        }

        /// <summary>Bảng nền nâu gỗ có viền (bảng, thông báo, thanh công cụ); thiếu hình thì nền tối trơn.</summary>
        public static Image Panel(Transform parent, string name = "Panel")
        {
            var img = Img(parent, null, PanelSprite != null ? Color.white : PanelColor, name);
            return Framed(img, PanelSprite);
        }

        public static Button Btn(Transform parent, string label, Color color, UnityAction onClick, int fontSize = 40)
        {
            var img = Framed(Img(parent, null, color, "Button"), ButtonSprite);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() =>
            {
                if (Game.I != null) Game.I.Audio.Play(Sfx.Click);
            });
            b.onClick.AddListener(onClick);
            var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            // Bóng mờ cho chữ trắng đọc rõ trên nút màu nhạt.
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(2f, -2f);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            return b;
        }

        /// <summary>Danh sách cuộn dọc; trả về khung chứa các dòng.</summary>
        public static RectTransform ScrollList(RectTransform parent, float height)
        {
            var view = NewRect(parent, "Viewport");
            Height(view, height);
            var viewLayout = view.GetComponent<LayoutElement>();
            viewLayout.flexibleHeight = 1f;
            viewLayout.minHeight = 200f;
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            view.gameObject.AddComponent<RectMask2D>();

            var list = NewRect(view, "List");
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = Vector2.zero;
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return list;
        }

        public static RectTransform Row(RectTransform parent, float height)
        {
            var row = Framed(Img(parent, null, RowColor, "Row"), RowSprite).rectTransform;
            Height(row, height);
            return row;
        }

        /// <summary>Dòng thanh trượt 0..100%: tên bên trái, thanh ở giữa, phần trăm bên phải.</summary>
        public static Slider SliderRow(RectTransform parent, string title, float value, UnityAction<float> onChange) =>
            SliderRow(parent, title, value, onChange, Accent);

        public static Slider SliderRow(RectTransform parent, string title, float value, UnityAction<float> onChange, Color fillColor)
        {
            var row = Row(parent, 96f);
            var label = Label(row, title, 34, Ink, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, Vector2.zero, new Vector2(0.34f, 1f), new Vector2(28f, 0f), Vector2.zero);
            var percent = Label(row, "", 34, Ink, TextAnchor.MiddleRight);
            percent.fontStyle = FontStyle.Bold;
            Stretch(percent.rectTransform, new Vector2(0.86f, 0f), Vector2.one, Vector2.zero, new Vector2(-28f, 0f));

            var area = NewRect(row, "Slider");
            Stretch(area, new Vector2(0.36f, 0.5f), new Vector2(0.84f, 0.5f), new Vector2(0f, -22f), new Vector2(0f, 22f));
            var track = Framed(Img(area, null, TrackColor, "Track"), RowSprite);
            Stretch(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -8f), new Vector2(0f, 8f));
            var fillArea = NewRect(area, "FillArea");
            Stretch(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -8f), new Vector2(0f, 8f));
            var fill = Framed(Img(fillArea, null, fillColor, "Fill"), RowSprite);
            Stretch(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            var handleArea = NewRect(area, "HandleArea");
            Stretch(handleArea, Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            var handle = Img(handleArea, KnobSprite, Color.white, "Handle");
            handle.rectTransform.sizeDelta = new Vector2(44f, 22f);

            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            percent.text = Mathf.RoundToInt(value * 100f) + "%";
            slider.onValueChanged.AddListener(v =>
            {
                percent.text = Mathf.RoundToInt(v * 100f) + "%";
                onChange(v);
            });
            return slider;
        }

        public static RectTransform HLayout(RectTransform parent, float height, float spacing)
        {
            var r = NewRect(parent, "Buttons");
            Height(r, height);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return r;
        }

        public static void Height(Component c, float height)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }

        public static void SetText(Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        public static void Look(Button b, bool enabled, Color onColor) => b.image.color = enabled ? onColor : Off;

        public static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Fill(RectTransform rt) => Stretch(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static string CountryName(Origin origin) => origin == Origin.VN ? "Việt Nam" : "Hàn Quốc";
    }
}
