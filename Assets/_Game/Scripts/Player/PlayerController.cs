using UnityEngine;
using UnityEngine.InputSystem;

namespace Tami
{
    /// <summary>
    /// Nhân vật chính (chủ hội chợ). Chạm xuống đường để đi tới, chạm vào quầy/ô sạp/biển để đi tới rồi tương tác.
    /// Chỉ tương tác được khi đứng gần; đi ngang qua quầy có túi tiền sẽ tự thu tiền.
    /// Trên PC dùng A/D hoặc phím mũi tên để đi; trên điện thoại chạm rồi kéo để hiện joystick.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Tốc độ đi (đơn vị/giây)")]
        public float speed = 5.2f;
        [Tooltip("Khoảng cách theo trục x để tương tác với quầy, ô sạp, sân khấu")]
        public float interactRange = 1.6f;
        [Tooltip("Khoảng cách theo trục x để tự thu tiền khi đi ngang quầy")]
        public float collectRange = 1.4f;
        [Header("Hình nhân vật (ghép từ nhiều phần)")]
        [Tooltip("Object chứa các phần hình, lật trái/phải khi đổi hướng")]
        public Transform visual;
        public Transform head, body, armLeft, armRight, legLeft, legRight;
        [Tooltip("Góc vung tay chân khi đi (độ)")]
        public float swingAngle = 28f;
        [Tooltip("Số bước mỗi giây khi đi")]
        public float stepRate = 2.2f;

        Vector2 target;
        bool moving;
        IInteractable pending;
        float animTime, walkBlend;
        Vector3 headBase, bodyBase;

        public bool IsMoving { get; private set; }

        void Start()
        {
            target = transform.position;
            if (head != null) headBase = head.localPosition;
            if (body != null) bodyBase = body.localPosition;
        }

        /// <summary>Gần thì tương tác ngay, xa thì đi tới rồi tương tác.</summary>
        public void RequestInteract(IInteractable it)
        {
            if (it == null) return;
            if (InRange(it.InteractPoint))
            {
                pending = null;
                Face(it.InteractPoint.x);
                it.Interact();
                return;
            }
            pending = it;
            SetTarget(it.InteractPoint);
        }

        /// <summary>Đi tới một điểm trên đường (huỷ tương tác đang chờ).</summary>
        public void MoveTo(Vector2 worldPoint)
        {
            pending = null;
            SetTarget(worldPoint);
        }

        /// <summary>Đủ gần để tương tác: lệch ngang trong interactRange và lệch dọc không quá 1,6 đơn vị.</summary>
        public bool InRange(Vector3 point) =>
            Mathf.Abs(point.x - transform.position.x) <= interactRange && Mathf.Abs(point.y - transform.position.y) <= 1.6f;

        void SetTarget(Vector2 p)
        {
            target = ClampToRoad(p);
            moving = true;
        }

        // Đi tự do trên cỏ, gạch và đường trong phần phố đã mở.
        static Vector2 ClampToRoad(Vector2 p) => Game.I.Street.ClampToWalk(p);

        void Update()
        {
            var g = Game.I;
            if (g == null || !g.Started) return;
            float dt = Time.deltaTime;
            Vector2 pos = transform.position;
            Vector2 before = pos;

            // Bàn phím đi hết tốc độ; joystick trên điện thoại đi nhanh chậm theo độ kéo.
            Vector2 input = KeyboardInput();
            if (input != Vector2.zero) input = input.normalized;
            else if (CameraScroller.JoystickActive) input = CameraScroller.JoystickVector;
            if (input != Vector2.zero && !g.UI.ModalOpen && !g.UI.DialogueOpen && !DecorMode.Active)
            {
                pending = null;
                moving = false;
                pos = ClampToRoad(pos + input * (speed * dt));
                transform.position = pos;
            }
            else if (moving)
            {
                pos = Vector2.MoveTowards(pos, target, speed * dt);
                transform.position = pos;
                if ((pos - target).sqrMagnitude < 0.0004f)
                {
                    moving = false;
                    // Vật đã bị huỷ (ví dụ ô sạp vừa dựng quầy) thì bỏ qua.
                    if (pending is Object o && o == null) pending = null;
                    if (pending != null && InRange(pending.InteractPoint))
                    {
                        var it = pending;
                        pending = null;
                        Face(it.InteractPoint.x);
                        it.Interact();
                    }
                }
            }

            IsMoving = (pos - before).sqrMagnitude > 1e-7f;
            if (IsMoving) Face(pos.x + (pos.x - before.x));
            AutoCollect(pos);

            Animate(dt);
        }

        /// <summary>Đi: tay chân vung ngược nhau, người nhún theo bước. Đứng yên: đầu nhún nhẹ như đang thở.</summary>
        void Animate(float dt)
        {
            animTime += dt;
            walkBlend = Mathf.MoveTowards(walkBlend, IsMoving ? 1f : 0f, dt * 8f);
            float phase = animTime * stepRate * Mathf.PI * 2f;
            float swing = Mathf.Sin(phase) * swingAngle * walkBlend;
            float step = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * walkBlend;
            float breathe = Mathf.Sin(animTime * 2.4f) * 0.012f * (1f - walkBlend);

            Rotate(legLeft, swing);
            Rotate(legRight, -swing);
            Rotate(armLeft, -swing * 0.8f);
            Rotate(armRight, swing * 0.8f);
            if (body != null) body.localPosition = bodyBase + new Vector3(0f, step, 0f);
            if (head != null)
            {
                head.localPosition = headBase + new Vector3(0f, step * 1.2f + breathe, 0f);
                head.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * 3f * walkBlend);
            }
        }

        static void Rotate(Transform t, float angle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        static Vector2 KeyboardInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float h = 0f, v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            return new Vector2(h, v);
        }

        /// <summary>Đi ngang qua quầy có tiền thì tự thu (GDD mục 4.3).</summary>
        void AutoCollect(Vector2 p)
        {
            var street = Game.I.Street;
            // Chỉ thu khi đi ngang phía trước quầy (trên đường hoặc sát mép gạch trên).
            if (Mathf.Abs(p.y - street.StandY) > 1.6f) return;
            foreach (var stall in street.Stalls)
            {
                if (stall.Vault > 0 && Mathf.Abs(stall.Slot.X - p.x) <= collectRange) stall.Collect();
            }
        }

        void Face(float x)
        {
            if (visual == null) return;
            float dx = x - transform.position.x;
            // Hình gốc nhìn sang phải.
            if (Mathf.Abs(dx) > 0.01f) visual.localScale = new Vector3(dx < 0f ? -1f : 1f, 1f, 1f);
        }
    }
}
