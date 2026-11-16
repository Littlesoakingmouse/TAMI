using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tami
{
    /// <summary>
    /// Camera bám theo nhân vật chính theo cả chiều ngang lẫn dọc; vuốt (hoặc lăn chuột) để xem quanh phố.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraScroller : MonoBehaviour
    {
        // Ngưỡng kéo theo mật độ điểm ảnh (khoảng 2 mm) để chạm trên điện thoại không bị nhầm thành kéo.
        static float DragThresholdPx => Screen.dpi > 0f ? Mathf.Max(14f, Screen.dpi * 0.08f) : 14f;

        /// <summary>True nếu lần nhấn hiện tại đã thành thao tác kéo; dùng để bỏ qua click.</summary>
        public static bool DraggedThisPress { get; private set; }
        /// <summary>Đang kéo vật khác (ví dụ đồ trang trí) nên camera không trượt theo ngón tay.</summary>
        public static bool DragBlocked;

        /// <summary>
        /// Joystick nổi trên màn hình cảm ứng: chạm rồi kéo thì joystick hiện ở chỗ bắt đầu chạm và nhân vật đi theo
        /// hướng kéo; chạm rồi nhả (chưa vượt ngưỡng kéo) thì không hiện, vẫn là chạm để đi tới hoặc tương tác.
        /// </summary>
        public static bool JoystickActive { get; private set; }
        /// <summary>Chỗ bắt đầu chạm (toạ độ màn hình).</summary>
        public static Vector2 JoystickOrigin { get; private set; }
        /// <summary>Hướng và độ mạnh 0..1 (đã bỏ vùng chết ở giữa).</summary>
        public static Vector2 JoystickVector { get; private set; }
        // Kéo khoảng 1 cm là đi hết tốc độ.
        static float JoystickRadiusPx => Screen.dpi > 0f ? Mathf.Max(60f, Screen.dpi * 0.4f) : 90f;
        const float JoystickDeadZone = 0.15f;

        /// <summary>Phạm vi thế giới camera được phép nhìn tới (đã tính phần bị thanh giao diện che).</summary>
        public float contentMinX;
        public float contentMaxX;
        public float contentMinY = -9f;
        public float contentMaxY = 9f;

        [Tooltip("Nhân vật camera bám theo. Vuốt để xem quanh phố; camera quay lại nhân vật khi nhân vật đi hoặc sau vài giây.")]
        public Transform follow;
        public float followSpeed = 5f;
        public float peekSeconds = 2.5f;
        [Tooltip("Camera nhìn cao hơn chân nhân vật bấy nhiêu đơn vị (để thấy cả dãy quầy khi đứng trên đường)")]
        public float followOffsetY = 1.2f;
        [Tooltip("Tạm dừng bám nhân vật (chế độ trang trí)")]
        public bool followPaused;

        public float MinCameraX => contentMinX + HalfWidth;
        public float MaxCameraX => Mathf.Max(MinCameraX, contentMaxX - HalfWidth);
        public float MinCameraY => contentMinY + HalfHeight;
        public float MaxCameraY => Mathf.Max(MinCameraY, contentMaxY - HalfHeight);

        static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        Camera cam;
        Vector2 pressPos, lastPos, velocity;
        bool pressing, dragging, startedOverUI, joystickPress;
        float lastManualTime = -100f;
        Vector3 lastFollowPos;

        float HalfWidth => Cam.orthographicSize * Cam.aspect;
        float HalfHeight => Cam.orthographicSize;
        Camera Cam => cam != null ? cam : cam = GetComponent<Camera>();

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer != null)
            {
                bool pressed = pointer.press.isPressed;
                Vector2 pos = pointer.position.ReadValue();
                if (pressed && !pressing)
                {
                    pressing = true;
                    dragging = false;
                    DraggedThisPress = false;
                    pressPos = lastPos = pos;
                    startedOverUI = IsOverUI(pos);
                    velocity = Vector2.zero;
                    joystickPress = !startedOverUI && IsTouchPress() && JoystickAllowed();
                }
                else if (pressed)
                {
                    if (!startedOverUI && !DragBlocked)
                    {
                        if (!dragging && (pos - pressPos).sqrMagnitude > DragThresholdPx * DragThresholdPx)
                        {
                            dragging = true;
                            DraggedThisPress = true;
                        }
                        if (dragging && joystickPress)
                        {
                            Vector2 v = Vector2.ClampMagnitude((pos - pressPos) / JoystickRadiusPx, 1f);
                            float m = v.magnitude;
                            JoystickVector = m <= JoystickDeadZone ? Vector2.zero : v * ((m - JoystickDeadZone) / (1f - JoystickDeadZone) / m);
                            JoystickOrigin = pressPos;
                            JoystickActive = JoystickAllowed();
                        }
                        else if (dragging)
                        {
                            Vector2 d = -(pos - lastPos) * WorldPerPixel();
                            Move(d);
                            velocity = d / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                            lastManualTime = Time.unscaledTime;
                        }
                    }
                    lastPos = pos;
                }
                else if (pressing)
                {
                    pressing = false;
                    dragging = false;
                    joystickPress = false;
                    JoystickActive = false;
                    JoystickVector = Vector2.zero;
                }
            }

            if (!pressing && velocity.sqrMagnitude > 0.0001f)
            {
                Move(velocity * Time.unscaledDeltaTime);
                velocity = Vector2.Lerp(velocity, Vector2.zero, 6f * Time.unscaledDeltaTime);
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(mouse.position.ReadValue()))
                {
                    Move(new Vector2(-Mathf.Sign(scroll) * 2f, 0f));
                    lastManualTime = Time.unscaledTime;
                }
            }
            Clamp();
        }

        // Bám theo sau khi nhân vật đã di chuyển trong khung này, tránh camera lệch nhịp làm hình bị rung.
        void LateUpdate()
        {
            if (follow != null && !followPaused)
            {
                bool followMoved = (follow.position - lastFollowPos).sqrMagnitude > 1e-6f;
                lastFollowPos = follow.position;
                // Kéo joystick là điều khiển nhân vật, không phải kéo camera: vẫn bám theo nhân vật.
                if ((!dragging || joystickPress) && (followMoved || Time.unscaledTime - lastManualTime > peekSeconds))
                {
                    velocity = Vector2.zero;
                    var p = transform.position;
                    float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
                    p.x = Mathf.Lerp(p.x, follow.position.x, k);
                    p.y = Mathf.Lerp(p.y, follow.position.y + followOffsetY, k);
                    transform.position = p;
                }
            }
            Clamp();
        }

        /// <summary>Đặt camera ngay tại vị trí bám nhân vật (khi vào game).</summary>
        public void SnapToFollow()
        {
            if (follow == null) return;
            var p = transform.position;
            p.x = follow.position.x;
            p.y = follow.position.y + followOffsetY;
            transform.position = p;
            Clamp();
        }

        static bool IsTouchPress() => Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;

        /// <summary>Joystick chỉ dùng khi đang ở ngoài phố: không mở bảng, hội thoại hay chế độ trang trí.</summary>
        static bool JoystickAllowed()
        {
            var g = Game.I;
            return g != null && g.Started && !g.UI.ModalOpen && !g.UI.DialogueOpen && !DecorMode.Active;
        }

        float WorldPerPixel() => Cam.orthographicSize * 2f / Screen.height;

        void Move(Vector2 d)
        {
            var p = transform.position;
            p.x += d.x;
            p.y += d.y;
            transform.position = p;
        }

        void Clamp()
        {
            var p = transform.position;
            float x = Mathf.Clamp(p.x, MinCameraX, MaxCameraX);
            float y = Mathf.Clamp(p.y, MinCameraY, MaxCameraY);
            if (!Mathf.Approximately(x, p.x)) velocity.x = 0f;
            if (!Mathf.Approximately(y, p.y)) velocity.y = 0f;
            p.x = x;
            p.y = y;
            transform.position = p;
        }

        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            Hits.Clear();
            es.RaycastAll(data, Hits);
            foreach (var h in Hits)
            {
                if (h.module is GraphicRaycaster) return true;
            }
            return false;
        }
    }
}
