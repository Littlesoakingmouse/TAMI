"""
Thu gọn một khung 9-slice: giữ 4 góc (corner điểm ảnh) và một dải giữa hẹp, bỏ phần giữa lặp lại.
Khung gọn hiển thị y hệt khi kéo to nhỏ trong Unity (Sprite Border = corner ở cả 4 cạnh).

  py -3 make_9slice.py <ảnh gốc> <ảnh ra> [corner=80] [mid=8]
"""
import sys
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
corner = int(sys.argv[3]) if len(sys.argv) > 3 else 80
mid = int(sys.argv[4]) if len(sys.argv) > 4 else 8

im = Image.open(src).convert('RGBA')
W, H = im.size
mx, my = W // 2 - mid // 2, H // 2 - mid // 2
xs = [(0, corner), (mx, mx + mid), (W - corner, W)]
ys = [(0, corner), (my, my + mid), (H - corner, H)]
out = Image.new('RGBA', (corner * 2 + mid, corner * 2 + mid))
oy = 0
for y0, y1 in ys:
    ox = 0
    for x0, x1 in xs:
        out.paste(im.crop((x0, y0, x1, y1)), (ox, oy))
        ox += x1 - x0
    oy += y1 - y0
out.save(dst)
print(dst, out.size, 'border', corner)
