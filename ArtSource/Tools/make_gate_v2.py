"""
Cổng chào mới (cổng đỏ có cờ Việt Nam - Hàn Quốc): viết chữ lên tấm biển kem rồi xuất Assets/_Game/Art/Map/gate_arch.png.

  py -3 -I make_gate_v2.py [ảnh nguồn] [chữ]

Mặc định nguồn là ArtSource/Sheets/ui2/gate_v2.png, chữ "Hội chợ Việt - Hàn" (font Nunito Black của game).
Ảnh ra rộng 960 điểm ảnh, neo ở chân cột (game nạp với 200 điểm ảnh / đơn vị như cổng cũ).
"""
import os, sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(HERE, '..', 'Sheets', 'ui2', 'gate_v2.png')
FONT = os.path.join(ASSETS, '..', 'Fonts', 'Nunito-Black.ttf')

# Tấm biển kem trên ảnh nguồn 1536 x 1024 (x0, y0, x1, y1) và màu chữ đỏ đậm hợp viền cổng.
SIGN = (463, 244, 1085, 369)
TEXT_COLOR = (150, 30, 24, 255)
OUT_WIDTH = 960


def fit_font(draw, text, max_w, max_h):
    size = max_h
    while size > 10:
        font = ImageFont.truetype(FONT, size)
        l, t, r, b = draw.textbbox((0, 0), text, font=font)
        if r - l <= max_w and b - t <= max_h:
            return font
        size -= 2
    return ImageFont.truetype(FONT, size)


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else SOURCE
    text = sys.argv[2] if len(sys.argv) > 2 else 'Hội chợ Việt - Hàn'
    gate = Image.open(src).convert('RGBA')
    draw = ImageDraw.Draw(gate)

    x0, y0, x1, y1 = SIGN
    pad_x, pad_y = 34, 22
    font = fit_font(draw, text, (x1 - x0) - 2 * pad_x, (y1 - y0) - 2 * pad_y)
    l, t, r, b = draw.textbbox((0, 0), text, font=font)
    # Căn giữa theo khung chữ thật (bỏ phần thừa trên/dưới của font) để chữ nằm đúng giữa biển.
    tx = (x0 + x1) / 2 - (l + r) / 2
    ty = (y0 + y1) / 2 - (t + b) / 2
    draw.text((tx, ty), text, font=font, fill=TEXT_COLOR)

    # Quầng sáng quanh cổng có alpha 0 nên cắt theo alpha là sát cổng.
    gate = gate.crop(gate.getchannel('A').getbbox())
    gate = gate.resize((OUT_WIDTH, round(gate.height * OUT_WIDTH / gate.width)), Image.LANCZOS)
    out = os.path.join(ASSETS, 'Map', 'gate_arch.png')
    gate.save(out)
    print('gate', gate.size, 'font', font.size, '->', out)


main()
