using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>
    /// Toàn bộ giao diện, dựng bằng code (GDD mục 8). File này: khung chính, HUD, thanh tab, thông báo,
    /// hiệu ứng thu tiền. Các bảng nằm ở UIManager.Panels.cs và UIManager.Menus.cs, hội thoại ở
    /// UIManager.Overlays.cs.
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        // Bố cục màn hình ngang, không có thanh nền: hàng icon trên cùng (thành tựu, cài đặt bên trái; xu, Ngọc
        // bên phải) và cột 3 nút tròn sát mép phải. Bảng (modal) nằm giữa, chừa hai khoảng này.
        const float TopBarHeight = 110f;
        const float SideColumnWidth = 200f;
        const float SideButtonSize = 150f;
        const float PanelWidth = 1280f;
        const float MaxPanelHeight = 860f;
        static readonly Color HudOutline = new Color(0.24f, 0.14f, 0.1f, 0.9f);

        RectTransform root, hudLayer, joystickLayer, modalLayer, tabLayer, overlayLayer, toastLayer;
        RectTransform joystickBase, joystickKnob;
        RectTransform coinIcon;
        Text coinsText, gemsText, toastText, badgeText, boostText;
        GameObject badge;
        CanvasGroup toastGroup;
        float toastUntil, infoTimer, modalTimer;

        GameObject modal;
        RectTransform modalPanel;
        float modalHeight;
        System.Action refreshModal;
        readonly Stack<Image> flyCoinPool = new Stack<Image>();

        public bool ModalOpen => modal != null;

        public void Build()
        {
            UIFont = Game.I.Font;
            var frames = Game.I.art;
            PanelSprite = frames.uiPanel;
            ButtonSprite = frames.uiButton;
            RowSprite = frames.uiRow;
            KnobSprite = frames.uiKnob;
            NameSprite = frames.frameName;
            NoticeSprite = frames.frameNotice;
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            // Mọi lớp giao diện nằm trong vùng an toàn của màn hình điện thoại.
            root = NewRect(go.transform, "SafeArea");
            Fill(root);
            root.gameObject.AddComponent<SafeArea>();

            hudLayer = Layer("HUD");
            joystickLayer = Layer("Joystick");
            modalLayer = Layer("Modals");
            tabLayer = Layer("Tabs");
            overlayLayer = Layer("Overlays");
            toastLayer = Layer("Toast");

            BuildHud();
            BuildSideButtons();
            BuildToast();
            BuildJoystick();

            var g = Game.I;
            g.Economy.OnChanged += RefreshAll;
            g.Street.OnChanged += RefreshAll;
            g.Street.OnStallBuilt += OnStallBuilt;
            g.Street.OnSegmentUnlocked += OnSegmentUnlocked;
            RefreshAll();
        }

        RectTransform Layer(string name)
        {
            var layer = NewRect(root, name);
            Fill(layer);
            // Mỗi lớp là một canvas con: chữ HUD đổi hay toast mờ dần chỉ dựng lại lớp đó, không dựng lại cả giao diện.
            layer.gameObject.AddComponent<Canvas>();
            layer.gameObject.AddComponent<GraphicRaycaster>();
            return layer;
        }

        // ---------- HUD ----------

        void BuildHud()
        {
            var art = Game.I.art;
            const float y = -TopBarHeight * 0.5f;

            // Bên trái: thành tựu (kèm số thưởng chờ nhận) và cài đặt.
            var trophy = IconButton(hudLayer, art.iconTrophy, OpenAchievementsPanel);
            Place((RectTransform)trophy.transform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(76f, y), new Vector2(96f, 96f));
            badge = Img(trophy.transform, KnobSprite, new Color(0.95f, 0.25f, 0.3f), "Badge").gameObject;
            Place((RectTransform)badge.transform, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(44f, 44f));
            badgeText = Label(badge.transform, "", 28, Color.white, TextAnchor.MiddleCenter);
            badgeText.fontStyle = FontStyle.Bold;
            Fill(badgeText.rectTransform);

            var settings = IconButton(hudLayer, art.iconSettings, OpenSettingsPanel);
            Place((RectTransform)settings.transform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(190f, y), new Vector2(92f, 92f));

            // Bên phải: xu rồi Ngọc, chữ có viền tối để đọc được ngay trên nền phố.
            var coin = Img(hudLayer, art.coin, Color.white, "CoinIcon");
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            Place(coin.rectTransform, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-470f, y), new Vector2(80f, 80f));
            coinIcon = coin.rectTransform;
            coinsText = HudLabel(new Color(1f, 0.93f, 0.6f), -420f, -250f, y);

            var gem = Img(hudLayer, art.gem, Color.white, "GemIcon");
            gem.preserveAspect = true;
            gem.raycastTarget = false;
            Place(gem.rectTransform, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-200f, y), new Vector2(74f, 74f));
            gemsText = HudLabel(new Color(0.78f, 0.93f, 1f), -154f, -24f, y);

            // Dưới xu và Ngọc: tăng thu nhập đang có (thưởng trò chơi dân gian), chỉ hiện khi còn hiệu lực.
            boostText = Label(hudLayer, "", 32, new Color(1f, 0.62f, 0.85f), TextAnchor.MiddleRight);
            boostText.fontStyle = FontStyle.Bold;
            Place(boostText.rectTransform, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-24f, y - 58f), new Vector2(520f, 44f));
            var boostOutline = boostText.gameObject.AddComponent<Outline>();
            boostOutline.effectColor = HudOutline;
            boostOutline.effectDistance = new Vector2(2f, -2f);
        }

        /// <summary>Số liệu trên HUD: chữ đậm có viền, căn trái trong khoảng [left, right] tính từ mép phải.</summary>
        Text HudLabel(Color color, float left, float right, float y)
        {
            var t = Label(hudLayer, "0", 52, color, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            Place(t.rectTransform, Vector2.one, new Vector2(0f, 0.5f), new Vector2(left, y), new Vector2(right - left, 80f));
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = HudOutline;
            outline.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        /// <summary>Nút chỉ có hình (không nền), nhấn thì hình tối đi một chút.</summary>
        Button IconButton(Transform parent, Sprite icon, UnityEngine.Events.UnityAction onClick)
        {
            var img = Img(parent, icon, Color.white, "IconButton");
            img.preserveAspect = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            b.colors = colors;
            b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Cột 3 nút tròn sát mép phải (thay thanh menu ở đáy). Nằm trên lớp bảng nên vẫn bấm được khi đang mở bảng.</summary>
        void BuildSideButtons()
        {
            var art = Game.I.art;
            SideButton(art.tabManager, "Người bán", OpenManagersPanel, 0);
            SideButton(art.tabDecor, "Trang trí", () => Game.I.Decor.Enter(), 1);
            SideButton(art.tabShop, "Thông số", OpenStatsPanel, 2);
        }

        void SideButton(Sprite icon, string title, UnityEngine.Events.UnityAction onClick, int index)
        {
            const float step = SideButtonSize + 50f;
            float y = (1 - index) * step + 20f;
            var b = IconButton(tabLayer, icon, onClick);
            Place((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-SideColumnWidth * 0.5f, y), new Vector2(SideButtonSize, SideButtonSize));
            var label = Label(b.transform, title, 28, Color.white, TextAnchor.UpperCenter);
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(SideColumnWidth, 40f));
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = HudOutline;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        void BuildToast()
        {
            // Khung thông báo viền mảnh, nền kem.
            var toast = NoticeSprite != null
                ? Framed(Img(toastLayer, null, new Color(0.98f, 0.98f, 0.97f), "Toast"), NoticeSprite).rectTransform
                : Panel(toastLayer, "Toast").rectTransform;
            Place(toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -TopBarHeight - 16f), new Vector2(1100f, 96f));
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastText = Label(toast, "", 36, Ink, TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
        }

        const float JoystickSize = 220f;

        /// <summary>Joystick nổi: vòng nền mờ hiện ở chỗ bắt đầu chạm, núm đi theo ngón tay.</summary>
        void BuildJoystick()
        {
            var ring = Img(joystickLayer, KnobSprite, new Color(1f, 1f, 1f, 0.35f), "JoystickBase");
            ring.raycastTarget = false;
            ring.preserveAspect = false;
            joystickBase = ring.rectTransform;
            joystickBase.sizeDelta = new Vector2(JoystickSize, JoystickSize);
            var knob = Img(joystickBase, KnobSprite, new Color(1f, 1f, 1f, 0.9f), "JoystickKnob");
            knob.raycastTarget = false;
            knob.preserveAspect = false;
            joystickKnob = knob.rectTransform;
            joystickKnob.sizeDelta = new Vector2(JoystickSize * 0.45f, JoystickSize * 0.45f);
            joystickBase.gameObject.SetActive(false);
        }

        void UpdateJoystick()
        {
            bool on = CameraScroller.JoystickActive;
            if (joystickBase.gameObject.activeSelf != on) joystickBase.gameObject.SetActive(on);
            if (!on) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickLayer, CameraScroller.JoystickOrigin, null, out var origin);
            joystickBase.anchoredPosition = origin;
            joystickKnob.anchoredPosition = CameraScroller.JoystickVector * (JoystickSize * 0.36f);
        }

        void Update()
        {
            UpdateJoystick();
            infoTimer -= Time.unscaledDeltaTime;
            if (infoTimer <= 0f)
            {
                infoTimer = 0.25f;
                RefreshInfo();
            }

            if (folkGame != null)
            {
                if (modal == null) folkGame = null;
                else folkGame.Tick(Time.unscaledDeltaTime);
            }

            if (modal != null)
            {
                FitModal();
                modalTimer -= Time.unscaledDeltaTime;
                if (modalTimer <= 0f)
                {
                    modalTimer = 0.5f;
                    refreshModal?.Invoke();
                }
            }

            if (toastGroup.alpha > 0f && Time.unscaledTime > toastUntil)
            {
                toastGroup.alpha = Mathf.Max(0f, toastGroup.alpha - Time.unscaledDeltaTime * 2.5f);
            }
        }

        void RefreshAll()
        {
            if (coinsText == null) return;
            coinsText.text = NumberFormat.Short(Game.I.Economy.Coins);
            gemsText.text = Game.I.Economy.Gems.ToString();
            RefreshInfo();
            refreshModal?.Invoke();
            RefreshDecorBar();
        }

        void RefreshInfo()
        {
            int claimable = Game.I.Achievements.ClaimableCount;
            badge.SetActive(claimable > 0);
            badgeText.text = claimable.ToString();

            var g = Game.I;
            boostText.text = g.IncomeBoostActive
                ? "Thu nhập x" + g.IncomeBoostMultiplier.ToString("0.#") + " · " + NumberFormat.Clock(g.IncomeBoostUntil - Time.time)
                : "";
        }

        public void Toast(string message)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastGroup.alpha = 1f;
            toastUntil = Time.unscaledTime + 2.8f;
        }

        // ---------- Khung bảng (modal) ----------

        RectTransform OpenModal(string title, float desiredHeight)
        {
            CloseModal();
            modal = new GameObject("Modal", typeof(RectTransform));
            modal.transform.SetParent(modalLayer, false);
            var modalRect = (RectTransform)modal.transform;
            Fill(modalRect);

            var dim = Img(modalRect, null, new Color(0f, 0f, 0f, 0.55f), "Dim");
            Fill(dim.rectTransform);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(CloseModal);

            // Bảng nằm giữa vùng dưới hàng icon trên cùng.
            float height = Mathf.Min(desiredHeight, MaxPanelHeight);
            var panel = Panel(modalRect).rectTransform;
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -TopBarHeight * 0.5f), new Vector2(PanelWidth, height));
            modalPanel = panel;
            modalHeight = height;
            FitModal();
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // chặn click xuyên xuống lớp mờ

            var titleText = Label(panel, title, 52, Accent, TextAnchor.MiddleLeft);
            titleText.fontStyle = FontStyle.Bold;
            Stretch(titleText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -120f), new Vector2(-140f, -20f));

            var close = Btn(panel, "X", CloseColor, CloseModal);
            Place((RectTransform)close.transform, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(96f, 96f));

            var content = NewRect(panel, "Content");
            Stretch(content, Vector2.zero, Vector2.one, new Vector2(36f, 36f), new Vector2(-36f, -140f));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            modalTimer = 0.5f;
            return content;
        }

        /// <summary>Thu nhỏ bảng cho vừa màn hình nhỏ hoặc tỉ lệ khác 16:9, giữ nguyên bố cục bên trong.</summary>
        void FitModal()
        {
            if (modalPanel == null) return;
            float availableHeight = root.rect.height - TopBarHeight - 40f;
            // Bảng nằm giữa nên chừa cột nút bên phải ở cả hai bên.
            float availableWidth = root.rect.width - 2f * SideColumnWidth - 20f;
            float scale = availableHeight > 100f && availableWidth > 100f
                ? Mathf.Min(1f, availableHeight / modalHeight, availableWidth / PanelWidth)
                : 1f;
            // Gọi mỗi khung khi đang mở bảng: chỉ gán khi đổi để không làm canvas dựng lại.
            var s = new Vector3(scale, scale, 1f);
            if (modalPanel.localScale != s) modalPanel.localScale = s;
        }

        public void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null;
            modalPanel = null;
            refreshModal = null;
            folkGame = null;
        }

        // ---------- Hiệu ứng thu tiền ----------

        public void OnCoinsCollected(Vector3 worldPos, double amount)
        {
            FloatingText.Spawn(worldPos, "+" + NumberFormat.Short(amount), new Color(1f, 0.85f, 0.25f));
            StartCoroutine(FlyCoins(Game.I.Cam.WorldToScreenPoint(worldPos), 5));
            RefreshInfo();
        }

        IEnumerator FlyCoins(Vector3 from, int count)
        {
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(FlyOne(from + (Vector3)(Random.insideUnitCircle * 30f)));
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }

        IEnumerator FlyOne(Vector3 from)
        {
            // Đồng xu bay dùng lại từ kho, không tạo/xoá object mỗi lần thu tiền.
            var img = flyCoinPool.Count > 0 ? flyCoinPool.Pop() : null;
            if (img == null)
            {
                img = Img(toastLayer, Game.I.art.coin, Color.white, "FlyCoin");
                img.raycastTarget = false;
                img.rectTransform.sizeDelta = new Vector2(60f, 60f);
            }
            img.gameObject.SetActive(true);
            var rt = img.rectTransform;
            rt.position = from;
            Vector3 mid = from + new Vector3(Random.Range(-140f, 140f), 180f, 0f);

            const float duration = 0.55f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                k *= k;
                Vector3 a = Vector3.Lerp(from, mid, k);
                Vector3 b = Vector3.Lerp(mid, coinIcon.position, k);
                rt.position = Vector3.Lerp(a, b, k);
                yield return null;
            }
            img.gameObject.SetActive(false);
            flyCoinPool.Push(img);
            StartCoroutine(Pulse(coinIcon));
        }

        static IEnumerator Pulse(RectTransform rt)
        {
            const float duration = 0.15f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.one * (1f + 0.2f * Mathf.Sin(t / duration * Mathf.PI));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ---------- Sự kiện con phố ----------

        void OnStallBuilt(Stall stall)
        {
            var data = stall.Data;
            if (!string.IsNullOrEmpty(data.introLine)) ShowDialogue(new[] { new DialogueLine(data.introSpeaker, data.introLine) });
        }

        void OnSegmentUnlocked(SegmentInfo info)
        {
            if (info.dialogue != null && info.dialogue.Length > 0) ShowDialogue(info.dialogue);
        }
    }
}
