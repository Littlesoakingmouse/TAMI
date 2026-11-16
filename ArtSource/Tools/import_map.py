"""
Đưa texture nền map của hoạ sĩ (ArtSource/Sheets/map_*) vào Assets/_Game/Art/Map.

  py -3 -I import_map.py

- map_brick.webp: gạch vỉa hè (lát kín từ đáy map tới lớp cỏ), 8 viên mỗi hàng.
- map_road.jpg: đá lát đường đi bộ, lưới 8x8 viên.
Cả hai là ảnh lát liền mạch; chỉ thu nhỏ (và xoá dấu lấp lánh còn sót trên ảnh đá).
Tỉ lệ (điểm ảnh mỗi đơn vị) đặt trong file .meta / menu "Tami/Cập nhật hình tạm và dữ liệu":
gạch 133.33 (mỗi viên rộng 0,6 đơn vị), đá 100.4 (lòng đường cao 2,55 đơn vị vừa đúng 4 hàng đá).
"""
import os
from PIL import Image

HERE = os.path.dirname(__file__)
SHEETS = os.path.join(HERE, '..', 'Sheets')
OUT = os.path.join(HERE, '..', '..', 'Assets', '_Game', 'Art', 'Map')

BRICK_PX = 640
ROAD_PX = 512


def main():
    os.makedirs(OUT, exist_ok=True)

    brick = Image.open(os.path.join(SHEETS, 'map_brick.webp')).convert('RGB')
    brick.resize((BRICK_PX, BRICK_PX), Image.LANCZOS).save(os.path.join(OUT, 'ground_brick.png'))

    road = Image.open(os.path.join(SHEETS, 'map_road.jpg')).convert('RGB')
    # Dấu lấp lánh ở chỗ giao mạch vữa (896, 896): chép đè một chỗ giao mạch sạch (640, 640) cùng lưới 128.
    patch = road.crop((640 - 48, 640 - 48, 640 + 48, 640 + 48))
    road.paste(patch, (896 - 48, 896 - 48))
    road.resize((ROAD_PX, ROAD_PX), Image.LANCZOS).save(os.path.join(OUT, 'road_tile.png'))
    print('map textures ->', os.path.abspath(OUT))


main()
