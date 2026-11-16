using UnityEngine;
using UnityEngine.InputSystem;

namespace Tami
{
    /// <summary>
    /// Chế độ trang trí: chọn món ở thanh dưới, kéo bản xem trước tới chỗ muốn đặt trên gạch rồi bấm Đặt để mua.
    /// Kéo món đã đặt để dời chỗ; chạm chọn món đã đặt để gỡ (hoàn một phần tiền).
    /// Trong chế độ này nhân vật đứng yên, vuốt chỗ trống để xem quanh map.
    /// </summary>
    public class DecorMode : MonoBehaviour
    {
        public static bool Active { get; private set; }

        static readonly Color Valid = new Color(1f, 1f, 1f, 0.75f);
        static readonly Color Invalid = new Color(1f, 0.35f, 0.35f, 0.65f);

        DecorationData placing;
        SpriteRenderer ghost;
        PlacedDecor selected;
        Vector2 dragOffset, selectedOrigin;
        bool pressing, draggingGhost, draggingSelected;

        Game G => Game.I;
        StreetManager Street => G.Street;
        CameraScroller Scroller => G.Cam.GetComponent<CameraScroller>();

        public bool IsPlacing => placing != null;
        public PlacedDecor Selected => selected;
        public DecorationData Placing => placing;

        public void Enter()
        {
            if (Active) return;
            G.UI.CloseModal();
            Active = true;
            Scroller.followPaused = true;
            G.UI.ShowDecorBar(this);
        }

        public void Exit()
        {
            if (!Active) return;
            CancelPlacing();
            Deselect();
            Active = false;
            CameraScroller.DragBlocked = false;
            Scroller.followPaused = false;
            G.UI.HideDecorBar();
        }

        /// <summary>Bắt đầu đặt món mới: hiện bản xem trước ở chỗ trống gần giữa màn hình.</summary>
        public void StartPlacing(DecorationData d)
        {
            Deselect();
            placing = d;
            if (ghost == null)
            {
                ghost = G.NewSprite(transform, null, 32000, "DecorGhost");
            }
            ghost.sprite = d.sprite;
            ghost.gameObject.SetActive(true);
            ghost.transform.position = FindFreeSpot(G.Cam.transform.position);
            G.UI.RefreshDecorBar();
        }

        public void CancelPlacing()
        {
            placing = null;
            if (ghost != null) ghost.gameObject.SetActive(false);
            G.UI.RefreshDecorBar();
        }

        /// <summary>Mua và đặt món đang xem trước tại chỗ hiện tại.</summary>
        public void ConfirmPlacing()
        {
            if (placing == null) return;
            var d = placing;
            Vector2 at = ghost.transform.position;
            if (Street.DecorSlotsLeft <= 0) { G.UI.Toast("Đã đủ " + Street.DecorCapacity + " món. Mở rộng phố để đặt thêm."); return; }
            if (!Street.CanPlaceDecor(at)) { G.UI.Toast("Chỗ này không đặt được. Kéo món tới vùng gạch còn trống."); G.Audio.Play(Sfx.Error); return; }
            if (!Street.CanAffordDecor(d)) { G.UI.Toast(d.gemCost > 0 ? "Chưa đủ Ngọc" : "Chưa đủ xu"); G.Audio.Play(Sfx.Error); return; }
            if (Street.BuyDecor(d, at) != null)
            {
                G.UI.Toast("Đã đặt " + d.displayName + " (+" + d.attraction + " độ hấp dẫn)");
                CancelPlacing();
            }
        }

        public void RemoveSelected()
        {
            if (selected == null) return;
            var d = selected.Data;
            var target = selected;
            Deselect();
            Street.RemoveDecor(target, G.config.decorRefund);
            G.UI.Toast("Đã gỡ " + d.displayName);
            G.UI.RefreshDecorBar();
        }

        public void Deselect()
        {
            if (selected != null) selected.SetHighlight(false);
            selected = null;
            if (G != null && G.UI != null) G.UI.RefreshDecorBar();
        }

        void Select(PlacedDecor d)
        {
            if (selected != null) selected.SetHighlight(false);
            selected = d;
            selected.SetHighlight(true);
            G.UI.RefreshDecorBar();
        }

        /// <summary>Tìm chỗ đặt được gần một điểm: ưu tiên gạch dưới đường, rồi tới gạch phía trên.</summary>
        Vector2 FindFreeSpot(Vector2 near)
        {
            float[] rows = { Street.roadBottomY - 1.3f, Street.roadBottomY - 3f, Street.roadTopY + 0.3f };
            for (int step = 0; step < 40; step++)
            {
                float dx = (step % 2 == 0 ? 1 : -1) * (step / 2) * 0.6f;
                foreach (float y in rows)
                {
                    var p = new Vector2(near.x + dx, y);
                    if (Street.CanPlaceDecor(p)) return p;
                }
            }
            return new Vector2(near.x, rows[0]);
        }

        void Update()
        {
            if (!Active) return;
            if (ghost != null && ghost.gameObject.activeSelf)
            {
                ghost.color = Street.CanPlaceDecor(ghost.transform.position) ? Valid : Invalid;
            }

            var pointer = Pointer.current;
            if (pointer == null) return;
            bool pressed = pointer.press.isPressed;
            Vector2 screen = pointer.position.ReadValue();
            Vector2 world = G.Cam.ScreenToWorldPoint(screen);

            if (pressed && !pressing)
            {
                pressing = true;
                if (CameraScroller.IsOverUI(screen)) return;
                if (placing != null && GhostContains(world))
                {
                    draggingGhost = true;
                    dragOffset = world - (Vector2)ghost.transform.position;
                }
                else
                {
                    var hit = DecorAt(world);
                    if (hit != null)
                    {
                        if (placing != null) CancelPlacing();
                        Select(hit);
                        draggingSelected = true;
                        selectedOrigin = hit.transform.position;
                        dragOffset = world - selectedOrigin;
                    }
                }
                CameraScroller.DragBlocked = draggingGhost || draggingSelected;
            }
            else if (pressed)
            {
                if (draggingGhost) ghost.transform.position = world - dragOffset;
                if (draggingSelected)
                {
                    Vector2 p = world - dragOffset;
                    selected.MoveTo(p);
                    selected.Renderer.color = Street.CanPlaceDecor(p, selected) ? new Color(1f, 0.92f, 0.6f) : Invalid;
                }
            }
            else if (pressing)
            {
                pressing = false;
                if (draggingSelected)
                {
                    // Chỗ mới không hợp lệ thì trả món về chỗ cũ.
                    if (!Street.CanPlaceDecor(selected.transform.position, selected)) selected.MoveTo(selectedOrigin);
                    else if ((Vector2)selected.transform.position != selectedOrigin) Street.NotifyChanged();
                    selected.SetHighlight(true);
                }
                else if (!draggingGhost && !CameraScroller.DraggedThisPress && !CameraScroller.IsOverUI(screen))
                {
                    // Chạm chỗ trống: dời bản xem trước tới đó, hoặc bỏ chọn món đang chọn.
                    if (placing != null) ghost.transform.position = world;
                    else Deselect();
                }
                draggingGhost = draggingSelected = false;
                CameraScroller.DragBlocked = false;
            }
        }

        void OnDestroy()
        {
            Active = false;
            CameraScroller.DragBlocked = false;
        }

        bool GhostContains(Vector2 world)
        {
            if (ghost == null || ghost.sprite == null) return false;
            var b = ghost.bounds;
            b.Expand(0.4f);
            return b.Contains(new Vector3(world.x, world.y, b.center.z));
        }

        static PlacedDecor DecorAt(Vector2 world)
        {
            foreach (var hit in Physics2D.OverlapPointAll(world))
            {
                if (hit.TryGetComponent<PlacedDecor>(out var d)) return d;
            }
            return null;
        }
    }
}
