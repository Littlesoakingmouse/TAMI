using UnityEngine;

namespace Tami
{
    /// <summary>Một món trang trí (GDD mục 4.10, 6.3). Giá Ngọc lớn hơn 0 thì chỉ mua bằng Ngọc.</summary>
    [CreateAssetMenu(menuName = "Tami/Decoration", fileName = "Deco_")]
    public class DecorationData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Origin origin;
        public double coinCost;
        public int gemCost;
        public int attraction = 1;
        public Sprite sprite;
    }
}
