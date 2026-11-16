using UnityEngine;

namespace Tami.EditorTools
{
    /// <summary>
    /// Vẽ hình tạm theo style nhân vật chính: màu phẳng, viền nâu đậm, nhân vật chibi đầu to, mắt chấm, má hồng.
    /// Toạ độ tính bằng điểm ảnh logic, 64 điểm = 1 đơn vị.
    /// </summary>
    public static partial class PlaceholderArt
    {
        static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Color Lighten(Color c, float k) => Color.Lerp(c, Color.white, k);
        static Color Darken(Color c, float k) => Color.Lerp(c, Color.black, k);

        /// <summary>Bảng vẽ mới có viền nâu đậm (độ dày theo điểm ảnh logic).</summary>
        static Px N(int w, int h, float outline = 2f) => new Px(w, h) { Outline = outline };

        // Bảng màu lấy từ hình nhân vật chính.
        static readonly Color Ink = Px.InkBrown;
        static readonly Color Shadow = new Color(0.24f, 0.14f, 0.08f, 0.22f);
        static readonly Color Skin = C("#EFC383");
        static readonly Color Blush = C("#E59789");
        static readonly Color Red = C("#C93F32");
        static readonly Color Orange = C("#DB8A28");
        static readonly Color Black = C("#1C1517");
        static readonly Color Cream = C("#F6E7C8");
        static readonly Color Gold = C("#F2B83B");
        static readonly Color GoldDark = C("#C98A1E");
        static readonly Color Wood = C("#C08048");
        static readonly Color WoodDark = C("#8A5532");
        static readonly Color Teal = C("#4E9C8F");
        static readonly Color Blue = C("#4A78B8");
        static readonly Color Navy = C("#34405E");
        static readonly Color Pink = C("#EBA3AE");
        static readonly Color Green = C("#8DB25A");
        static readonly Color Paper = C("#FFF8EA");

        // ---------- Nền ----------

        public static Px Road()
        {
            // Nền lát đá: nét mảnh và nhạt hơn để nhân vật nổi bật.
            var p = new Px(64, 64) { Outline = 1.2f, OutlineColor = C("#6B5B50") };
            p.Rect(0, 0, 64, 64, C("#857467"));
            p.RoundRect(2, 2, 31, 31, 5, C("#B3A293"));
            p.RoundRect(33, 2, 62, 31, 5, C("#AA998A"));
            p.RoundRect(2, 33, 31, 62, 5, C("#A89786"));
            p.RoundRect(33, 33, 62, 62, 5, C("#B8A898"));
            return p;
        }

        public static Px Ground()
        {
            // Tường gạch ấm phía sau dãy quầy.
            var p = new Px(64, 64) { Outline = 1.2f, OutlineColor = C("#7E4A33") };
            p.Rect(0, 0, 64, 64, C("#9A5E42"));
            for (int row = 0; row < 4; row++)
            {
                float offset = row % 2 == 0 ? 0f : 16f;
                for (int i = -1; i < 3; i++)
                {
                    float x = i * 32 + offset;
                    var col = (row + i) % 2 == 0 ? C("#C98A62") : C("#C08159");
                    p.RoundRect(x + 1.5f, row * 16 + 1.5f, x + 30.5f, row * 16 + 14.5f, 4, col);
                }
            }
            return p;
        }

        /// <summary>Ô cỏ lặp lại: nền xanh, vài khóm cỏ và hoa nhỏ (cây sẽ đặt riêng sau).</summary>
        public static Px Grass()
        {
            var p = new Px(64, 64);
            p.Rect(0, 0, 64, 64, C("#8CC05A"));
            using (p.Plain())
            {
                Color tuft = C("#74A846"), light = C("#A3D16E");
                foreach (var (x, y) in new[] { (8f, 10f), (40f, 6f), (24f, 30f), (54f, 34f), (10f, 50f), (38f, 52f) })
                {
                    p.Poly(tuft, V(x - 4, y), V(x - 1, y + 7), V(x, y), V(x + 2, y + 8), V(x + 4, y));
                }
                p.Circle(30f, 16f, 3f, light);
                p.Circle(52f, 18f, 2.5f, light);
                p.Circle(18f, 40f, 2.5f, light);
                p.Circle(48f, 48f, 2f, Paper);
                p.Circle(14f, 22f, 2f, C("#F7D04A"));
            }
            return p;
        }

        /// <summary>Ô trời lặp lại: xanh nhạt với mây trắng phẳng.</summary>
        public static Px Sky()
        {
            // Ô to (8 x 4 đơn vị) và chỉ 2 đám mây để không thấy lặp dày.
            var p = new Px(512, 256, 1f);
            p.Rect(0, 0, 512, 256, C("#BFE6F2"));
            using (p.Plain())
            {
                Color cloud = new Color(1f, 1f, 1f, 0.92f);
                p.Ellipse(110f, 170f, 60f, 22f, cloud);
                p.Circle(90f, 182f, 28f, cloud);
                p.Circle(128f, 190f, 34f, cloud);
                p.Ellipse(380f, 70f, 46f, 16f, cloud);
                p.Circle(366f, 80f, 20f, cloud);
                p.Circle(396f, 84f, 24f, cloud);
            }
            return p;
        }

        public static Px SlotEmpty()
        {
            var p = N(192, 192, 3f);
            var white = new Color(1f, 1f, 1f, 0.9f);
            using (p.Plain())
            {
                p.RoundRect(14, 14, 178, 178, 22, new Color(1f, 1f, 1f, 0.14f));
                float a = 14, b = 178, len = 34, th = 6;
                p.Line(a, a, a + len, a, th, white); p.Line(a, a, a, a + len, th, white);
                p.Line(b, a, b - len, a, th, white); p.Line(b, a, b, a + len, th, white);
                p.Line(a, b, a + len, b, th, white); p.Line(a, b, a, b - len, th, white);
                p.Line(b, b, b - len, b, th, white); p.Line(b, b, b, b - len, th, white);
            }
            // Dấu + có viền như nút bấm.
            p.Circle(96, 96, 36, Gold);
            float a0 = 9, a1 = 25;
            p.Poly(Paper, V(96 - a0, 96 + a1), V(96 + a0, 96 + a1), V(96 + a0, 96 + a0), V(96 + a1, 96 + a0),
                V(96 + a1, 96 - a0), V(96 + a0, 96 - a0), V(96 + a0, 96 - a1), V(96 - a0, 96 - a1),
                V(96 - a0, 96 - a0), V(96 - a1, 96 - a0), V(96 - a1, 96 + a0), V(96 - a0, 96 + a0));
            return p;
        }

        public static Px Gate()
        {
            var p = N(320, 160, 2.5f);
            using (p.Plain()) p.Ellipse(160, 8, 150, 8, Shadow);
            foreach (float x in new[] { 18f, 262f })
            {
                p.RoundRect(x - 6, 0, x + 46, 16, 5, C("#8D8078"));
                p.RoundRect(x, 12, x + 40, 132, 6, Red);
            }
            // Biển tên
            p.RoundRect(40, 98, 280, 136, 10, Cream);
            // Mái: đỏ kiểu Việt, đầu đao cong kiểu Hàn
            p.Poly(C("#3B3F48"), V(0, 140), V(16, 128), V(304, 128), V(320, 140), V(296, 158), V(24, 158));
            p.Rect(18, 128, 302, 136, Gold);
            p.Circle(10, 142, 7, Gold);
            p.Circle(310, 142, 7, Gold);
            Lantern(p, 82, 82, Red);
            Lantern(p, 238, 82, Blue);
            return p;
        }

        static void Lantern(Px p, float x, float y, Color c)
        {
            using (p.Plain()) p.Line(x, y + 12, x, y + 22, 2.5f, Ink);
            p.RoundRect(x - 6, y + 10, x + 6, y + 16, 2, Gold);
            p.RoundRect(x - 6, y - 16, x + 6, y - 10, 2, Gold);
            p.Ellipse(x, y, 11, 13, c);
            using (p.Plain()) p.Ellipse(x - 4, y + 3, 2.5f, 4.5f, new Color(1f, 1f, 1f, 0.45f));
        }

        public static Px ExpandSign()
        {
            var p = N(192, 128, 3f);
            using (p.Plain()) p.Ellipse(96, 5, 34, 5, Shadow);
            p.RoundRect(70, 0, 84, 50, 3, WoodDark);
            p.RoundRect(108, 0, 122, 50, 3, WoodDark);
            p.RoundRect(6, 40, 186, 124, 14, Gold);
            using (p.Plain()) p.RoundRect(16, 50, 176, 114, 10, C("#F7D27A"));
            return p;
        }

        // ---------- Quầy hàng ----------

        public static Px Stall(Origin origin, int level)
        {
            var p = N(192, 192, 2.5f);
            bool vn = origin == Origin.VN;
            Color main = vn ? Red : Blue;
            Color accent = vn ? Gold : Pink;
            Color wood = level == 1 ? C("#D59A5E") : level == 2 ? Wood : C("#A8643A");

            using (p.Plain()) p.Ellipse(96, 10, 88, 9, Shadow);

            // Cột
            p.RoundRect(22, 60, 36, 156, 4, WoodDark);
            p.RoundRect(156, 60, 170, 156, 4, WoodDark);

            // Thân quầy
            p.RoundRect(14, 12, 178, 90, 12, wood);
            if (level == 1)
            {
                for (int i = 0; i < 7; i++) p.RoundRect(26 + i * 21, 24, 38 + i * 21, 76, 4, i % 2 == 0 ? main : Cream);
            }
            else
            {
                p.RoundRect(32, 24, 160, 74, 10, main);
                p.RoundRect(44, 34, 148, 64, 7, accent);
                if (level == 3)
                {
                    p.Circle(96, 49, 12, main);
                    using (p.Plain()) p.Circle(96, 49, 5, accent);
                }
            }

            // Mặt quầy và hàng hoá
            p.RoundRect(6, 86, 186, 102, 7, Lighten(wood, 0.25f));
            Bowl(p, 56, 108, 20, accent);
            Bowl(p, 98, 110, 22, vn ? Green : C("#E4572E"));
            Bowl(p, 140, 108, 20, C("#F7B267"));

            if (level == 1)
            {
                // Mái bạt sọc, mép lượn sóng
                p.Poly(main, V(4, 140), V(188, 140), V(176, 180), V(16, 180));
                using (p.Plain())
                {
                    for (int i = 0; i < 9; i += 2)
                    {
                        float x0 = 8 + i * 20;
                        p.Poly(Cream, V(x0 + 2, 142), V(x0 + 20, 142), V(x0 + 19, 177), V(x0 + 4, 177));
                    }
                }
                for (int i = 0; i < 9; i++) p.Circle(16 + i * 20, 140, 10, i % 2 == 0 ? main : Cream);
            }
            else if (level == 2)
            {
                // Mái gỗ, biển hiệu kem
                p.Poly(C("#9A5D34"), V(0, 132), V(192, 132), V(172, 184), V(20, 184));
                using (p.Plain()) for (int i = 0; i < 6; i++) p.Line(42 + i * 22, 142, 46 + i * 22, 176, 3, C("#7E4A28"));
                p.RoundRect(0, 124, 192, 138, 5, main);
                p.RoundRect(60, 150, 132, 178, 7, Cream);
                using (p.Plain()) p.RoundRect(72, 161, 120, 167, 3, main);
                Lantern(p, 34, 112, vn ? Red : Blue);
                Lantern(p, 158, 112, vn ? Red : Blue);
            }
            else if (vn)
            {
                // Mái ngói đỏ, đầu đao vàng
                p.Poly(C("#A8382C"), V(0, 146), V(14, 134), V(178, 134), V(192, 146), V(176, 188), V(16, 188));
                using (p.Plain()) for (int i = 0; i < 7; i++) p.Line(34 + i * 21, 142, 38 + i * 21, 182, 3, C("#842A20"));
                p.RoundRect(16, 180, 176, 190, 4, Gold);
                p.Circle(10, 148, 8, Gold);
                p.Circle(182, 148, 8, Gold);
                p.RoundRect(0, 124, 192, 136, 5, Gold);
                Lantern(p, 34, 108, Red);
                Lantern(p, 158, 108, Red);
            }
            else
            {
                // Mái hanok cong, dải hoa văn dancheong
                p.Poly(C("#4A5560"), V(0, 160), V(8, 140), V(184, 140), V(192, 160), V(174, 188), V(18, 188));
                using (p.Plain()) for (int i = 0; i < 9; i++) p.Line(32 + i * 16, 148, 34 + i * 16, 182, 2.5f, C("#38414A"));
                p.RoundRect(0, 124, 192, 140, 5, Teal);
                using (p.Plain())
                {
                    Color[] dancheong = { Red, Gold, Paper };
                    for (int i = 0; i < 12; i++) p.Circle(14 + i * 15, 132, 3.5f, dancheong[i % dancheong.Length]);
                }
                Lantern(p, 34, 108, Blue);
                Lantern(p, 158, 108, Red);
            }
            return p;
        }

        static void Bowl(Px p, float x, float y, float r, Color food)
        {
            p.Ellipse(x, y, r, r * 0.48f, Paper);
            using (p.Plain()) p.Ellipse(x, y + 2, r * 0.72f, r * 0.26f, food);
        }

        // ---------- Khách (chibi đầu to như nhân vật chính) ----------

        public static Px Npc(int variant)
        {
            var p = N(64, 96, 2f);
            Color[] skins = { Skin, C("#E3AE73"), C("#F6D3A5") };
            Color skin = skins[variant % skins.Length];
            Color hair = variant % 2 == 0 ? C("#3B2A22") : C("#6E4328");
            Color shoes = C("#3B2A22");
            bool conical = false, bun = false, longHair = false, gat = false, chef = false;

            using (p.Plain()) p.Ellipse(32, 4, 17, 3.5f, Shadow);

            switch (variant)
            {
                case 0: // Áo phông xanh ngọc, quần dài
                    Legs(p, Navy, shoes);
                    Torso(p, Teal, skin);
                    break;
                case 1: // Áo dài đỏ, nón lá
                    Legs(p, Paper, Paper);
                    p.RoundRect(20, 8, 44, 42, 9, Red);
                    using (p.Plain()) p.Line(32, 12, 32, 34, 1.6f, Darken(Red, 0.3f));
                    Arms(p, Red, skin);
                    conical = true;
                    longHair = true;
                    hair = C("#241B1B");
                    break;
                case 2: // Hanbok nữ: áo vàng, váy đỏ
                    p.Poly(Red, V(12, 3), V(52, 3), V(44, 36), V(20, 36));
                    p.RoundRect(20, 28, 44, 42, 7, Gold);
                    using (p.Plain()) p.Poly(Red, V(32, 34), V(38, 26), V(34, 26));
                    Arms(p, Gold, skin);
                    bun = true;
                    hair = C("#241B1B");
                    break;
                case 3: // Áo vàng, quần jean, tóc dài nâu
                    Legs(p, Blue, shoes);
                    Torso(p, Gold, skin);
                    longHair = true;
                    break;
                case 4: // Hanbok nam, mũ gat
                    Legs(p, C("#E9E0CC"), shoes);
                    Torso(p, Paper, skin);
                    using (p.Plain()) p.RoundRect(29, 16, 35, 38, 2, Teal);
                    gat = true;
                    hair = C("#241B1B");
                    break;
                case 6: // Quản lý quầy: áo trắng, tạp dề cam, mũ đầu bếp
                    Legs(p, Navy, shoes);
                    Torso(p, Paper, skin);
                    p.RoundRect(23, 12, 41, 34, 4, Orange);
                    chef = true;
                    break;
                default: // Nông dân, nón lá
                    Legs(p, C("#6E4328"), shoes);
                    Torso(p, C("#B58A5A"), skin);
                    conical = true;
                    break;
            }

            Head(p, 32, 40, skin, hair, longHair);
            if (bun) p.Circle(32, 88, 7, hair);
            if (conical)
            {
                p.Poly(C("#E9D3A0"), V(2, 70), V(62, 70), V(32, 94));
                using (p.Plain()) p.Line(10, 72, 54, 72, 1.5f, C("#C7A970"));
            }
            if (gat)
            {
                p.RoundRect(23, 74, 41, 90, 4, Black);
                p.Ellipse(32, 74, 27, 4.5f, Black);
            }
            if (chef)
            {
                p.Circle(24, 84, 8, Paper);
                p.Circle(40, 84, 8, Paper);
                p.Circle(32, 88, 9, Paper);
                p.RoundRect(20, 72, 44, 82, 4, Paper);
            }
            return p;
        }

        /// <summary>Đầu chibi: tóc trùm như mũ bảo hiểm, mặt vuông bo tròn, mắt chấm, mũi gạch, má hồng.</summary>
        static void Head(Px p, float cx, float bottom, Color skin, Color hair, bool longHair)
        {
            if (longHair) p.RoundRect(cx - 24, bottom - 6, cx + 24, bottom + 30, 12, hair);
            p.Ellipse(cx, bottom + 24, 27, 24, hair);
            p.RoundRect(cx - 15, bottom + 1, cx + 17, bottom + 32, 10, skin);
            // Mái tóc phủ trán
            p.Poly(hair, V(cx - 20, bottom + 28), V(cx + 22, bottom + 28), V(cx + 20, bottom + 40), V(cx - 18, bottom + 42));
            using (p.Plain())
            {
                p.Ellipse(cx - 9, bottom + 9, 4.5f, 3.6f, Blush);
                p.Ellipse(cx + 12, bottom + 9, 4.5f, 3.6f, Blush);
                p.Ellipse(cx - 6, bottom + 15, 1.7f, 2.4f, Ink);
                p.Ellipse(cx + 9, bottom + 15, 1.7f, 2.4f, Ink);
                p.Ellipse(cx + 1.5f, bottom + 14, 2.2f, 0.7f, Ink);
            }
        }

        static void Legs(Px p, Color pants, Color shoes)
        {
            p.RoundRect(22, 3, 31, 20, 4, pants);
            p.RoundRect(33, 3, 42, 20, 4, pants);
        }

        static void Torso(Px p, Color shirt, Color skin)
        {
            p.RoundRect(19, 14, 45, 42, 7, shirt);
            Arms(p, shirt, skin);
        }

        static void Arms(Px p, Color sleeve, Color skin)
        {
            p.Circle(15, 20, 4, skin);
            p.Circle(49, 20, 4, skin);
            p.RoundRect(11, 22, 21, 38, 5, sleeve);
            p.RoundRect(43, 22, 53, 38, 5, sleeve);
        }

        // ---------- Biểu tượng ----------

        public static Px Coin()
        {
            var p = N(64, 64, 3f);
            p.Circle(32, 32, 28, Gold);
            using (p.WithOutline(2f)) p.Circle(32, 32, 18, C("#F7CF5E"));
            p.RoundRect(26, 26, 38, 38, 2, GoldDark);
            using (p.Plain()) p.Ellipse(20, 44, 4, 5.5f, new Color(1f, 1f, 1f, 0.7f));
            return p;
        }

        public static Px MoneyBag()
        {
            var p = N(64, 64, 2.5f);
            using (p.Plain()) p.Ellipse(32, 4, 20, 4, Shadow);
            p.Poly(C("#D9A562"), V(16, 61), V(26, 46), V(38, 46), V(48, 61), V(32, 56));
            p.Circle(32, 26, 22, C("#D9A562"));
            p.RoundRect(21, 43, 43, 50, 3, Red);
            p.Circle(32, 24, 10, Gold);
            using (p.Plain()) p.RoundRect(30.5f, 18, 33.5f, 30, 1.5f, GoldDark);
            using (p.Plain()) p.Ellipse(21, 32, 3, 4, new Color(1f, 1f, 1f, 0.5f));
            return p;
        }

        static void BubbleBase(Px p)
        {
            p.Poly(Paper, V(12, 16), V(24, 14), V(8, 2));
            p.RoundRect(3, 11, 45, 46, 13, Paper);
            using (p.Plain()) p.Poly(Paper, V(13, 15), V(23, 15), V(14, 10));
        }

        public static Px BubbleSad()
        {
            var p = N(48, 48, 2f);
            BubbleBase(p);
            using (p.Plain())
            {
                p.Circle(24, 29, 11, C("#8FB6E0"));
                p.Ellipse(20, 31, 1.6f, 2.2f, Ink);
                p.Ellipse(28, 31, 1.6f, 2.2f, Ink);
                p.Line(20, 23.5f, 24, 25.5f, 1.8f, Ink);
                p.Line(24, 25.5f, 28, 23.5f, 1.8f, Ink);
            }
            return p;
        }

        public static Px BubbleBuy()
        {
            var p = N(48, 48, 2f);
            BubbleBase(p);
            using (p.Plain())
            {
                p.Circle(19.5f, 32, 6, Red);
                p.Circle(28.5f, 32, 6, Red);
                p.Poly(Red, V(13.8f, 30), V(34.2f, 30), V(24, 19));
            }
            return p;
        }

        public static Px MarkerFlag()
        {
            var p = N(40, 40, 2f);
            p.RoundRect(8, 2, 12, 38, 2, WoodDark);
            p.Poly(Gold, V(12, 37), V(36, 31), V(12, 23));
            return p;
        }

        public static Px MarkerCamera()
        {
            var p = N(40, 40, 2f);
            p.RoundRect(12, 25, 22, 33, 2, Pink);
            p.RoundRect(3, 6, 37, 29, 6, Pink);
            p.Circle(20, 17, 7.5f, Navy);
            using (p.Plain()) p.Circle(18, 19, 2.5f, new Color(1f, 1f, 1f, 0.7f));
            return p;
        }

        public static Px MarkerCrown()
        {
            var p = N(40, 40, 2f);
            p.Poly(Gold, V(5, 7), V(35, 7), V(36, 29), V(27, 19), V(20, 32), V(13, 19), V(4, 29));
            using (p.Plain()) p.Circle(20, 13, 3, Red);
            return p;
        }

        public static Px MarkerMystery()
        {
            var p = N(40, 40, 2f);
            p.Circle(20, 20, 17, Orange);
            using (p.Plain())
            {
                p.RoundRect(17.5f, 16, 22.5f, 30, 2.5f, Paper);
                p.Circle(20, 10, 3, Paper);
            }
            return p;
        }

        public static Px White()
        {
            var p = new Px(8, 8, 1f);
            p.Rect(0, 0, 8, 8, Color.white);
            return p;
        }
    }
}
