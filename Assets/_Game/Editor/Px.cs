using UnityEngine;

namespace Tami.EditorTools
{
    /// <summary>
    /// Bảng vẽ đơn giản để sinh hình tạm: hình khối có khử răng cưa, gốc toạ độ ở góc dưới trái.
    /// Toạ độ tính theo điểm ảnh logic (64 = 1 đơn vị); ảnh thật được vẽ to gấp <see cref="Scale"/> lần cho nét.
    /// Khi <see cref="Outline"/> > 0, mỗi khối màu đặc được viền nét nâu đậm như style nhân vật chính.
    /// </summary>
    public class Px
    {
        public const float DefaultScale = 2f;
        public static readonly Color InkBrown = new Color(0.235f, 0.141f, 0.082f); // #3C2415

        public readonly int W, H;
        public readonly float Scale;
        public readonly int TexW, TexH;
        /// <summary>Độ dày viền (điểm ảnh logic). 0 là không viền.</summary>
        public float Outline;
        public Color OutlineColor = InkBrown;
        readonly Color[] pixels;

        public Px(int width, int height, float scale = DefaultScale)
        {
            W = width;
            H = height;
            Scale = scale;
            TexW = Mathf.RoundToInt(width * scale);
            TexH = Mathf.RoundToInt(height * scale);
            pixels = new Color[TexW * TexH];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);
        }

        public Texture2D ToTexture()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Tạm tắt viền cho các chi tiết nhỏ (mắt, má hồng, đường kẻ): using (p.Plain()) { ... }</summary>
        public System.IDisposable Plain() => new OutlineScope(this, 0f);

        /// <summary>Tạm đổi độ dày viền.</summary>
        public System.IDisposable WithOutline(float width) => new OutlineScope(this, width);

        sealed class OutlineScope : System.IDisposable
        {
            readonly Px p;
            readonly float saved;
            public OutlineScope(Px p, float width) { this.p = p; saved = p.Outline; p.Outline = width; }
            public void Dispose() => p.Outline = saved;
        }

        void Blend(int x, int y, Color c, float cover)
        {
            if (x < 0 || y < 0 || x >= TexW || y >= TexH || cover <= 0f) return;
            float a = c.a * Mathf.Clamp01(cover);
            int i = y * TexW + x;
            Color d = pixels[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 0f) return;
            Color o = (c * a + d * (d.a * (1f - a))) / outA;
            o.a = outA;
            pixels[i] = o;
        }

        /// <summary>Tô theo hàm khoảng cách có dấu (âm là bên trong), toạ độ logic. Nét viền nằm giữa mép hình.</summary>
        public void Fill(System.Func<float, float, float> sdf, Color c, float x0, float y0, float x1, float y1)
        {
            // Hình trong suốt (bóng, vầng sáng) không viền.
            float stroke = c.a >= 0.99f ? Outline * Scale : 0f;
            float half = stroke * 0.5f;
            float pad = half / Scale + 1f;
            int ix0 = Mathf.Max(0, Mathf.FloorToInt((x0 - pad) * Scale) - 1);
            int iy0 = Mathf.Max(0, Mathf.FloorToInt((y0 - pad) * Scale) - 1);
            int ix1 = Mathf.Min(TexW - 1, Mathf.CeilToInt((x1 + pad) * Scale) + 1);
            int iy1 = Mathf.Min(TexH - 1, Mathf.CeilToInt((y1 + pad) * Scale) + 1);
            for (int y = iy0; y <= iy1; y++)
            {
                for (int x = ix0; x <= ix1; x++)
                {
                    float d = sdf((x + 0.5f) / Scale, (y + 0.5f) / Scale) * Scale;
                    if (stroke > 0f) Blend(x, y, OutlineColor, Mathf.Clamp01(0.5f - (d - half)));
                    Blend(x, y, c, Mathf.Clamp01(0.5f - (d + half)));
                }
            }
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c) => RoundRect(x0, y0, x1, y1, 0f, c);

        public void RoundRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            r = Mathf.Min(r, Mathf.Min(hx, hy));
            Fill((x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - hx + r;
                float qy = Mathf.Abs(y - cy) - hy + r;
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            }, c, x0, y0, x1, y1);
        }

        public void Circle(float cx, float cy, float r, Color c)
        {
            Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, cx - r, cy - r, cx + r, cy + r);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            Fill((x, y) =>
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
            }, c, cx - rx, cy - ry, cx + rx, cy + ry);
        }

        public void Line(float ax, float ay, float bx, float by, float thickness, Color c)
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx, by) - a;
            float r = thickness * 0.5f;
            Fill((x, y) =>
            {
                var pa = new Vector2(x, y) - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
                return (pa - ba * h).magnitude - r;
            }, c, Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r);
        }

        /// <summary>Đa giác bất kỳ (khoảng cách tới cạnh gần nhất, âm khi ở trong).</summary>
        public void Poly(Color c, params Vector2[] pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            Fill((x, y) =>
            {
                var q = new Vector2(x, y);
                float best = float.MaxValue;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    var e = pts[i] - pts[j];
                    var w = q - pts[j];
                    float h = Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                    best = Mathf.Min(best, (w - e * h).sqrMagnitude);
                }
                float dist = Mathf.Sqrt(best);
                return Inside(pts, x, y) ? -dist : dist;
            }, c, minX, minY, maxX, maxY);
        }

        static bool Inside(Vector2[] p, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if ((p[i].y > y) != (p[j].y > y) &&
                    x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
