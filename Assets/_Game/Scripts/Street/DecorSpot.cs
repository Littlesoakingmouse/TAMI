using System.Collections;
using UnityEngine;

namespace Tami
{
    /// <summary>Một chỗ đặt đồ trang trí (đặt sẵn trong scene, prefab DecorSpot). Vị trí là chân món đồ.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DecorSpot : MonoBehaviour
    {
        public int Index { get; private set; }
        public DecorationData Data { get; private set; }

        SpriteRenderer sr;
        Vector3 baseScale;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.sprite = null;
            baseScale = transform.localScale;
        }

        public void Init(int index) => Index = index;

        public void Place(DecorationData data)
        {
            Data = data;
            sr.sprite = data.sprite;
            if (isActiveAndEnabled && Game.I.Started) StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                transform.localScale = baseScale * (1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.25f);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 0f));
        }
    }
}
