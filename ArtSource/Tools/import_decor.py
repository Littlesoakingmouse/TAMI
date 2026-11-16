"""
Cắt 12 sheet đồ trang trí của hoạ sĩ (thư mục trangtri, mỗi sheet 4 món) vào Assets/_Game/Art/Decor.

  py -3 -I import_decor.py [thư_mục_ra]

Mỗi sheet đọc theo hàng: trên trái, trên phải, dưới trái, dưới phải. Chiều cao là đơn vị thế giới
(nhân vật chính cao khoảng 1,9). Tên, giá, độ hấp dẫn nằm ở DecorTable trong Editor/TamiBuilder.cs.
Chạy lại khi hoạ sĩ cập nhật sheet, sau đó chạy menu "Tami/Cập nhật hình tạm và dữ liệu" trong Unity.
"""
import glob, os, re, sys

sys.path.insert(0, os.path.dirname(__file__))
from cut_auto import cut_decor, cut_decor_single
from cut_stall_sheets import ASSETS, load_manifest, save_manifest

SRC = r'E:\unity_pj\TAMIGAME\trangtri'

# Số trong tên file "rê (N).png" -> 4 món (id, chiều cao).
SHEETS = {
    6: [('den_long', 1.1), ('non_la', 1.0), ('hu_sanh', 0.9), ('cay_quat', 1.4)],
    7: [('quang_ganh', 0.9), ('chum_nuoc', 1.0), ('coi_da', 0.9), ('dun_rom', 1.3)],
    8: [('mam_ngu_qua', 0.9), ('banh_chung', 0.8), ('den_ong_sao', 1.1), ('cay_dao', 1.2)],
    9: [('thap_co', 2.4), ('thuy_dinh', 1.9), ('trong_dong', 1.1), ('nghe_da', 1.2)],
    10: [('xe_banh_mi', 1.8), ('sap_hoa_qua', 1.8), ('ganh_hang_rong', 1.1), ('xich_lo', 1.5)],
    11: [('o_che_nang', 2.2), ('ban_ghe_nhua', 0.9), ('xe_tra_da', 1.2), ('bo_tra_da', 0.8)],
    12: [('den_long_xanh_do', 1.2), ('onggi', 1.1), ('quat_buchae', 0.9), ('binh_men_ngoc', 1.0)],
    13: [('cong_hanok', 2.2), ('jangseung', 1.8), ('thap_da', 1.2), ('jeongja', 2.1)],
    14: [('pojangmacha', 1.8), ('may_ban_hang', 1.9), ('jangdokdae', 0.9), ('ban_tra_thap', 0.7)],
    15: [('cay_anh_dao', 2.6), ('cay_phong', 2.6), ('cay_thong', 2.0), ('nguoi_tuyet', 1.4)],
    16: [('tram_xe_buyt', 2.0), ('cua_hang_tien_loi', 2.4), ('bang_hieu', 2.0), ('xe_bungeoppang', 1.9)],
    17: [('neolttwigi', 0.7), ('chuoi_den_long', 2.0), ('trong_buk', 1.3), ('ban_tra', 0.9)],
}


# Cây xanh lẻ (thư mục cay-co/cay, mỗi file một cây): số trong "rê (N).png" -> (id, chiều cao).
# Không dùng rê (9) (đã bỏ khỏi hàng cây và đồ trang trí).
# Cũng là hình hàng cây phía sau dãy quầy (GameArt.backTrees).
TREES_SRC = r'E:\unity_pj\TAMIGAME\cay-co\cay'
TREES = {
    6: ('cay_bang', 2.6),
    7: ('cay_sau', 2.6),
    8: ('cay_xa_cu', 2.6),
    10: ('cay_sao', 2.6),
    11: ('cay_dau', 2.6),
}


def numbered(folder):
    """Tên file có dấu (rê) nên tìm theo số trong ngoặc."""
    return {int(m.group(1)): f for f in glob.glob(os.path.join(folder, '*.png'))
            if (m := re.search(r'\((\d+)\)\.png$', f))}


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ASSETS, 'Decor')
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, 'decor_art.json')
    manifest = load_manifest(path)
    files = numbered(SRC)
    for n, items in SHEETS.items():
        cut_decor(files[n], items, out_dir, manifest)
    trees = numbered(TREES_SRC)
    for n, (item_id, height) in TREES.items():
        cut_decor_single(trees[n], item_id, height, out_dir, manifest)
    save_manifest(path, manifest)


main()
