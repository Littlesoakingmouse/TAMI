using UnityEngine;

namespace Tami.EditorTools
{
    /// <summary>Hình tạm cho sân khấu, chân dung, biểu tượng giao diện, đồ trang trí và nghệ sĩ.</summary>
    public static partial class PlaceholderArt
    {
        // ---------- Sân khấu và mũi tên ----------

        public static Px Stage()
        {
            var p = N(256, 192, 2.5f);
            using (p.Plain()) p.Ellipse(128, 8, 124, 8, Shadow);
            // Phông nền: rèm đỏ
            p.RoundRect(16, 40, 240, 172, 8, C("#7A2A22"));
            for (int i = 0; i < 7; i++)
            {
                float x = 22 + i * 31;
                p.RoundRect(x, 44, x + 26, 168, 12, i % 2 == 0 ? Red : C("#B33629"));
            }
            // Mái
            p.Poly(C("#A8382C"), V(0, 166), V(14, 156), V(242, 156), V(256, 166), V(238, 190), V(18, 190));
            p.RoundRect(10, 152, 246, 162, 4, Gold);
            Lantern(p, 40, 136, Red);
            Lantern(p, 216, 136, Blue);
            // Sàn gỗ
            p.RoundRect(0, 4, 256, 48, 8, Wood);
            p.RoundRect(4, 34, 252, 48, 5, C("#D59A5E"));
            for (int i = 0; i < 8; i++) p.Circle(20 + i * 31, 19, 5.5f, Gold);
            return p;
        }

        /// <summary>Vòng sáng vàng dưới chân nhân vật chính.</summary>
        public static Px PlayerRing()
        {
            var p = new Px(96, 40);
            p.Ellipse(48, 20, 46, 18, new Color(1f, 0.82f, 0.3f, 0.85f));
            p.Ellipse(48, 20, 40, 14, new Color(0.35f, 0.25f, 0.1f, 0.3f));
            return p;
        }

        public static Px Arrow()
        {
            var p = N(64, 80, 3.5f);
            p.Poly(Gold, V(32, 4), V(60, 36), V(44, 36), V(44, 76), V(20, 76), V(20, 36), V(4, 36));
            using (p.Plain()) p.RoundRect(25, 40, 30, 70, 2.5f, new Color(1f, 1f, 1f, 0.5f));
            return p;
        }

        public static Px Gem()
        {
            var p = N(64, 64, 3f);
            p.Poly(C("#5CC3E8"), V(32, 4), V(60, 38), V(49, 56), V(15, 56), V(4, 38));
            using (p.Plain())
            {
                p.Poly(C("#A8E6F7"), V(18, 53), V(46, 53), V(40, 41), V(24, 41));
                p.Poly(C("#86D8F2"), V(32, 9), V(40, 40), V(24, 40));
            }
            return p;
        }

        // ---------- Chân dung nhân vật dẫn truyện (đầu chibi trong khung tròn) ----------

        public static Px Portrait(string who)
        {
            var p = N(192, 192, 4f);
            switch (who)
            {
                case "mayor": // Ông trưởng phố: tóc bạc, ria mép
                    p.Circle(96, 96, 90, C("#F7D27A"));
                    BigHead(p, Skin, C("#E6E1DA"), false);
                    p.Ellipse(98, 70, 16, 6, C("#E6E1DA"));
                    break;
                case "minji": // Minji: tóc đen dài, băng đô hồng
                    p.Circle(96, 96, 90, Pink);
                    BigHead(p, C("#F6D3A5"), C("#241B1B"), true);
                    p.RoundRect(40, 134, 152, 146, 6, C("#E86A8C"));
                    break;
                case "ongba": // Ông bà: khăn đóng xanh, râu trắng
                    p.Circle(96, 96, 90, C("#A8D5D8"));
                    BigHead(p, Skin, C("#E6E1DA"), false);
                    p.RoundRect(42, 128, 150, 158, 14, Navy);
                    using (p.Plain()) p.Line(46, 140, 146, 140, 3, C("#5A6A92"));
                    p.Poly(C("#F4F1EA"), V(80, 52), V(116, 52), V(98, 30));
                    break;
                default: // Nhân vật chính (dự phòng khi thiếu hình hoạ sĩ): tóc cam, mũ đỏ
                    p.Circle(96, 96, 90, Green);
                    BigHead(p, Skin, Orange, false);
                    p.Ellipse(96, 140, 54, 16, Red);
                    p.RoundRect(96, 128, 166, 138, 5, Red);
                    break;
            }
            return p;
        }

        static void BigHead(Px p, Color skin, Color hair, bool longHair)
        {
            if (longHair) p.RoundRect(34, 28, 158, 120, 30, hair);
            p.Ellipse(96, 104, 66, 58, hair);
            p.RoundRect(56, 46, 140, 128, 26, skin);
            p.Poly(hair, V(46, 120), V(146, 120), V(142, 152), V(50, 156));
            using (p.Plain())
            {
                p.Ellipse(70, 74, 12, 9, Blush);
                p.Ellipse(126, 74, 12, 9, Blush);
                p.Ellipse(78, 92, 5, 7, Ink);
                p.Ellipse(118, 92, 5, 7, Ink);
                p.Ellipse(98, 88, 6, 1.8f, Ink);
            }
        }

        // ---------- Biểu tượng giao diện (96x96) ----------

        public static Px IconTrophy()
        {
            var p = N(96, 96, 3.5f);
            p.RoundRect(28, 6, 68, 18, 4, GoldDark);
            p.RoundRect(42, 16, 54, 40, 3, GoldDark);
            p.Ellipse(19, 68, 11, 13, Gold);
            p.Ellipse(77, 68, 11, 13, Gold);
            p.Poly(Gold, V(20, 88), V(76, 88), V(70, 54), V(60, 40), V(36, 40), V(26, 54));
            using (p.Plain()) p.Circle(48, 66, 8, C("#FFE9A8"));
            return p;
        }

        public static Px IconSettings()
        {
            var p = N(96, 96, 3.5f);
            var c = C("#E8DFD3");
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                p.Circle(48 + Mathf.Cos(a) * 34, 48 + Mathf.Sin(a) * 34, 9, c);
            }
            p.Circle(48, 48, 32, c);
            p.Circle(48, 48, 12, C("#8D7F75"));
            return p;
        }

        public static Px IconGift()
        {
            var p = N(96, 96, 3.5f);
            p.Ellipse(34, 78, 15, 10, Gold);
            p.Ellipse(62, 78, 15, 10, Gold);
            p.RoundRect(12, 8, 84, 58, 7, Red);
            p.RoundRect(6, 52, 90, 70, 6, C("#E05244"));
            p.Rect(41, 8, 55, 70, Gold);
            return p;
        }

        public static Px TabStreet()
        {
            var p = N(96, 96, 3.5f);
            p.RoundRect(14, 8, 82, 52, 6, Cream);
            p.RoundRect(38, 8, 58, 36, 4, Wood);
            p.Poly(Red, V(4, 50), V(92, 50), V(80, 84), V(16, 84));
            return p;
        }

        public static Px TabManager()
        {
            var p = N(96, 96, 3.5f);
            p.RoundRect(18, 6, 78, 46, 16, Paper);
            p.RoundRect(34, 6, 62, 40, 6, Orange);
            p.RoundRect(28, 42, 68, 76, 14, Skin);
            using (p.Plain())
            {
                p.Ellipse(41, 60, 2.5f, 3.5f, Ink);
                p.Ellipse(57, 60, 2.5f, 3.5f, Ink);
            }
            p.Circle(36, 82, 9, Paper);
            p.Circle(60, 82, 9, Paper);
            p.Circle(48, 86, 10, Paper);
            p.RoundRect(28, 70, 68, 80, 4, Paper);
            return p;
        }

        public static Px TabDecor()
        {
            var p = N(96, 96, 3.5f);
            using (p.Plain()) p.Line(48, 80, 48, 94, 4, Ink);
            p.RoundRect(32, 72, 64, 82, 3, Gold);
            p.RoundRect(32, 14, 64, 24, 3, Gold);
            p.Ellipse(48, 48, 30, 30, Red);
            using (p.Plain()) p.Ellipse(38, 56, 6, 10, new Color(1f, 1f, 1f, 0.4f));
            using (p.Plain()) p.Line(48, 4, 48, 14, 3, Gold);
            return p;
        }

        public static Px TabShop()
        {
            var p = N(96, 96, 3.5f);
            using (p.WithOutline(9f)) p.Line(32, 60, 48, 82, 1, Teal);
            using (p.WithOutline(9f)) p.Line(64, 60, 48, 82, 1, Teal);
            using (p.Plain())
            {
                p.Line(32, 60, 48, 82, 6, Teal);
                p.Line(64, 60, 48, 82, 6, Teal);
            }
            p.RoundRect(14, 6, 82, 64, 12, Teal);
            p.Circle(48, 34, 12, Gold);
            return p;
        }

        // ---------- Đồ trang trí (96x128, chân ở dưới) ----------

        public static Px Decoration(string id)
        {
            var p = N(96, 128, 2.5f);
            using (p.Plain()) p.Ellipse(48, 5, 30, 5, Shadow);
            switch (id)
            {
                case "chum_nuoc":
                    p.Ellipse(48, 40, 32, 34, C("#B0653A"));
                    p.RoundRect(30, 68, 66, 82, 5, C("#A0582F"));
                    using (p.Plain()) p.Ellipse(48, 80, 15, 3.5f, C("#3E5B6B"));
                    using (p.Plain()) p.Ellipse(34, 48, 4, 9, new Color(1f, 1f, 1f, 0.35f));
                    break;
                case "cau_doi":
                    p.RoundRect(10, 2, 86, 10, 3, WoodDark);
                    foreach (float x in new[] { 14f, 58f })
                    {
                        p.RoundRect(x, 10, x + 24, 120, 4, Red);
                        using (p.Plain()) for (int i = 0; i < 4; i++) p.Circle(x + 12, 28 + i * 24, 5.5f, Gold);
                    }
                    break;
                case "den_long":
                    p.RoundRect(45, 0, 51, 120, 3, WoodDark);
                    p.RoundRect(18, 114, 78, 120, 3, WoodDark);
                    Lantern(p, 24, 94, Red);
                    Lantern(p, 72, 94, Gold);
                    Lantern(p, 48, 68, Teal);
                    break;
                case "cay_mai":
                case "cay_dao":
                    bool mai = id == "cay_mai";
                    p.RoundRect(20, 2, 76, 26, 7, C("#B0653A"));
                    p.Line(48, 24, 46, 70, 7, C("#6E4328"));
                    p.Line(46, 60, 26, 92, 4.5f, C("#6E4328"));
                    p.Line(46, 64, 72, 98, 4.5f, C("#6E4328"));
                    var blossom = mai ? C("#F7D04A") : Pink;
                    var center = mai ? Orange : C("#D9707E");
                    float[] bx = { 22, 34, 50, 64, 76, 30, 58, 44, 70, 18 };
                    float[] by = { 92, 104, 110, 104, 94, 80, 86, 96, 80, 78 };
                    for (int i = 0; i < bx.Length; i++)
                    {
                        using (p.WithOutline(1.8f)) p.Circle(bx[i], by[i], 7, blossom);
                        using (p.Plain()) p.Circle(bx[i], by[i], 2.5f, center);
                    }
                    break;
                case "cay_neu":
                    for (int i = 0; i < 6; i++) p.RoundRect(44, 4 + i * 19, 52, 23 + i * 19, 2, i % 2 == 0 ? Green : C("#6E9A45"));
                    p.Poly(Red, V(52, 122), V(80, 114), V(52, 104));
                    p.Circle(48, 123, 4, Gold);
                    break;
                case "xe_dap_hoa":
                    p.Circle(24, 24, 17, Black);
                    using (p.Plain()) p.Circle(24, 24, 12, C("#D8D2C8"));
                    p.Circle(72, 24, 17, Black);
                    using (p.Plain()) p.Circle(72, 24, 12, C("#D8D2C8"));
                    p.Line(24, 24, 46, 54, 5, Teal);
                    p.Line(46, 54, 72, 24, 5, Teal);
                    p.Line(46, 54, 70, 56, 5, Teal);
                    p.RoundRect(8, 44, 44, 64, 5, C("#C9955F"));
                    for (int i = 0; i < 6; i++) p.Circle(12 + i * 6, 68 + (i % 2) * 6, 6, i % 3 == 0 ? Pink : i % 3 == 1 ? Gold : Red);
                    break;
                case "onggi":
                    p.Ellipse(48, 30, 36, 25, C("#7A4E36"));
                    p.Ellipse(48, 68, 27, 19, C("#7A4E36"));
                    p.Ellipse(48, 98, 17, 13, C("#7A4E36"));
                    using (p.Plain())
                    {
                        p.Ellipse(34, 36, 4, 8, new Color(1f, 1f, 1f, 0.25f));
                        p.Ellipse(38, 72, 3, 6, new Color(1f, 1f, 1f, 0.25f));
                    }
                    break;
                case "bokjumeoni":
                    p.Line(10, 120, 86, 120, 5, WoodDark);
                    p.Line(12, 120, 12, 2, 5, WoodDark);
                    p.Line(84, 120, 84, 2, 5, WoodDark);
                    Color[] pouch = { Red, Blue, Gold, Teal };
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 24 + i * 16;
                        using (p.Plain()) p.Line(x, 118, x, 96, 2, Ink);
                        p.Circle(x, 86, 8.5f, pouch[i]);
                        p.RoundRect(x - 5, 92, x + 5, 98, 2, pouch[(i + 2) % 4]);
                    }
                    break;
                case "den_hoa_sen":
                    p.Line(10, 0, 10, 116, 4.5f, WoodDark);
                    p.Line(86, 0, 86, 116, 4.5f, WoodDark);
                    using (p.Plain()) p.Line(10, 114, 86, 114, 2, Ink);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 26 + i * 22;
                        using (p.Plain()) p.Line(x, 114, x, 96, 2, Ink);
                        p.Poly(C("#E58594"), V(x - 11, 88), V(x, 100), V(x + 11, 88));
                        p.Ellipse(x, 86, 11, 10, Pink);
                        using (p.Plain()) p.Circle(x, 85, 3, Gold);
                    }
                    break;
                case "co_obangsaek":
                    p.Line(20, 0, 20, 124, 5, WoodDark);
                    Color[] obang = { Navy, Red, Gold, Paper, Black };
                    p.Rect(22, 60, 86, 120, Paper);
                    using (p.Plain()) for (int i = 0; i < 5; i++) p.Rect(23.5f, 118.5f - (i + 1) * 11.6f, 84.5f, 118.5f - i * 11.6f, obang[i]);
                    break;
                case "jangseung":
                    foreach (float x in new[] { 28f, 68f })
                    {
                        p.RoundRect(x - 14, 2, x + 14, 112, 12, C("#B58A5A"));
                        p.RoundRect(x - 16, 104, x + 16, 124, 8, Black);
                        using (p.Plain())
                        {
                            p.Circle(x - 6, 86, 3.5f, Ink);
                            p.Circle(x + 6, 86, 3.5f, Ink);
                            p.RoundRect(x - 8, 64, x + 8, 70, 3, Red);
                        }
                        p.RoundRect(x - 10, 20, x + 10, 50, 3, Paper);
                    }
                    break;
            }
            return p;
        }

        // ---------- Nghệ sĩ biểu diễn (128x128, chân ở dưới) ----------

        public static Px Performer(string id)
        {
            var p = N(128, 128, 2.5f);
            switch (id)
            {
                case "mua_lan":
                    p.RoundRect(10, 4, 118, 50, 22, Gold);
                    for (int i = 0; i < 5; i++) p.Circle(20 + i * 22, 50, 10, Red);
                    p.Ellipse(64, 78, 48, 40, Red);
                    p.Ellipse(64, 74, 36, 28, Gold);
                    p.Circle(48, 86, 10, Paper);
                    p.Circle(80, 86, 10, Paper);
                    using (p.Plain())
                    {
                        p.Circle(50, 85, 5, Ink);
                        p.Circle(82, 85, 5, Ink);
                    }
                    p.RoundRect(44, 54, 84, 64, 4, C("#A8382C"));
                    p.Circle(64, 116, 8, Teal);
                    break;
                case "samulnori":
                    Dancer(p, Paper, Blue, Skin, C("#241B1B"));
                    p.Poly(Red, V(30, 54), V(56, 44), V(56, 64));
                    p.Poly(Red, V(98, 54), V(72, 44), V(72, 64));
                    p.RoundRect(52, 50, 76, 56, 2, Wood);
                    using (p.Plain()) p.Line(64, 118, 96, 126, 3, Gold);
                    break;
                case "roi_nuoc":
                    p.RoundRect(0, 0, 128, 30, 6, Blue);
                    using (p.Plain()) for (int i = 0; i < 6; i++) p.Ellipse(10 + i * 22, 26, 9, 3, C("#A9CBEF"));
                    p.RoundRect(44, 24, 84, 68, 12, Red);
                    p.Circle(64, 84, 17, C("#E9C46A"));
                    using (p.Plain())
                    {
                        p.Ellipse(58, 86, 2.2f, 3, Ink);
                        p.Ellipse(70, 86, 2.2f, 3, Ink);
                        p.Ellipse(55, 80, 4, 3, Blush);
                        p.Ellipse(73, 80, 4, 3, Blush);
                    }
                    p.Poly(Gold, V(44, 96), V(84, 96), V(64, 112));
                    break;
                case "buchaechum":
                    foreach (float x in new[] { 22f, 106f })
                    {
                        p.Circle(x, 70, 21, Red);
                        using (p.Plain())
                        {
                            p.Circle(x, 70, 15, Gold);
                            p.Circle(x, 70, 7, Teal);
                        }
                    }
                    Dancer(p, Pink, Red, Skin, C("#241B1B"));
                    break;
                case "quan_ho":
                    Dancer(p, C("#A8774E"), C("#6E4328"), Skin, C("#241B1B"));
                    p.Ellipse(64, 112, 34, 7, C("#3B2A22"));
                    using (p.Plain()) p.Ellipse(64, 113, 26, 4, Gold);
                    break;
                default:
                    p.Ellipse(24, 32, 18, 14, Wood);
                    using (p.Plain()) p.Ellipse(24, 38, 15, 5, Gold);
                    Dancer(p, Paper, Navy, Skin, C("#241B1B"));
                    p.Poly(Red, V(88, 70), V(118, 92), V(118, 60));
                    break;
            }
            return p;
        }

        static void Dancer(Px p, Color top, Color skirt, Color skin, Color hair)
        {
            p.Poly(skirt, V(38, 4), V(90, 4), V(76, 52), V(52, 52));
            p.RoundRect(34, 52, 48, 72, 6, top);
            p.RoundRect(80, 52, 94, 72, 6, top);
            p.RoundRect(48, 44, 80, 74, 10, top);
            // Đầu chibi to
            p.Ellipse(64, 96, 27, 25, hair);
            p.RoundRect(48, 72, 80, 104, 11, skin);
            p.Poly(hair, V(44, 100), V(84, 100), V(82, 112), V(46, 114));
            using (p.Plain())
            {
                p.Ellipse(55, 81, 4.5f, 3.6f, Blush);
                p.Ellipse(74, 81, 4.5f, 3.6f, Blush);
                p.Ellipse(58, 88, 1.8f, 2.6f, Ink);
                p.Ellipse(71, 88, 1.8f, 2.6f, Ink);
            }
        }
    }
}
