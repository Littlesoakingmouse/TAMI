"""
Vẽ hình cho khu trò chơi dân gian, xuất vào Assets/_Game/Art/Decor (128 điểm ảnh mỗi đơn vị, neo ở đáy).

  py -3 -I make_folk_art.py [thư_mục_ra]

Cùng nét với bộ đồ trang trí (viền nâu đậm, màu phẳng, vệt sáng):
- game_da_cau.png: quả cầu lông (đế xếp nhiều lớp, chùm lông nhiều màu) - Đá cầu, Việt Nam.
- game_tuho.png:   bình tuho cổ cao có hai quai, cắm vài mũi tên - Ném tên vào bình, Hàn Quốc.
"""
import math, os, sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

SS = 4
OUTLINE = (61, 36, 26, 255)
LINE = 4 * SS


def canvas(w, h):
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def S(pts):
    return [(x * SS, y * SS) for x, y in pts]


def shrink(im):
    return im.resize((im.width // SS, im.height // SS), Image.LANCZOS)


def feather(d, base, angle, length, width, fill, light):
    """Một chiếc lông hình giọt nước, gốc ở base, nghiêng theo angle (độ, 0 = thẳng đứng)."""
    a = math.radians(angle)
    ux, uy = math.sin(a), -math.cos(a)        # hướng dọc lông
    px, py = -uy, ux                           # hướng ngang lông
    pts = []
    for i in range(25):
        t = i / 24
        # Bề ngang phình ở 2/3 thân lông, nhọn ở gốc, tròn ở ngọn.
        wdt = width * math.sin(math.pi * min(1.0, t * 0.75 + 0.05)) * (0.35 + 0.65 * t ** 0.5)
        pts.append((base[0] + ux * length * t + px * wdt, base[1] + uy * length * t + py * wdt))
    for i in range(24, -1, -1):
        t = i / 24
        wdt = width * math.sin(math.pi * min(1.0, t * 0.75 + 0.05)) * (0.35 + 0.65 * t ** 0.5)
        pts.append((base[0] + ux * length * t - px * wdt, base[1] + uy * length * t - py * wdt))
    d.polygon(S(pts), fill=fill)
    d.line(S(pts + [pts[0]]), fill=OUTLINE, width=LINE - SS, joint='curve')
    # Sống lông và vệt sáng.
    d.line(S([base, (base[0] + ux * length * 0.92, base[1] + uy * length * 0.92)]), fill=light, width=2 * SS)


def da_cau():
    im, d = canvas(170, 150)
    cx, by = 85, 124
    feathers = [(-34, (240, 236, 224, 255), (255, 255, 255, 255)),
                (34, (240, 236, 224, 255), (255, 255, 255, 255)),
                (-20, (232, 86, 74, 255), (255, 150, 132, 255)),
                (20, (82, 168, 226, 255), (150, 210, 250, 255)),
                (-7, (250, 196, 64, 255), (255, 232, 150, 255)),
                (7, (96, 190, 120, 255), (170, 230, 180, 255))]
    for ang, fill, light in feathers:
        feather(d, (cx + ang * 0.25, by - 8), ang, 104, 13, fill, light)
    # Đế cầu: các lớp đệm xếp chồng, lớp trên sáng hơn.
    layers = [(by + 18, 50, (150, 90, 60, 255)), (by + 8, 48, (196, 120, 76, 255)), (by - 2, 44, (214, 69, 56, 255))]
    for y, w, c in layers:
        d.rounded_rectangle([(cx - w / 2) * SS, (y - 10) * SS, (cx + w / 2) * SS, y * SS], radius=5 * SS,
                            fill=c, outline=OUTLINE, width=LINE - SS)
    d.rounded_rectangle([(cx - 16) * SS, (by - 10) * SS, (cx + 4) * SS, (by - 7) * SS], radius=SS, fill=(255, 150, 132, 255))
    d.ellipse([(cx - 6) * SS, (by - 15) * SS, (cx + 6) * SS, (by - 6) * SS], fill=(120, 72, 48, 255), outline=OUTLINE, width=2 * SS)
    return shrink(im)


def arrow(d, x0, y0, x1, y1, color):
    """Mũi tên cắm vào miệng bình: (x0,y0) ở trong bình, (x1,y1) là đuôi có lông."""
    d.line(S([(x0, y0), (x1, y1)]), fill=OUTLINE, width=7 * SS)
    d.line(S([(x0, y0), (x1, y1)]), fill=(222, 178, 112, 255), width=3 * SS)
    dx, dy = x1 - x0, y1 - y0
    n = math.hypot(dx, dy)
    ux, uy = dx / n, dy / n
    px, py = -uy, ux
    for side in (1, -1):
        p = [(x1 - ux * 2, y1 - uy * 2), (x1 - ux * 20, y1 - uy * 20),
             (x1 - ux * 24 + px * 9 * side, y1 - uy * 24 + py * 9 * side), (x1 - ux * 4 + px * 9 * side, y1 - uy * 4 + py * 9 * side)]
        d.polygon(S(p), fill=color)
        d.line(S(p + [p[0]]), fill=OUTLINE, width=2 * SS)


def tuho():
    im, d = canvas(140, 200)
    cx = 70
    body, light, dark = (70, 120, 170, 255), (130, 180, 220, 255), (44, 82, 124, 255)
    rim = (196, 150, 72, 255)
    # Mũi tên vẽ trước để nằm sau mép miệng bình.
    arrow(d, cx - 4, 92, cx - 34, 8, (232, 86, 74, 255))
    arrow(d, cx + 2, 92, cx + 26, 4, (82, 168, 226, 255))
    arrow(d, cx, 92, cx - 6, 2, (250, 196, 64, 255))
    # Lòng miệng bình (tối) phía sau mũi tên đã có; thân bình.
    d.ellipse(S([(cx - 46, 112), (cx + 46, 194)]), fill=body, outline=OUTLINE, width=LINE)
    d.ellipse(S([(cx - 34, 124), (cx - 18, 168)]), fill=light)
    # Cổ cao.
    d.rectangle(S([(cx - 15, 60), (cx + 15, 122)]), fill=body)
    d.line(S([(cx - 15, 60), (cx - 15, 120)]), fill=OUTLINE, width=LINE)
    d.line(S([(cx + 15, 60), (cx + 15, 120)]), fill=OUTLINE, width=LINE)
    d.rectangle(S([(cx - 9, 64), (cx - 5, 118)]), fill=light)
    # Hai quai tròn hai bên cổ.
    for sx in (-1, 1):
        x = cx + sx * 25
        d.ellipse(S([(x - 11, 70), (x + 11, 94)]), outline=OUTLINE, width=8 * SS)
        d.ellipse(S([(x - 11, 70), (x + 11, 94)]), outline=rim, width=3 * SS)
    # Đai vàng ở chân cổ và đế.
    d.rounded_rectangle(S([(cx - 20, 112), (cx + 20, 122)]), radius=3 * SS, fill=rim, outline=OUTLINE, width=LINE - SS)
    d.rounded_rectangle(S([(cx - 30, 184), (cx + 30, 196)]), radius=4 * SS, fill=dark, outline=OUTLINE, width=LINE - SS)
    # Miệng loe: chỉ vẽ nửa trước của vành để mũi tên trông như cắm vào trong.
    d.ellipse(S([(cx - 22, 52), (cx + 22, 66)]), outline=OUTLINE, width=LINE)
    d.chord(S([(cx - 22, 52), (cx + 22, 66)]), 0, 180, fill=rim, outline=OUTLINE, width=LINE - SS)
    return shrink(im)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ASSETS, 'Decor')
    for name, make in (('game_da_cau', da_cau), ('game_tuho', tuho)):
        im = make()
        im = im.crop(im.getbbox())
        im.save(os.path.join(out, name + '.png'))
        print(name, im.size)


main()
