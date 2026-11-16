using UnityEngine;

namespace Tami
{
    /// <summary>Một tiết mục biểu diễn (GDD mục 4.9, 6.2).</summary>
    [CreateAssetMenu(menuName = "Tami/Performer", fileName = "Performer_")]
    public class PerformerData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Origin origin;
        public double coinCost;
        [Tooltip("Thời gian hiệu lực (giây)")]
        public float duration = 60f;
        [TextArea] public string description;
        public Sprite sprite;
    }
}
