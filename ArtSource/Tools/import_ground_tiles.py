"""
Nhập bộ nền mới của hoạ sĩ (TAMIGAME/cay-co/duong-co) vào Assets/_Game/Art/Map, thu về 1024 điểm ảnh mỗi cạnh.

  py -3 -I import_ground_tiles.py

- road_tile.png    <- "duong di bo.png": gạch xám xếp so le, 8 hàng mỗi ô. Ô = 5,1 đơn vị (ppu 200,784) nên mỗi hàng
                      cao đúng 0,6375 = một "viên" của lưới đường (đường cao 4 hàng, lối đi dọc khớp hàng với hai đường).
- ground_brick.png <- "via he (2).png": vỉa hè gạch hoa hồng 4x4 viên. Ô = 4,8 đơn vị (ppu 213,333).
- grass_tile.png   <- "co.png": cỏ (bỏ viền trong suốt 3 điểm ảnh ở mép phải, mép dưới). Ô = 6 đơn vị (ppu 170,667).
"""
import os, sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, '..', '..', '..', 'cay-co', 'duong-co')
SIZE = 1024


def tile(src, dst):
    im = Image.open(os.path.join(SRC, src)).convert('RGBA')
    box = im.getchannel('A').getbbox()
    im = im.crop(box).resize((SIZE, SIZE), Image.LANCZOS)
    im.putalpha(255)
    im.save(os.path.join(ASSETS, 'Map', dst))
    print(dst, box)


tile('duong di bo.png', 'road_tile.png')
tile('via he (2).png', 'ground_brick.png')
tile('co.png', 'grass_tile.png')
