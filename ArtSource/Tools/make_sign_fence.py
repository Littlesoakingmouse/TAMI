"""
Cắt bộ hình bảng tên, khung thông báo, biển gỗ mở khoá và hàng rào đường.

  py -3 -I make_sign_fence.py

Nguồn: TAMIGAME/dialog/name.png, TAMIGAME/dialog/notification.png, ArtSource/Sheets/ui2/*.webp (nền đã trong suốt).
- Art/UI/frame_name.png    : bảng tên 9-slice (viền đen dày, nền trắng để nhuộm), thu nhỏ 1/2.
- Art/UI/frame_notice.png  : khung thông báo 9-slice (viền đen mảnh), thu nhỏ 1/4.
- Art/UI/sign_wood.png     : biển gỗ có chân cho biển mở rộng phố / mở khu (rộng 384 điểm ảnh = 3 đơn vị).
- Art/Map/fence.png        : một chu kỳ hàng rào (thanh ngang + 6 cọc) lát lặp được theo chiều ngang.
- Art/Map/fence_post.png   : cột gỗ đứng ở hai đầu mỗi đoạn rào.
"""
import os, sys

from PIL import Image, ImageChops, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.join(HERE, '..', 'Sheets', 'ui2')
DIALOG = os.path.join(HERE, '..', '..', '..', 'dialog')


def scaled(im, factor):
    return im.resize((round(im.width * factor), round(im.height * factor)), Image.LANCZOS)


def rounded_ends(post):
    """Bo tròn hai đầu cột và kẻ viền nâu đậm: cột kéo dài 9-slice (viền trên dưới 14) mà hai đầu thò ra vẫn gọn."""
    ss = 4
    w, h = post.size
    mask = Image.new('L', (w * ss, h * ss), 0)
    line = Image.new('RGBA', (w * ss, h * ss), (0, 0, 0, 0))
    box = [0, 0, w * ss - 1, h * ss - 1]
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=11 * ss, fill=255)
    ImageDraw.Draw(line).rounded_rectangle(box, radius=11 * ss, outline=(74, 36, 16, 255), width=3 * ss)
    mask = mask.resize((w, h), Image.LANCZOS)
    line = line.resize((w, h), Image.LANCZOS)
    out = post.copy()
    out.putalpha(ImageChops.multiply(post.getchannel('A'), mask))
    out.alpha_composite(line)
    return out


def main():
    ui = os.path.join(ASSETS, 'UI')
    tiles = os.path.join(ASSETS, 'Map')

    name = Image.open(os.path.join(DIALOG, 'name.png')).convert('RGBA')
    scaled(name, 0.5).save(os.path.join(ui, 'frame_name.png'))

    notice = Image.open(os.path.join(DIALOG, 'notification.png')).convert('RGBA')
    scaled(notice, 0.25).save(os.path.join(ui, 'frame_notice.png'))

    sign = Image.open(os.path.join(SHEETS, 'sign_wood.webp')).convert('RGBA')
    sign = sign.crop(sign.getbbox())
    sign.resize((384, round(sign.height * 384 / sign.width)), Image.LANCZOS).save(os.path.join(ui, 'sign_wood.png'))

    # Hàng rào: cọc cách nhau ~263,8 điểm ảnh; cắt đúng 6 chu kỳ để lát nối liền (thanh ngang chạy suốt).
    fence = Image.open(os.path.join(SHEETS, 'fence.webp')).convert('RGBA')
    period = 263.8
    x0 = 209
    box = fence.getbbox()
    fence = fence.crop((x0, box[1], round(x0 + 6 * period), box[3]))
    scaled(fence, 0.25).save(os.path.join(tiles, 'fence.png'))

    post = Image.open(os.path.join(SHEETS, 'fence_post.webp')).convert('RGBA')
    post = post.crop(post.getbbox())
    post = post.resize((40, round(post.height * 40 / post.width)), Image.LANCZOS)
    rounded_ends(post).save(os.path.join(tiles, 'fence_post.png'))

    for p in ('UI/frame_name.png', 'UI/frame_notice.png', 'UI/sign_wood.png', 'Map/fence.png', 'Map/fence_post.png'):
        print(p, Image.open(os.path.join(ASSETS, p)).size)


main()
