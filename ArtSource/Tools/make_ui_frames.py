"""
Vẽ bộ khung giao diện 9-slice tông trung tính (viền xám bạc nhạt, bo tròn, vệt sáng, gờ dưới), xuất vào
Assets/_Game/Art/UI.

  py -3 -I make_ui_frames.py [thư_mục_ra]

- frame_panel.png  (128x128, viền 9-slice 40): bảng trắng ngà viền xám bạc nhạt; dùng nguyên màu.
- frame_button.png (96x96, viền 32): nút nền sáng có gờ dưới; UI nhuộm theo màu nút (xanh, vàng, xám, đỏ).
- frame_row.png    (64x64, viền 22): dòng danh sách bo tròn nền trắng, nhuộm mờ.
- frame_knob.png   (64x64): núm tròn (núm thanh trượt, chấm báo thành tựu).
"""
import os, sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

SS = 4
OUTLINE = (61, 36, 26, 255)
SOFT_OUTLINE = (182, 182, 188, 255)  # xám bạc: viền nhẹ cho giao diện tông trung tính


def canvas(w, h):
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def rr(d, x0, y0, x1, y1, r, fill=None, outline=None, width=0):
    d.rounded_rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], radius=r * SS, fill=fill,
                        outline=outline, width=width * SS)


def shrink(im):
    return im.resize((im.width // SS, im.height // SS), Image.LANCZOS)


def panel():
    """Bảng trắng ngà: viền ngoài xám bạc, viền trong xám rất nhạt, vệt sáng phía trên."""
    im, d = canvas(128, 128)
    rr(d, 2, 2, 126, 126, 28, fill=(252, 252, 250, 255), outline=SOFT_OUTLINE, width=6)
    rr(d, 9, 9, 119, 32, 20, fill=(255, 255, 255, 255))
    rr(d, 12, 12, 116, 116, 20, outline=(236, 236, 236, 255), width=3)
    return shrink(im)


def button():
    """Nút bo tròn có gờ dưới, nền sáng xám trung tính: UI nhuộm theo màu nút nên viền và gờ
    thành tông đậm của chính màu đó (không còn viền đen)."""
    im, d = canvas(96, 96)
    rr(d, 2, 2, 94, 94, 26, fill=(205, 205, 205, 255), outline=(128, 128, 128, 255), width=5)
    rr(d, 7, 7, 89, 80, 21, fill=(246, 246, 246, 255))
    rr(d, 14, 11, 82, 25, 8, fill=(255, 255, 255, 255))
    return shrink(im)


def row():
    im, d = canvas(64, 64)
    rr(d, 1, 1, 63, 63, 18, fill=(255, 255, 255, 255))
    return shrink(im)


def knob():
    im, d = canvas(64, 64)
    d.ellipse([3 * SS, 3 * SS, 61 * SS, 61 * SS], fill=(255, 255, 255, 255), outline=SOFT_OUTLINE, width=5 * SS)
    d.ellipse([15 * SS, 11 * SS, 33 * SS, 23 * SS], fill=(236, 236, 238, 255))
    return shrink(im)


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ASSETS, 'UI')
    os.makedirs(out, exist_ok=True)
    for name, make in (('frame_panel', panel), ('frame_button', button), ('frame_row', row), ('frame_knob', knob)):
        im = make()
        im.save(os.path.join(out, name + '.png'))
        print(name, im.size)


main()
