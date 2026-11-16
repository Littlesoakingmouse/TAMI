using System.Collections;
using UnityEngine;

namespace Tami
{
    /// <summary>Một món trang trí người chơi đã đặt trên gạch. Vị trí là chân món đồ.</summary>
    public class PlacedDecor : MonoBehaviour
    {
        public DecorationData Data { get; private set; }
        public SpriteRenderer Renderer { get; private set; }

        YSort ysort;

        public static PlacedDecor Create(Transform parent, DecorationData data, Vector2 position)
        {
            var g = Game.I;
            var sr = g.NewSprite(parent, data.sprite, 0, "Decor_" + data.id);
            sr.transform.position = position;
            var placed = sr.gameObject.AddComponent<PlacedDecor>();
            placed.Data = data;
            placed.Renderer = sr;
            // Vùng chạm để chọn và kéo trong chế độ trang trí.
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            if (data.sprite != null)
            {
                var b = data.sprite.bounds;
                col.size = new Vector2(Mathf.Max(0.8f, b.size.x), Mathf.Max(0.8f, b.size.y));
                col.offset = b.center;
            }
            placed.ysort = sr.gameObject.AddComponent<YSort>();
            placed.ysort.isStatic = true;
            return placed;
        }

        public void MoveTo(Vector2 position)
        {
            transform.position = position;
            ysort.Apply();
        }

        public void SetHighlight(bool on) => Renderer.color = on ? new Color(1f, 0.92f, 0.6f) : Color.white;

        public void Pop() => StartCoroutine(PopRoutine());

        IEnumerator PopRoutine()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.25f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
