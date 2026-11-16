"""
Cắt sheet nền trong suốt theo khoảng trống (không cần lưới đều), rồi đưa vào Assets.

  py -3 cut_auto.py food  <sheet> <id_hang1> <id_hang2> ...   (mỗi hàng 3 món của một quầy)
  py -3 cut_auto.py stall <sheet> <id_quay>                   (4 hình sạp: cấp 1-4, đọc trái sang phải, trên xuống dưới)
  (đồ trang trí: dùng import_decor.py, gọi cut_decor bên dưới)

Sạp: mọi quầy cùng một khổ (thân sạp cấp 1 rộng STALL_L1_UNITS đơn vị prefab), neo ở giữa đáy hình,
mặt quầy dò tự động. Ghi manifest như cut_stall_sheets.py để menu "Tami/Cập nhật hình tạm và dữ liệu" nạp vào.
"""
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import STALL_DIR, FOOD_DIR, FOOD_SCALE, FOOD_WIDTH_UNITS, load_manifest, save_manifest

STALL_L1_UNITS = 4.03      # bề ngang sạp cấp 1 (tính cả mái) theo đơn vị prefab, như quầy Bánh chưng
STALL_OUT_WIDTH = 440      # bề ngang ảnh sạp cấp 1 sau khi thu nhỏ (điểm ảnh)


def bands(profile, min_gap):
    """Các đoạn liên tục có điểm ảnh; hai đoạn cách nhau ít hơn min_gap thì gộp."""
    on = profile > 0
    out, start = [], None
    for i, v in enumerate(on):
        if v and start is None:
            start = i
        elif not v and start is not None:
            out.append([start, i])
            start = None
    if start is not None:
        out.append([start, len(on)])
    merged = []
    for b in out:
        if merged and b[0] - merged[-1][1] < min_gap:
            merged[-1][1] = b[1]
        else:
            merged.append(b)
    return merged


def components(mask):
    """Gán nhãn các cụm điểm ảnh liền nhau (4 hướng). Trả về list bbox (x0, y0, x1, y1, số điểm)."""
    H, W = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    out = []
    for y in range(H):
        for x in range(W):
            if not mask[y, x] or seen[y, x]:
                continue
            stack = [(y, x)]
            seen[y, x] = True
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while stack:
                cy, cx = stack.pop()
                n += 1
                x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((ny, nx))
            out.append([x0, y0, x1 + 1, y1 + 1, n])
    return out


def split(im, count, merge_gap=4):
    """
    Tách `count` hình lớn nhất (theo cụm điểm ảnh liền nhau), mảnh vụn như lấp lánh gộp vào hình gần nhất.
    Trả về các hàng (trên xuống dưới), mỗi hàng các bbox trái sang phải, theo toạ độ ảnh gốc.
    """
    a = np.asarray(im)[..., 3] > 16
    H, W = a.shape
    f = max(1, int(max(H, W) / 500))          # làm việc trên ảnh thu nhỏ cho nhanh
    small = a[: H // f * f, : W // f * f].reshape(H // f, f, W // f, f).any(axis=(1, 3))
    comps = components(small)
    # Gộp các cụm sát nhau (ví dụ bát bún chả và đĩa bún, đèn lồng treo cạnh sạp) thành một hình.
    merged = True
    while merged:
        merged = False
        for i in range(len(comps)):
            for j in range(i + 1, len(comps)):
                p, q = comps[i], comps[j]
                gap = max(p[0] - q[2], q[0] - p[2], p[1] - q[3], q[1] - p[3])
                if gap < merge_gap:
                    comps[i] = [min(p[0], q[0]), min(p[1], q[1]), max(p[2], q[2]), max(p[3], q[3]), p[4] + q[4]]
                    del comps[j]
                    merged = True
                    break
            if merged:
                break
    comps.sort(key=lambda c: -c[4])
    big, rest = comps[:count], comps[count:]
    for c in rest:
        cx, cy = (c[0] + c[2]) / 2, (c[1] + c[3]) / 2
        near = min(big, key=lambda b: (max(b[0] - cx, 0, cx - b[2]) ** 2 + max(b[1] - cy, 0, cy - b[3]) ** 2))
        dist = max(near[0] - cx, 0, cx - near[2]) + max(near[1] - cy, 0, cy - near[3])
        if dist < 25:                         # chỉ gộp mảnh nằm sát hình
            near[0], near[1] = min(near[0], c[0]), min(near[1], c[1])
            near[2], near[3] = max(near[2], c[2]), max(near[3], c[3])
    boxes = [(b[0] * f, b[1] * f, min(W, b[2] * f), min(H, b[3] * f)) for b in big]
    # Xếp hàng: hình cùng hàng khi tâm dọc chênh nhau ít hơn nửa chiều cao.
    boxes.sort(key=lambda b: (b[1] + b[3]) / 2)
    rows = []
    for b in boxes:
        cy = (b[1] + b[3]) / 2
        if rows and abs(cy - rows[-1][0]) < (b[3] - b[1]) / 2:
            rows[-1][1].append(b)
        else:
            rows.append([cy, [b]])
    return [sorted(r[1], key=lambda b: b[0]) for r in rows]


def split_2x2(im):
    """
    Sheet sạp luôn là lưới 2x2 nhưng khoảng cách không đều và đồ trang trí có thể chạm nhau:
    cắt theo đường ít điểm ảnh nhất quanh giữa sheet (dọc), rồi quanh giữa từng nửa (ngang), sau đó cắt sát.
    """
    a = np.asarray(im)[..., 3] > 16
    H, W = a.shape

    def valley(profile, lo, hi):
        seg = profile[int(lo):int(hi)]
        idx = np.flatnonzero(seg == seg.min())
        return int(lo) + int(idx[len(idx) // 2])

    x = valley(a.sum(axis=0), W * 0.35, W * 0.65)
    cells = []
    for cx0, cx1 in ((0, x), (x, W)):
        y = valley(a[:, cx0:cx1].sum(axis=1), H * 0.35, H * 0.65)
        for cy0, cy1 in ((0, y), (y, H)):
            sub = a[cy0:cy1, cx0:cx1]
            ys, xs = np.nonzero(sub)
            cells.append((cx0 + xs.min(), cy0 + ys.min(), cx0 + xs.max() + 1, cy0 + ys.max() + 1))
    # Thứ tự cấp: trên trái, trên phải, dưới trái, dưới phải.
    return [cells[0], cells[2], cells[1], cells[3]]


def split_masked(im, count, cell=8, attach=25, merge_gap=3, rows=2):
    """
    Tách `count` món lớn nhất theo cụm điểm ảnh liền nhau, kèm mặt nạ từng món: món to (ô che nắng)
    có khung chữ nhật đè lên món bên cạnh nên phải cắt theo mặt nạ chứ không cắt theo khung.
    Mảnh vụn (cánh hoa rơi, hơi nước) gộp vào món gần nhất. Trả về [(bbox, mặt nạ cùng cỡ bbox)]
    xếp theo hàng (nửa trên rồi nửa dưới sheet), trong hàng từ trái sang phải.
    """
    a = np.asarray(im.getchannel('A')) > 16   # chỉ đọc kênh alpha: sheet 4K đủ 4 kênh tốn nhiều bộ nhớ
    H, W = a.shape
    h, w = H // cell, W // cell
    small = a[: h * cell, : w * cell].reshape(h, cell, w, cell).any(axis=(1, 3))
    # Mảnh rời nằm sát nhau (dãy 3 chum, bàn và đệm ngồi) gộp thành một món: nới vùng có hình ra
    # merge_gap ô rồi gắn nhãn, hai mảnh cách nhau dưới 2 * merge_gap ô sẽ chung một nhãn.
    grown = small.copy()
    for _ in range(merge_gap):
        g = grown.copy()
        g[1:] |= grown[:-1]; g[:-1] |= grown[1:]; g[:, 1:] |= grown[:, :-1]; g[:, :-1] |= grown[:, 1:]
        grown = g
    label, comps = label_components(grown)
    label[~small] = -1
    sizes = np.bincount(label[small], minlength=len(comps))
    order = sorted(range(len(comps)), key=lambda i: -sizes[i])
    big = [[i] for i in order[:count]]
    for i in order[count:]:
        c = comps[i]
        cx, cy = (c[0] + c[2]) / 2, (c[1] + c[3]) / 2
        def dist(group):
            b = comps[group[0]]
            return max(b[0] - cx, 0, cx - b[2]) + max(b[1] - cy, 0, cy - b[3])
        near = min(big, key=dist)
        if dist(near) < attach:
            near.append(i)
    out = []
    for group in big:
        m = np.isin(label, group)
        # Nới mặt nạ 1 ô để không xén mất viền mềm của hình.
        grown = m.copy()
        grown[1:] |= m[:-1]; grown[:-1] |= m[1:]; grown[:, 1:] |= m[:, :-1]; grown[:, :-1] |= m[:, 1:]
        full = np.zeros((H, W), bool)
        full[: h * cell, : w * cell] = np.repeat(np.repeat(grown, cell, 0), cell, 1)
        full &= a
        ys, xs = np.nonzero(full)
        box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
        out.append((box, full[box[1]:box[3], box[0]:box[2]]))
    if rows == 1:
        return sorted(out, key=lambda o: o[0][0])
    # Lưới 2 hàng: món có tâm cao hơn thuộc hàng trên (món to như ô che nắng có thể vắt qua giữa sheet).
    half = count // 2
    out.sort(key=lambda o: (o[0][1] + o[0][3]) / 2)
    return sorted(out[:half], key=lambda o: o[0][0]) + sorted(out[half:], key=lambda o: o[0][0])


def label_components(mask):
    """Như components() nhưng trả thêm mảng nhãn: label[y, x] = số thứ tự cụm, -1 nếu trống."""
    H, W = mask.shape
    label = np.full((H, W), -1)
    out = []
    for y in range(H):
        for x in range(W):
            if not mask[y, x] or label[y, x] >= 0:
                continue
            n_id = len(out)
            stack = [(y, x)]
            label[y, x] = n_id
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while stack:
                cy, cx = stack.pop()
                n += 1
                x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and label[ny, nx] < 0:
                        label[ny, nx] = n_id
                        stack.append((ny, nx))
            out.append([x0, y0, x1 + 1, y1 + 1, n])
    return label, out


DECOR_PPU = 128.0   # điểm ảnh mỗi đơn vị của hình đồ trang trí sau khi thu nhỏ


def cut_decor(sheet, items, out_dir, manifest):
    """
    Cắt 4 món trang trí trong một sheet (đọc theo hàng) và thu nhỏ theo chiều cao mong muốn.
    items: [(id, chiều cao theo đơn vị thế giới)]. Ghi <id>.png vào out_dir và thêm vào manifest (dict theo id).
    """
    im = Image.open(sheet).convert('RGBA')
    parts = split_masked(im, len(items))
    for (item_id, height), (box, mask) in zip(items, parts):
        part = im.crop(box)
        alpha = np.asarray(part.getchannel('A')) * mask
        part.putalpha(Image.fromarray(alpha.astype(np.uint8)))
        k = height * DECOR_PPU / part.height
        small = part.resize((max(1, round(part.width * k)), max(1, round(part.height * k))), Image.LANCZOS)
        name = f'decor_{item_id}.png'
        small.save(os.path.join(out_dir, name))
        manifest[item_id] = {'file': name, 'ppu': DECOR_PPU}
        print('decor', item_id, small.size)


def cut_decor_single(path, item_id, height, out_dir, manifest):
    """Một món trang trí nằm riêng một file (nền trong suốt): cắt sát rồi thu nhỏ theo chiều cao mong muốn."""
    im = Image.open(path).convert('RGBA')
    part = im.crop(im.getchannel('A').point(lambda a: 255 if a > 16 else 0).getbbox())
    k = height * DECOR_PPU / part.height
    small = part.resize((max(1, round(part.width * k)), max(1, round(part.height * k))), Image.LANCZOS)
    name = f'decor_{item_id}.png'
    small.save(os.path.join(out_dir, name))
    manifest[item_id] = {'file': name, 'ppu': DECOR_PPU}
    print('decor', item_id, small.size)


def counter_top(cell):
    """
    Dò mặt quầy: hàng đặc kín đầu tiên nằm dưới ô cửa trong suốt (đèn, tua rua treo trong ô cửa chỉ phủ
    một phần bề ngang nên không tính). Trả về số điểm ảnh tính từ đáy hình.
    """
    a = np.asarray(cell)[..., 3] > 128
    H, W = a.shape
    fill = a[:, int(W * 0.2):int(W * 0.8)].mean(axis=1)
    y = int(H * 0.2)
    while y < H and fill[y] > 0.5:
        y += 1              # còn trong mái
    while y < H and fill[y] < 0.85:
        y += 1              # ô cửa
    return H - (y + int(H * 0.03))


def cut_food(sheet, ids):
    os.makedirs(FOOD_DIR, exist_ok=True)
    im = Image.open(sheet).convert('RGBA')
    rows = split(im, 3 * len(ids))
    path = os.path.join(FOOD_DIR, 'food_art.json')
    m = load_manifest(path)
    for stall_id, row in zip(ids, rows):
        dishes = []
        for i, (x0, y0, x1, y1) in enumerate(row[:3]):
            part = im.crop((x0, y0, x1, y1))
            w, h = part.size
            k = 180.0 / w  # món rộng ~180 điểm ảnh sau khi thu nhỏ
            small = part.resize((round(w * k), round(h * k)), Image.LANCZOS)
            name = f'{stall_id}_{i + 1}.png'
            small.save(os.path.join(FOOD_DIR, name))
            dishes.append({'file': name, 'ppu': small.width / FOOD_WIDTH_UNITS})
        m[stall_id] = {'dishes': dishes}
        print('food', stall_id, len(row), 'mon')
    save_manifest(path, m)


def cut_stall(sheet, stall_id):
    os.makedirs(STALL_DIR, exist_ok=True)
    im = Image.open(sheet).convert('RGBA')
    cells = split_2x2(im)
    if len(cells) != 4:
        raise SystemExit(f'{sheet}: tìm thấy {len(cells)} hình sạp, cần 4')
    l1 = im.crop(cells[0])
    k = STALL_OUT_WIDTH / l1.width               # cùng một tỉ lệ thu nhỏ cho cả 4 cấp
    ppu_src = l1.width / STALL_L1_UNITS
    top_px = counter_top(l1)
    levels = []
    for i, box in enumerate(cells):
        part = im.crop(box)
        small = part.resize((round(part.width * k), round(part.height * k)), Image.LANCZOS)
        name = f'{stall_id}_{i + 1}.png'
        small.save(os.path.join(STALL_DIR, name))
        levels.append({'file': name, 'pivot': [0.5, 0.0]})
    path = os.path.join(STALL_DIR, 'stall_art.json')
    m = load_manifest(path)
    m[stall_id] = {'ppu': ppu_src * k, 'counterTop': top_px / ppu_src,
                   'dishSpacing': 0.95 * STALL_L1_UNITS / 4.03, 'dishScale': 0.9, 'levels': levels}
    save_manifest(path, m)
    print('stall', stall_id, 'counterTop', round(top_px / ppu_src, 2), [c[2] - c[0] for c in cells])


if __name__ == '__main__':
    if sys.argv[1] == 'food':
        cut_food(sys.argv[2], sys.argv[3:])
    else:
        cut_stall(sys.argv[2], sys.argv[3])
