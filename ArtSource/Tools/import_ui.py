"""
Đưa hình giao diện của hoạ sĩ (ArtSource/Sheets/ui_*) vào Assets/_Game/Art/UI.

  py -3 -I import_ui.py

- ui_buttons.webp: 3 nút tròn (người bán, trang trí, nâng cấp), nền trắng.
- ui_hud_icons.webp: Ngọc, cài đặt (trên), thành tựu, xu (dưới), nền trắng.
- ui_speech_bubble.webp + ui_speech_tail.png: ô thoại 9-slice và đuôi ô thoại.
- ui_money_bags.webp: túi tiền trên mái quầy, túi nâu (có tiền) và túi đỏ (đầy quỹ), nền trắng.
Sau đó chạy menu "Tami/Cập nhật hình tạm và dữ liệu" trong Unity để gán vào GameArt.
"""
import os, sys
from collections import deque
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from cut_auto import split_masked
from cut_stall_sheets import ASSETS

SHEETS = os.path.join(os.path.dirname(__file__), '..', 'Sheets')
OUT = os.path.join(ASSETS, 'UI')

ICON_PX = 192        # cạnh dài của icon HUD sau khi thu nhỏ
BUTTON_PX = 256      # đường kính nút tròn
BUBBLE_SCALE = 0.25  # ô thoại gốc 2000x266, viền ~11 px
TAIL_OUTLINE_PX = 20  # viền đuôi gốc dày hơn viền ô thoại: thu nhỏ đuôi để hai viền bằng nhau
BUBBLE_OUTLINE_PX = 11
BAG_PX = 112         # chiều cao túi tiền (128 điểm ảnh mỗi đơn vị: cao 0,875 đơn vị trước khi nhân cỡ theo quỹ)


def remove_white(im, seeds=(), tol=28):
    """
    Bỏ nền trắng: loang từ mép ảnh (và các điểm `seeds`, ví dụ lỗ giữa bánh răng) qua các điểm gần trắng.
    Viền mềm sát nền được tách khỏi màu trắng để không còn quầng trắng quanh nét viền.
    """
    # Ảnh đã có nền trong suốt thì giữ nguyên.
    if im.mode == 'RGBA' and im.getpixel((0, 0))[3] == 0:
        return im
    rgb = np.asarray(im.convert('RGB')).astype(np.float32)
    H, W = rgb.shape[:2]
    whiteish = (rgb.min(-1) > 255 - tol) & (rgb.max(-1) - rgb.min(-1) < tol)
    bg = np.zeros((H, W), bool)
    q = deque()
    starts = [(0, x) for x in range(W)] + [(H - 1, x) for x in range(W)] + [(y, 0) for y in range(H)] + [(y, W - 1) for y in range(H)]
    for y, x in list(starts) + [(int(sy), int(sx)) for sx, sy in seeds]:
        if whiteish[y, x] and not bg[y, x]:
            bg[y, x] = True
            q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < H and 0 <= nx < W and whiteish[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True
                q.append((ny, nx))
    # Dải 3 điểm ảnh sát nền: độ đục theo độ tối so với nét viền nâu đậm.
    near = bg.copy()
    for _ in range(3):
        n = near.copy()
        n[1:] |= near[:-1]; n[:-1] |= near[1:]; n[:, 1:] |= near[:, :-1]; n[:, :-1] |= near[:, 1:]
        near = n
    alpha = np.where(bg, 0.0, 1.0)
    edge = near & ~bg
    a = np.clip((255 - rgb.min(-1)) / (255 - 70), 0, 1)
    alpha[edge] = a[edge]
    out = rgb.copy()
    safe = np.maximum(alpha, 1e-3)[..., None]
    out[edge] = np.clip((rgb[edge] - (1 - alpha[edge])[..., None] * 255) / safe[edge], 0, 255)
    rgba = np.dstack([out, alpha * 255]).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')


def crop_items(im, count, size, by_height=False, rows=2):
    """Cắt `count` hình theo cụm điểm ảnh (đọc theo hàng), thu nhỏ cho cạnh dài (hoặc chiều cao) bằng `size`."""
    out = []
    for box, mask in split_masked(im, count, rows=rows):
        part = im.crop(box)
        part.putalpha(Image.fromarray((np.asarray(part.getchannel('A')) * mask).astype(np.uint8)))
        k = size / (part.height if by_height else max(part.size))
        out.append(part.resize((round(part.width * k), round(part.height * k)), Image.LANCZOS))
    return out


def save(im, name):
    im.save(os.path.join(OUT, name))
    print(name, im.size)


def main():
    os.makedirs(OUT, exist_ok=True)

    buttons = remove_white(Image.open(os.path.join(SHEETS, 'ui_buttons.webp')))
    for im, name in zip(crop_items(buttons, 3, BUTTON_PX, rows=1), ('btn_managers', 'btn_decor', 'btn_upgrade')):
        save(im, name + '.png')

    src = Image.open(os.path.join(SHEETS, 'ui_hud_icons.webp'))
    # Lỗ giữa bánh răng là nền trắng bị viền bao kín: loang thêm từ tâm bánh răng.
    hud = remove_white(src, seeds=[(src.width * 0.738, src.height * 0.27)])
    for im, name in zip(crop_items(hud, 4, ICON_PX), ('icon_gem', 'icon_settings', 'icon_trophy', 'icon_coin')):
        save(im, name + '.png')

    # Hai túi cùng chiều cao; MoneyBag tự phóng to/thu nhỏ theo lượng tiền trong quỹ.
    bags = remove_white(Image.open(os.path.join(SHEETS, 'ui_money_bags.webp')))
    for im, name in zip(crop_items(bags, 2, BAG_PX, by_height=True, rows=1), ('money_bag', 'money_bag_full')):
        save(im, name + '.png')

    bubble = Image.open(os.path.join(SHEETS, 'ui_speech_bubble.webp')).convert('RGBA')
    save(bubble.resize((round(bubble.width * BUBBLE_SCALE), round(bubble.height * BUBBLE_SCALE)), Image.LANCZOS),
         'speech_bubble.png')

    # Đuôi đè lên mép dưới ô thoại: tô trắng nét viền cạnh trên để nét viền ô thoại liền với hai cạnh đuôi.
    tail = np.asarray(Image.open(os.path.join(SHEETS, 'ui_speech_tail.png')).convert('RGBA')).copy()
    dark = (tail[..., 3] > 0) & (tail[..., :3].max(-1) < 200)
    white = (tail[..., 3] > 128) & (tail[..., :3].min(-1) > 200)
    for x in range(tail.shape[1]):
        ys = np.nonzero(white[:, x])[0]
        if len(ys):
            top = dark[:, x] & (np.arange(tail.shape[0]) < ys[0])
            tail[top, x, :3] = 255
            tail[top, x, 3] = 255
    tail = Image.fromarray(tail, 'RGBA')
    k = BUBBLE_SCALE * BUBBLE_OUTLINE_PX / TAIL_OUTLINE_PX
    save(tail.resize((round(tail.width * k), round(tail.height * k)), Image.LANCZOS), 'speech_tail.png')


main()
