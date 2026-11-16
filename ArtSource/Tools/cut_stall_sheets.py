"""
Cắt sheet sạp hàng (2x2, cấp 1-4) và sheet món ăn (3x3, mỗi hàng 3 món của một quầy) vào Assets.

  py -3 cut_stall_sheets.py stall <sheet> <stall_id> [ppu_goc] [mat_quay_px]
  py -3 cut_stall_sheets.py food <sheet> <stall_id_hang1> [<stall_id_hang2> ...]

Sạp: 4 cấp dùng chung một tỉ lệ và cùng điểm neo (giữa đáy hình = chân quầy) để không bị nhảy khi nâng cấp.
  ppu_goc: số điểm ảnh gốc ứng với 1 đơn vị prefab (nhỏ hơn = quầy to hơn).
  mat_quay_px: mặt quầy cao hơn chân bao nhiêu điểm ảnh gốc (chỗ bày món, người bán đứng sau).
Ghi manifest Assets/_Game/Art/Stalls/stall_art.json và Assets/_Game/Art/Food/food_art.json
để menu "Tami/Cập nhật hình tạm và dữ liệu" đặt pivot, tỉ lệ và gán vào StallData.
"""
import json, os, sys
import numpy as np
from PIL import Image

ASSETS = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', '_Game', 'Art')
STALL_DIR = os.path.join(ASSETS, 'Stalls')
FOOD_DIR = os.path.join(ASSETS, 'Food')

# Sạp mặc định (sheet đầu tiên): mặt quầy cao hơn chân bao nhiêu điểm ảnh gốc, số điểm ảnh gốc mỗi đơn vị.
STALL_COUNTER_TOP_PX = 300
STALL_PPU_SRC = 268.0
STALL_SCALE = 0.5

FOOD_SCALE = 1 / 3
FOOD_WIDTH_UNITS = 0.75


def load_manifest(path):
    """Manifest dạng {"items": [{"id": ...}]} để Unity JsonUtility đọc được; trả về dict theo id."""
    if not os.path.exists(path):
        return {}
    items = json.load(open(path, encoding='utf-8')).get('items', [])
    return {it['id']: it for it in items}


def save_manifest(path, m):
    items = [dict(v, id=k) for k, v in m.items()]
    json.dump({'items': items}, open(path, 'w', encoding='utf-8'), indent=1)


def trim(cell):
    a = np.asarray(cell)[..., 3]
    ys, xs = np.nonzero(a > 8)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def cut_stall(sheet, stall_id, ppu_src=STALL_PPU_SRC, counter_top_px=STALL_COUNTER_TOP_PX):
    os.makedirs(STALL_DIR, exist_ok=True)
    im = Image.open(sheet).convert('RGBA')
    cw, ch = im.width // 2, im.height // 2
    k = 1.0
    levels = []
    for i in range(4):
        r, c = divmod(i, 2)
        cell = im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
        x0, y0, x1, y1 = trim(cell)
        # Neo ở giữa đáy hình (chân quầy); các cấp vẽ cân đối nên giữa khung trùng giữa quầy.
        ax, ay = (x0 + x1) / 2, y1
        part = cell.crop((x0, y0, x1, y1))
        w, h = part.size
        small = part.resize((round(w * STALL_SCALE), round(h * STALL_SCALE)), Image.LANCZOS)
        name = f'{stall_id}_{i + 1}.png'
        small.save(os.path.join(STALL_DIR, name))
        levels.append({'file': name, 'pivot': [(ax - x0) / w, (y1 - ay) / h]})
    path = os.path.join(STALL_DIR, 'stall_art.json')
    m = load_manifest(path)
    # Món bày cách nhau khoảng 205 điểm ảnh gốc trên mặt quầy.
    m[stall_id] = {'ppu': ppu_src * k * STALL_SCALE, 'counterTop': counter_top_px / ppu_src,
                   'dishSpacing': 205.0 / ppu_src, 'dishScale': 0.9, 'levels': levels}
    save_manifest(path, m)
    print('stall', stall_id, [l['file'] for l in levels])


def cut_food(sheet, stall_ids):
    os.makedirs(FOOD_DIR, exist_ok=True)
    im = Image.open(sheet).convert('RGBA')
    cw, ch = im.width // 3, im.height // 3
    path = os.path.join(FOOD_DIR, 'food_art.json')
    m = load_manifest(path)
    for r, stall_id in enumerate(stall_ids):
        dishes = []
        for c in range(3):
            cell = im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))
            x0, y0, x1, y1 = trim(cell)
            part = cell.crop((x0, y0, x1, y1))
            w, h = part.size
            small = part.resize((round(w * FOOD_SCALE), round(h * FOOD_SCALE)), Image.LANCZOS)
            name = f'{stall_id}_{c + 1}.png'
            small.save(os.path.join(FOOD_DIR, name))
            dishes.append({'file': name, 'ppu': small.width / FOOD_WIDTH_UNITS})
        m[stall_id] = {'dishes': dishes}
        print('food', stall_id, [d['file'] for d in dishes])
    save_manifest(path, m)


if __name__ == '__main__':
    kind = sys.argv[1]
    if kind == 'stall':
        extra = [float(v) for v in sys.argv[4:6]]
        cut_stall(sys.argv[2], sys.argv[3], *extra)
    else:
        cut_food(sys.argv[2], sys.argv[3:])
