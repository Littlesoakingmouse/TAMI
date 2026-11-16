using UnityEngine;

namespace Tami
{
    /// <summary>Một loại khách (GDD mục 4.5).</summary>
    [CreateAssetMenu(menuName = "Tami/Npc Type", fileName = "Npc_")]
    public class NpcTypeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public NpcKind kind;

        public float spawnWeight = 1f;
        public int minStalls = 1;
        public int maxStalls = 5;
        public float payMultiplier = 1f;
        public int groupSizeMin = 1;
        public int groupSizeMax = 1;

        [Tooltip("Số đoạn phố tối thiểu đã mở để loại khách này xuất hiện")]
        public int unlockSegment = 1;

        [Tooltip("Biểu tượng trên đầu khách (để trống với khách thường)")]
        public Sprite marker;
    }
}
