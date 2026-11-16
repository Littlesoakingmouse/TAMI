"""Chuẩn bị asset NPC: phân loại từng phần, thu nhỏ 1/2, tính pivot và vị trí ghép, xuất manifest JSON."""
import json, os, sys
import numpy as np
from PIL import Image

SRC = r'E:\unity_pj\TAMIGAME\Tami_npc'
DST = r'E:\unity_pj\TAMIGAME\Tami\Assets\_Game\Art\Npc'
SCALE = 0.5
PPU_SRC = 192.0  # nhân vật mẫu (thỏ đội nón) cao 288 px = 1,5 đơn vị

# Khung chuẩn đo từ ảnh mẫu npc-tho1/Asset 2 (toạ độ y hướng xuống, gốc ở giữa mép trên áo).
HEAD_BOTTOM = (-9.0, 23.0)
ARM_L_CENTER = (-61.0, 33.5)
ARM_R_CENTER = (46.5, 23.5)
LEG_L_CENTER = (-21.0, 80.5)
LEG_R_CENTER = (23.5, 80.0)
# Giống nhân vật chính: tay phải nằm sau thân, tay trái nằm trước thân (và trước áo khoác), dưới đầu.
ORDER = {'leg_left': 0, 'leg_right': 0, 'arm_right': 1, 'body': 2, 'top': 3, 'arm_left': 4, 'head': 5}
# Váy hanbok rộng hơn áo: tay đặt lệch ra ngoài.
SKIRT_ARM_L_CENTER = (-72.0, 32.0)
SKIRT_ARM_R_CENTER = (60.0, 24.0)
# Hanbok: khớp vai nằm ở mép dải áo trên cùng, thụt vào và hạ xuống chừng này (px ảnh gốc).
SKIRT_SHOULDER_INSET = 4.0
SKIRT_SHOULDER_DROP = 12.0
# Đồ liền thân cao (có cà vạt): hạ đầu để cằm che đầu nút cà vạt chừng này (px ảnh gốc), tay hạ theo.
SUIT_TALL_HEIGHT = 140
SUIT_TIE_OVERLAP = 6.0


def tie_top(im):
    """Dòng trên cùng của cà vạt (nét viền tối đầu tiên ở giữa thân, dưới viền trên)."""
    a = np.asarray(im).astype(int)
    h, w = a.shape[:2]
    dark = (a[..., 3] > 128) & (a[..., :3].mean(-1) < 110)
    c0, c1 = int(w * 0.35), int(w * 0.65)
    return next((y for y in range(10, h) if dark[y, c0:c1].any()), None)


def shoulder_edges(im):
    """Mép trái/phải của dải áo trên cùng (vai), tính từ giữa ảnh."""
    al = np.asarray(im)[..., 3] > 128
    xs = np.nonzero(al[6])[0]
    return xs.min() - im.width / 2, xs.max() + 1 - im.width / 2


def classify(files):
    parts = []
    for f in files:
        im = Image.open(f).convert('RGBA')
        # Ảnh xuất ở @2x/@4x: thu về cùng tỉ lệ với các bộ khác.
        for tag, k in (('@4x', 4), ('@3x', 3), ('@2x', 2)):
            if tag in os.path.basename(f):
                im = im.resize((round(im.width / k), round(im.height / k)), Image.LANCZOS)
                break
        if im.height > 250:  # ảnh mẫu đã ghép sẵn, không phải một phần
            continue
        parts.append((f, im))
    roles = {}
    head = max(parts, key=lambda p: p[1].width * p[1].height)
    roles['head'] = head
    rest = [p for p in parts if p is not head]
    body = max(rest, key=lambda p: p[1].width * p[1].height)
    w, h = body[1].size
    kind = 'skirt' if w >= 140 else ('suit' if h >= 120 else 'shirt')
    roles['body'] = body
    rest = [p for p in rest if p is not body]
    # Áo mặc ngoài bộ đồ liền thân (ví dụ gà bộ 3).
    tops = [p for p in rest if p[1].width >= 100]
    if tops:
        roles['top'] = tops[0]
        rest = [p for p in rest if p is not tops[0]]
    legs =[p for p in rest if p[1].height <= 56 and p[1].width <= 48]
    arms = [p for p in rest if p not in legs]
    if len(legs) == 2:
        legs.sort(key=lambda p: -p[1].width * p[1].height)
        roles['leg_left'], roles['leg_right'] = legs
    for a in arms:
        al = np.asarray(a[1])[..., 3] > 128
        hh = al.shape[0]
        def cx(rows):
            ys, xs = np.nonzero(rows)
            return xs.mean() if len(xs) else 0
        top, bottom = cx(al[: hh // 3]), cx(al[2 * hh // 3:])
        roles['arm_left' if top > bottom else 'arm_right'] = a
    return kind, roles


def main():
    os.makedirs(DST, exist_ok=True)
    rigs = []
    for folder in sorted(os.listdir(SRC)):
        path = os.path.join(SRC, folder)
        if not os.path.isdir(path):
            continue
        files = [os.path.join(path, f) for f in os.listdir(path) if f.lower().endswith('.png')]
        kind, roles = classify(files)
        rid = folder.replace('-', '_')
        if kind == 'shirt':
            feet = max(LEG_L_CENTER[1] + roles['leg_left'][1].height / 2 if 'leg_left' in roles else 0,
                       LEG_R_CENTER[1] + roles['leg_right'][1].height / 2 if 'leg_right' in roles else 0)
        else:
            feet = roles['body'][1].height
        body_im = roles['body'][1]
        # Thân liền cao: khung chuẩn đặt đầu ở mép trên nên cổ trông quá dài; hạ đầu và tay xuống sát cà vạt.
        drop = 0.0
        # Bộ có áo khoác ngoài (gà 3) giữ nguyên: hạ đầu sẽ che mất nơ ở cổ áo.
        if kind == 'suit' and body_im.height >= SUIT_TALL_HEIGHT and 'top' not in roles:
            tie = tie_top(body_im)
            if tie is not None:
                drop = max(0.0, tie + SUIT_TIE_OVERLAP - HEAD_BOTTOM[1])
        out = []
        for role, (f, im) in roles.items():
            w, h = im.size
            if role == 'head':
                pivot = (0.5, 0.0); joint = (HEAD_BOTTOM[0], HEAD_BOTTOM[1] + drop)
            elif role in ('body', 'top'):
                pivot = (0.5, 1.0); joint = (0.0, 0.0)
            elif kind == 'skirt' and role.startswith('arm'):
                # Tay hanbok (tay áo + bàn tay) treo ở hai vai, đối xứng theo dải áo trên cùng.
                pivot = {'arm_left': (0.75, 0.88), 'arm_right': (0.25, 0.88)}[role]
                left, right = shoulder_edges(body_im)
                x = left + SKIRT_SHOULDER_INSET if role == 'arm_left' else right - SKIRT_SHOULDER_INSET
                joint = (x, SKIRT_SHOULDER_DROP)
            else:
                skirt = kind == 'skirt'
                center = {'arm_left': SKIRT_ARM_L_CENTER if skirt else ARM_L_CENTER,
                          'arm_right': SKIRT_ARM_R_CENTER if skirt else ARM_R_CENTER,
                          'leg_left': LEG_L_CENTER, 'leg_right': LEG_R_CENTER}[role]
                pivot = {'arm_left': (0.75, 0.88), 'arm_right': (0.25, 0.88)}.get(role, (0.5, 0.92))
                joint = (center[0] + (pivot[0] - 0.5) * w, center[1] - (pivot[1] - 0.5) * h)
                if role.startswith('arm'):
                    joint = (joint[0], joint[1] + drop)
            name = f'{rid}_{role}.png'
            small = im.resize((max(1, round(w * SCALE)), max(1, round(h * SCALE))), Image.LANCZOS)
            small.save(os.path.join(DST, name))
            out.append({'role': role, 'file': name, 'pivot': pivot,
                        'pos': (round(joint[0] / PPU_SRC, 4), round((feet - joint[1]) / PPU_SRC, 4)),
                        'order': ORDER[role]})
        # Thư mục npc-banhang*: nhân viên quầy (người bán, người chào hàng), không đi lẫn trong khách.
        rigs.append({'id': rid, 'kind': kind, 'staff': 'banhang' in folder, 'parts': out})
        print(rid, kind, sorted(roles.keys()))
    with open(os.path.join(DST, 'npc_rigs.json'), 'w', encoding='utf-8') as fh:
        json.dump({'ppu': PPU_SRC * SCALE, 'rigs': rigs}, fh, indent=1)


main()
