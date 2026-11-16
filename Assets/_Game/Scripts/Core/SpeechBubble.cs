using UnityEngine;

namespace Tami
{
    /// <summary>
    /// Ô thoại trên đầu nhân vật: khung 9-slice dài theo câu nói, đuôi chĩa xuống người nói, chữ ở giữa.
    /// Các phần là con của nhân vật nên YSort của nhân vật sắp thứ tự vẽ cùng thân.
    /// </summary>
    public class SpeechBubble
    {
        const float Height = 0.62f;        // chiều cao khung (đơn vị cục bộ, trước khi nhân scale)
        const float CharWidth = 0.165f;    // bề ngang trung bình một ký tự với cỡ chữ CharacterSize
        const float Padding = 0.6f;
        const float CharacterSize = 0.046f;
        // Cạnh trên đuôi lấn vào trong khung bằng nét viền để che nét viền đáy khung chỗ nối.
        const float TailOverlap = 0.035f;

        readonly Transform root;
        readonly SpriteRenderer plate, tail;
        readonly TextMesh text;

        public bool Visible => plate.enabled;

        /// <param name="center">Tâm khung so với nhân vật.</param>
        /// <param name="scale">Thu nhỏ cả ô thoại (người chào hàng đứng sau quầy dùng ô nhỏ hơn).</param>
        /// <param name="order">Thứ tự vẽ của khung; đuôi và chữ vẽ đè lên trên.</param>
        public SpeechBubble(Transform parent, Vector3 center, float scale, int order, string name)
        {
            var g = Game.I;
            root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            root.localScale = Vector3.one * scale;

            plate = g.NewSprite(root, g.art.speechBubble != null ? g.art.speechBubble : g.art.white, order, "Plate");
            plate.drawMode = SpriteDrawMode.Sliced;
            if (g.art.speechTail != null)
            {
                tail = g.NewSprite(root, g.art.speechTail, order + 1, "Tail");
                tail.transform.localPosition = new Vector3(-0.12f, -Height * 0.5f + TailOverlap, 0f);
            }
            text = g.NewLabel(root, "", order + 2, CharacterSize, new Color(0.22f, 0.12f, 0.16f), "Text");
            Hide();
        }

        public void Show(string line)
        {
            float width = line.Length * CharWidth + Padding;
            text.text = line;
            plate.size = new Vector2(width, Height);
            // Khung đặt theo tâm dù pivot của hình ở đâu (hình trắng dự phòng có pivot ở cạnh trái).
            var sprite = plate.sprite;
            float pivotX = sprite != null ? sprite.pivot.x / sprite.rect.width : 0.5f;
            plate.transform.localPosition = new Vector3((pivotX - 0.5f) * width, 0f, 0f);
            plate.enabled = true;
            if (tail != null) tail.enabled = true;
        }

        public void Hide()
        {
            plate.enabled = false;
            if (tail != null) tail.enabled = false;
            text.text = "";
        }
    }
}
