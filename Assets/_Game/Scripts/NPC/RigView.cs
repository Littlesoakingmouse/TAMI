using UnityEngine;

namespace Tami
{
    /// <summary>Dựng hình tĩnh một nhân vật ghép nhiều phần (dùng cho người bán sau quầy).</summary>
    public static class RigView
    {
        /// <param name="orderOffset">Cộng vào thứ tự vẽ của từng phần.</param>
        public static Transform Build(Transform parent, NpcRig rig, int orderOffset, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            foreach (var part in rig.parts)
            {
                if (part.sprite == null) continue;
                var sr = Game.I.NewSprite(root, part.sprite, part.order + orderOffset, part.role);
                sr.transform.localPosition = part.position;
            }
            return root;
        }
    }
}
