using UnityEngine;

namespace Tami
{
    /// <summary>Co giao diện vào vùng an toàn của màn hình (tránh tai thỏ, camera đục lỗ, thanh điều hướng).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform rect;
        Rect applied;
        Vector2Int screen;

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (applied != Screen.safeArea || screen.x != Screen.width || screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var area = Screen.safeArea;
            applied = area;
            screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
