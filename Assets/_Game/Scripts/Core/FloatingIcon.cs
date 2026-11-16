using System.Collections.Generic;
using UnityEngine;

namespace Tami
{
    /// <summary>Icon (ví dụ món vừa mua) bật lên trên đầu khách, bay lên rồi mờ dần. Dùng lại object để không giật.</summary>
    public class FloatingIcon : MonoBehaviour
    {
        const float Life = 1.3f;
        const float Rise = 0.9f;
        static readonly Stack<FloatingIcon> Pool = new Stack<FloatingIcon>();

        SpriteRenderer sr;
        Vector3 start;
        float t, size;

        public static void Spawn(Sprite sprite, Vector3 position, float scale = 0.8f)
        {
            if (sprite == null) return;
            FloatingIcon icon = null;
            while (Pool.Count > 0 && icon == null) icon = Pool.Pop();
            if (icon == null)
            {
                var sr = Game.I.NewSprite(null, null, 29000, "FloatingIcon");
                icon = sr.gameObject.AddComponent<FloatingIcon>();
                icon.sr = sr;
            }
            icon.sr.sprite = sprite;
            icon.sr.color = Color.white;
            icon.start = position;
            icon.size = scale;
            icon.t = 0f;
            icon.transform.position = position;
            icon.transform.localScale = Vector3.zero;
            icon.gameObject.SetActive(true);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / Life;
            // Bật lên nhanh (hơi nảy), bay lên chậm dần, nửa sau mờ dần.
            float pop = t < 0.18f ? Mathf.Sin(t / 0.18f * Mathf.PI * 0.5f) * 1.15f : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.18f) / 0.15f));
            transform.localScale = Vector3.one * (size * pop);
            transform.position = start + Vector3.up * (Rise * (1f - (1f - k) * (1f - k)));
            sr.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((k - 0.5f) / 0.5f));
            if (t >= Life)
            {
                gameObject.SetActive(false);
                Pool.Push(this);
            }
        }
    }
}
