"""
Vẽ bàn ghế cho khu ăn uống, xuất vào Assets/_Game/Art/Dining (128 điểm ảnh mỗi đơn vị, neo ở đáy).

  py -3 -I make_dining_art.py [thư_mục_ra]

Vẽ bằng code theo nét của bộ đồ trang trí (viền nâu đậm, màu phẳng, vệt sáng):
- chair_plastic.png / table_plastic.png: ghế nhựa đỏ, bàn nhựa xanh vỉa hè (kiểu Việt).
- chair_wood.png / table_wood.png: ghế gỗ lưng chấn song, bàn gỗ (kiểu Hàn).
Ghế đứng sau khách, bàn che phía trước: lưng ghế lộ hai bên vai, mặt bàn che chân và bụng.
"""
import os, sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

PPU = 128
SS = 4                       # vẽ to gấp 4 rồi thu nhỏ cho viền mượt
OUTLINE = (61, 36, 26, 255)  # nâu đậm như viền các món trang trí
LINE = 4 * SS                # độ dày viền ~ 4 điểm ảnh sau thu nhỏ


def canvas(w, h):
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def box(d, x0, y0, x1, y1, fill, r=0):
    """Hình chữ nhật bo góc có viền, toạ độ theo điểm ảnh sau thu nhỏ."""
    xy = [x0 * SS, y0 * SS, x1 * SS, y1 * SS]
    d.rounded_rectangle(xy, radius=r * SS, fill=fill, outline=OUTLINE, width=LINE)


def poly(d, pts, fill):
    p = [(x * SS, y * SS) for x, y in pts]
    d.polygon(p, fill=fill)
    d.line(p + [p[0]], fill=OUTLINE, width=LINE, joint='curve')


def shrink(im):
    return im.resize((im.width // SS, im.height // SS), Image.LANCZOS)


def chair_plastic():
    """Ghế nhựa đỏ nhìn từ phía trước: lưng có 3 khe ngang, mặt ngồi, 4 chân xoè. Rộng 1,25, cao 1,4 đơn vị."""
    im, d = canvas(160, 180)
    red, light, dark = (214, 69, 56, 255), (240, 112, 92, 255), (168, 46, 40, 255)
    # Chân sau (thấy một phần giữa hai chân trước), vẽ trước để nằm dưới.
    poly(d, [(50, 116), (62, 116), (58, 176), (46, 176)], dark)
    poly(d, [(98, 116), (110, 116), (114, 176), (102, 176)], dark)
    # Chân trước xoè ra ngoài.
    poly(d, [(22, 116), (42, 116), (30, 177), (10, 177)], red)
    poly(d, [(118, 116), (138, 116), (150, 177), (130, 177)], red)
    # Lưng ghế bo tròn, có khe ngang và vệt sáng.
    box(d, 20, 6, 140, 102, red, r=24)
    d.rounded_rectangle([30 * SS, 14 * SS, 48 * SS, 94 * SS], radius=9 * SS, fill=light)
    for y in (30, 52, 74):
        d.rounded_rectangle([54 * SS, y * SS, 126 * SS, (y + 11) * SS], radius=5 * SS, fill=dark, outline=OUTLINE, width=SS * 2)
    # Mặt ngồi rộng hơn lưng một chút, mặt trên sáng.
    box(d, 10, 98, 150, 120, red, r=10)
    d.rounded_rectangle([18 * SS, 102 * SS, 142 * SS, 108 * SS], radius=3 * SS, fill=light)
    return shrink(im)


def chair_wood():
    """Ghế gỗ kiểu Hàn: hai cột lưng là chân sau, thanh trên cong, chấn song dọc, mặt ngồi, chân trước."""
    im, d = canvas(160, 180)
    wood, light, dark = (156, 92, 50, 255), (196, 128, 74, 255), (112, 64, 34, 255)
    box(d, 26, 112, 40, 177, wood, r=3)
    box(d, 120, 112, 134, 177, wood, r=3)
    box(d, 16, 18, 34, 177, dark, r=5)
    box(d, 126, 18, 144, 177, dark, r=5)
    box(d, 6, 6, 154, 26, wood, r=11)
    d.rounded_rectangle([14 * SS, 10 * SS, 146 * SS, 15 * SS], radius=2 * SS, fill=light)
    box(d, 32, 64, 128, 74, wood, r=4)
    for x in (54, 74, 94):
        box(d, x, 24, x + 12, 66, light, r=3)
    box(d, 10, 100, 150, 118, wood, r=7)
    d.rounded_rectangle([18 * SS, 104 * SS, 142 * SS, 109 * SS], radius=3 * SS, fill=light)
    return shrink(im)


def table(top, top_light, leg, cloth, cloth_alt, trim):
    """Bàn ăn 2 chỗ nhìn từ phía trước, phủ khăn dài gần chạm đất: khăn che hết chân khách ngồi phía sau,
    chỉ lộ thân trên và lưng ghế nên trông rõ là đang ngồi. Rộng 2,3, cao 0,875 đơn vị (2 khách đầu to
    ngồi cạnh nhau không đè đầu nhau).
    cloth_alt: màu ô caro xen kẽ (None = khăn trơn); trim: màu viền dưới khăn (None = không viền)."""
    W = 294
    im, d = canvas(W, 112)
    # Hai chân bàn lộ dưới mép khăn.
    box(d, 16, 80, 32, 110, leg, r=3)
    box(d, W - 32, 80, W - 16, 110, leg, r=3)
    # Khăn trải bàn.
    box(d, 6, 18, W - 6, 100, cloth, r=6)
    if cloth_alt is not None:
        sq = 16
        for gy in range(0, 5):
            for gx in range(0, W // sq + 1):
                if (gx + gy) % 2:
                    continue
                x0, y0 = 8 + gx * sq, 22 + gy * sq
                x1, y1 = min(x0 + sq, W - 8), min(y0 + sq, 98)
                if x0 < x1 and y0 < y1:
                    d.rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], fill=cloth_alt)
        d.rounded_rectangle([6 * SS, 18 * SS, (W - 6) * SS, 100 * SS], radius=6 * SS, outline=OUTLINE, width=LINE)
    if trim is not None:
        box(d, 6, 88, W - 6, 100, trim, r=4)
    # Mặt bàn dày phía trên khăn.
    box(d, 2, 4, W - 2, 26, top, r=9)
    d.rounded_rectangle([10 * SS, 8 * SS, (W - 10) * SS, 13 * SS], radius=3 * SS, fill=top_light)
    return shrink(im)


def stool(color, light, dark):
    """Ghế đẩu nhựa thấp vỉa hè nhìn từ phía trước: mặt ngồi có gờ, 4 chân xoè, khoét lỗ giữa thân.
    Rộng 0,56, cao 0,34 đơn vị: khách ngồi lên mặt ghế, chân đung đưa phía trước."""
    im, d = canvas(72, 44)
    # Chân sau thấp thoáng giữa hai chân trước.
    poly(d, [(22, 14), (28, 14), (26, 42), (20, 42)], dark)
    poly(d, [(44, 14), (50, 14), (52, 42), (46, 42)], dark)
    # Chân trước xoè.
    poly(d, [(6, 12), (18, 12), (12, 43), (2, 43)], color)
    poly(d, [(54, 12), (66, 12), (70, 43), (60, 43)], color)
    # Mặt ngồi và gờ.
    box(d, 2, 2, 70, 16, color, r=4)
    d.rounded_rectangle([7 * SS, 4 * SS, 65 * SS, 8 * SS], radius=2 * SS, fill=light)
    return shrink(im)


def low_table():
    """Bàn nhựa xanh thấp vỉa hè (không khăn): mặt bàn, gờ, 4 chân xoè. Rộng 1, cao 0,56 đơn vị."""
    blue, light, dark = (126, 182, 232, 255), (170, 212, 246, 255), (84, 136, 190, 255)
    im, d = canvas(128, 72)
    poly(d, [(36, 18), (44, 18), (42, 70), (34, 70)], dark)
    poly(d, [(84, 18), (92, 18), (94, 70), (86, 70)], dark)
    poly(d, [(10, 16), (26, 16), (18, 71), (4, 71)], blue)
    poly(d, [(102, 16), (118, 16), (124, 71), (110, 71)], blue)
    box(d, 2, 2, 126, 20, blue, r=6)
    d.rounded_rectangle([8 * SS, 5 * SS, 120 * SS, 10 * SS], radius=2 * SS, fill=light)
    return shrink(im)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ASSETS, 'Dining')
    os.makedirs(out, exist_ok=True)
    chair_plastic().save(os.path.join(out, 'chair_plastic.png'))
    chair_wood().save(os.path.join(out, 'chair_wood.png'))
    # Bàn nhựa xanh vỉa hè phủ khăn caro đỏ trắng (kiểu Việt), bàn gỗ phủ khăn đỏ viền vàng (kiểu Hàn).
    table((126, 182, 232, 255), (170, 212, 246, 255), (110, 165, 218, 255),
          (246, 240, 228, 255), (214, 69, 56, 255), None).save(os.path.join(out, 'table_plastic.png'))
    table((150, 88, 48, 255), (190, 124, 72, 255), (112, 64, 34, 255),
          (168, 44, 52, 255), None, (232, 178, 64, 255)).save(os.path.join(out, 'table_wood.png'))
    # Góc ghế nhựa vỉa hè: bàn thấp không khăn, ghế đẩu đỏ và xanh (khách có chân rời ngồi lộ chân).
    low_table().save(os.path.join(out, 'table_low.png'))
    stool((214, 69, 56, 255), (240, 112, 92, 255), (168, 46, 40, 255)).save(os.path.join(out, 'stool_red.png'))
    stool((110, 165, 218, 255), (160, 204, 240, 255), (76, 124, 176, 255)).save(os.path.join(out, 'stool_blue.png'))
    for n in ('chair_plastic', 'chair_wood', 'table_plastic', 'table_wood', 'table_low', 'stool_red', 'stool_blue'):
        im = Image.open(os.path.join(out, n + '.png'))
        print(n, im.size, 'units %.2f x %.2f' % (im.width / PPU, im.height / PPU))


main()
