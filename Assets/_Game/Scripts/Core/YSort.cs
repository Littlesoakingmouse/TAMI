using UnityEngine;

namespace Tami
{
    /// <summary>Vật ở thấp hơn trên màn hình được vẽ đè lên vật ở cao hơn.</summary>
    public class YSort : MonoBehaviour
    {
        const float Factor = 20f;

        public bool isStatic;

        Renderer[] renderers;
        int[] baseOrders;
        int applied = int.MinValue;

        void Awake()
        {
            if (renderers == null) Rebase();
        }

        /// <summary>
        /// Quét lại các phần con sau khi thêm renderer mới. Phần cũ giữ thứ tự gốc đã biết (sortingOrder
        /// hiện tại của chúng đã cộng độ cao); phần mới lấy sortingOrder đang đặt làm thứ tự gốc.
        /// </summary>
        public void Rebase()
        {
            var oldRenderers = renderers;
            var oldBases = baseOrders;
            renderers = GetComponentsInChildren<Renderer>(true);
            baseOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                int old = oldRenderers == null ? -1 : System.Array.IndexOf(oldRenderers, renderers[i]);
                baseOrders[i] = old >= 0 ? oldBases[old] : renderers[i].sortingOrder;
            }
            applied = int.MinValue;
            Apply();
        }

        /// <summary>Đặt thứ tự vẽ gốc cho một phần con mà không phải quét lại cả object.</summary>
        public void SetBaseOrder(Renderer r, int order)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == r) baseOrders[i] = order;
            }
            applied = int.MinValue;
        }

        void LateUpdate()
        {
            if (!isStatic) Apply();
        }

        public void Apply()
        {
            int order = Mathf.RoundToInt(-transform.position.y * Factor);
            // Chỉ gán khi đổi hàng: gán sortingOrder mỗi khung làm renderer bị đánh dấu thay đổi.
            if (order == applied) return;
            applied = order;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].sortingOrder = order + baseOrders[i];
            }
        }
    }
}
