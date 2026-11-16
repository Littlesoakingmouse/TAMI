"""
Tán lá viền trên/dưới map và cổng chào mới, xuất vào Assets/_Game/Art/Map.

  py -3 -I make_canopy_gate.py

Nguồn: ArtSource/Sheets/ui2/canopy.webp (dải tán lá, nền trong suốt), ArtSource/Sheets/ui2/gate.webp (cổng chào).
- canopy.png : dải tán lá rộng 1024 điểm ảnh, neo ở mép trên. Game đặt các tấm cạnh nhau, tấm xen kẽ lật ngang
               (mép tiếp giáp trùng khít nên không lộ đường nối); viền dưới map dùng chính hình này lật ngược.
- gate_arch.png: cổng chào "Hội chợ Việt - Hàn" rộng 960 điểm ảnh, neo ở chân cột.
"""
import os, sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from cut_stall_sheets import ASSETS

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.join(HERE, '..', 'Sheets', 'ui2')


def main():
    canopy = Image.open(os.path.join(SHEETS, 'canopy.webp')).convert('RGBA')
    canopy = canopy.crop(canopy.getchannel('A').getbbox())
    canopy = canopy.resize((1024, round(canopy.height * 1024 / canopy.width)), Image.LANCZOS)
    canopy.save(os.path.join(ASSETS, 'Map', 'canopy.png'))

    gate = Image.open(os.path.join(SHEETS, 'gate.webp')).convert('RGBA')
    gate = gate.crop(gate.getchannel('A').getbbox())
    gate = gate.resize((960, round(gate.height * 960 / gate.width)), Image.LANCZOS)
    gate.save(os.path.join(ASSETS, 'Map', 'gate_arch.png'))
    print('canopy', canopy.size, 'gate', gate.size)


main()
