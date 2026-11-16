using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>Giao diện chế độ trang trí: dòng hướng dẫn trên, thanh chọn món kéo ngang ở dưới, nút Đặt/Huỷ/Gỡ.</summary>
    public partial class UIManager
    {
        const float DecorBarHeight = 230f;

        RectTransform decorLayer, decorActions;
        Text decorInfo;
        Button decorPrimary, decorSecondary;
        DecorMode decorMode;
        readonly List<(Button card, Text price, DecorationData data)> decorCards = new List<(Button, Text, DecorationData)>();

        public void ShowDecorBar(DecorMode mode)
        {
            decorMode = mode;
            if (decorLayer == null) BuildDecorBar();
            decorLayer.gameObject.SetActive(true);
            // Cột nút bên phải nhường chỗ cho nút thao tác và thanh chọn món.
            tabLayer.gameObject.SetActive(false);
            RefreshDecorBar();
        }

        public void HideDecorBar()
        {
            if (decorLayer != null) decorLayer.gameObject.SetActive(false);
            tabLayer.gameObject.SetActive(true);
            RefreshInfo();
        }

        void BuildDecorBar()
        {
            var g = Game.I;
            decorLayer = NewRect(root, "DecorMode");
            Fill(decorLayer);
            decorLayer.SetSiblingIndex(tabLayer.GetSiblingIndex() + 1);

            // Dòng hướng dẫn ngay dưới thanh trên cùng.
            var banner = Panel(decorLayer, "Banner").rectTransform;
            Stretch(banner, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -TopBarHeight - 70f), new Vector2(0f, -TopBarHeight));
            decorInfo = Label(banner, "", 30, Accent, TextAnchor.MiddleCenter);
            Fill(decorInfo.rectTransform);

            // Thanh chọn món ở đáy màn hình.
            var bar = Panel(decorLayer, "DecorBar").rectTransform;
            Stretch(bar, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, DecorBarHeight));

            var done = Btn(bar, "Xong", Ok, () => Game.I.Decor.Exit(), 40);
            Stretch((RectTransform)done.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-220f, 24f), new Vector2(-20f, -24f));

            var view = NewRect(bar, "Viewport");
            Stretch(view, Vector2.zero, Vector2.one, new Vector2(16f, 10f), new Vector2(-240f, -10f));
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            view.gameObject.AddComponent<RectMask2D>();
            var list = NewRect(view, "List");
            list.anchorMin = Vector2.zero;
            list.anchorMax = new Vector2(0f, 1f);
            list.pivot = new Vector2(0f, 0.5f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = Vector2.zero;
            var h = list.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            list.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = list;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            foreach (var d in g.config.decorations)
            {
                var data = d;
                var card = Btn(list, "", RowColor, () => Game.I.Decor.StartPlacing(data), 26);
                var le = card.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 190f;
                var icon = Img(card.transform, d.sprite, Color.white, "Icon");
                icon.raycastTarget = false;
                Stretch(icon.rectTransform, new Vector2(0f, 0.36f), Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, -10f));
                var price = card.GetComponentInChildren<Text>();
                price.alignment = TextAnchor.LowerCenter;
                price.color = Ink;
                Stretch(price.rectTransform, Vector2.zero, new Vector2(1f, 0.36f), new Vector2(4f, 6f), new Vector2(-4f, 0f));
                decorCards.Add((card, price, d));
            }

            // Nút thao tác: Đặt/Huỷ khi đang đặt món mới, Gỡ/Bỏ chọn khi chọn món đã đặt.
            decorActions = NewRect(decorLayer, "Actions");
            // Đặt ở góc phải phía trên nút Xong để không che món đang kéo giữa màn hình.
            Place(decorActions, Vector2.right, Vector2.right, new Vector2(-20f, DecorBarHeight + 16f), new Vector2(600f, 96f));
            var hl = decorActions.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20f;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            decorPrimary = Btn(decorActions, "", Ok, OnDecorPrimary, 34);
            decorSecondary = Btn(decorActions, "", CloseColor, OnDecorSecondary, 34);
        }

        void OnDecorPrimary()
        {
            if (decorMode == null) return;
            if (decorMode.IsPlacing) decorMode.ConfirmPlacing();
            else if (decorMode.Selected != null) decorMode.RemoveSelected();
        }

        void OnDecorSecondary()
        {
            if (decorMode == null) return;
            if (decorMode.IsPlacing) decorMode.CancelPlacing();
            else decorMode.Deselect();
        }

        /// <summary>Cập nhật số món đã đặt, giá và nút thao tác theo trạng thái chế độ trang trí.</summary>
        public void RefreshDecorBar()
        {
            if (decorLayer == null || !decorLayer.gameObject.activeSelf || decorMode == null) return;
            var g = Game.I;
            var street = g.Street;
            decorInfo.text = "CHẾ ĐỘ TRANG TRÍ  ·  Đã đặt " + street.DecorationCount + "/" + street.DecorCapacity
                + "  ·  Chọn món bên dưới, kéo tới chỗ gạch trống rồi bấm Đặt. Kéo món đã đặt để dời chỗ.";

            foreach (var (card, price, data) in decorCards)
            {
                bool gem = data.gemCost > 0;
                price.text = (gem ? data.gemCost + " Ngọc" : NumberFormat.Short(data.coinCost) + " xu") + "\n<size=20>+" + data.attraction + " hấp dẫn</size>";
                bool picked = decorMode.Placing == data;
                card.image.color = picked ? Accent : street.CanAffordDecor(data) && street.DecorSlotsLeft > 0 ? RowColor : Off;
            }

            if (decorMode.IsPlacing)
            {
                var d = decorMode.Placing;
                decorActions.gameObject.SetActive(true);
                SetText(decorPrimary, "Đặt · " + (d.gemCost > 0 ? d.gemCost + " Ngọc" : NumberFormat.Short(d.coinCost) + " xu"));
                Look(decorPrimary, street.CanAffordDecor(d) && street.DecorSlotsLeft > 0, d.gemCost > 0 ? GemBlue : Ok);
                SetText(decorSecondary, "Huỷ");
                Look(decorSecondary, true, CloseColor);
            }
            else if (decorMode.Selected != null)
            {
                var d = decorMode.Selected.Data;
                float refund = g.config.decorRefund;
                decorActions.gameObject.SetActive(true);
                SetText(decorPrimary, "Gỡ · hoàn " + (d.gemCost > 0 ? Mathf.FloorToInt(d.gemCost * refund) + " Ngọc" : NumberFormat.Short(d.coinCost * refund) + " xu"));
                Look(decorPrimary, true, CloseColor);
                SetText(decorSecondary, "Bỏ chọn");
                Look(decorSecondary, true, Muted);
            }
            else
            {
                decorActions.gameObject.SetActive(false);
            }
        }
    }
}
