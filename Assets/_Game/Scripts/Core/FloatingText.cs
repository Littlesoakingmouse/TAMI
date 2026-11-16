using System.Collections.Generic;
using UnityEngine;

namespace Tami
{
    /// <summary>Chữ nổi lên rồi mờ dần, ví dụ "+1,2K". Dùng lại object để không giật.</summary>
    public class FloatingText : MonoBehaviour
    {
        const float Life = 1.1f;
        static readonly Stack<FloatingText> Pool = new Stack<FloatingText>();

        TextMesh text;
        Color baseColor;
        float t;

        public static void Spawn(Vector3 position, string message, Color color, float size = 0.075f)
        {
            FloatingText ft = null;
            while (Pool.Count > 0 && ft == null) ft = Pool.Pop();
            if (ft == null)
            {
                var tm = Game.I.NewLabel(null, message, 30000, size, color, "FloatingText");
                tm.fontStyle = FontStyle.Bold;
                ft = tm.gameObject.AddComponent<FloatingText>();
                ft.text = tm;
            }
            ft.text.text = message;
            ft.text.characterSize = size;
            ft.text.color = color;
            ft.baseColor = color;
            ft.t = 0f;
            ft.transform.position = position;
            ft.gameObject.SetActive(true);
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (1.2f * Time.deltaTime);
            // Chỉ đổi màu khi đang mờ dần: gán màu TextMesh làm dựng lại lưới chữ.
            if (t > Life * 0.5f)
            {
                var c = baseColor;
                c.a = 1f - Mathf.Clamp01((t - Life * 0.5f) / (Life * 0.5f));
                text.color = c;
            }
            if (t >= Life)
            {
                gameObject.SetActive(false);
                Pool.Push(this);
            }
        }
    }
}
