using UnityEngine;

namespace Tami
{
    /// <summary>Các sprite dùng chung. Hình tạm do menu Tami/Build MVP sinh ra, thay bằng art thật sau.</summary>
    [CreateAssetMenu(menuName = "Tami/Game Art", fileName = "GameArt")]
    public class GameArt : ScriptableObject
    {
        public Material spriteMaterial;
        [Tooltip("Font của toàn bộ chữ trong game (giao diện và chữ trên phố)")]
        public Font font;

        [Header("Con phố")]
        public Sprite road;
        public Sprite ground;
        [Tooltip("Lớp cỏ phía trên dãy quầy (cây thêm sau)")]
        public Sprite grass;
        public Sprite sky;
        [Tooltip("Các cây của hàng cây trên dải cỏ phía sau dãy quầy (pivot ở gốc cây)")]
        public Sprite[] backTrees;
        public Sprite slotEmpty;
        [Tooltip("Cổng chào đầu phố (Art/Map/gate_arch.png, chữ vẽ sẵn trên biển)")]
        public Sprite gate;
        [Tooltip("Dải tán lá viền mép trên và mép dưới map (neo ở mép trên; viền dưới lật ngược)")]
        public Sprite canopy;
        public Sprite expandSign;
        public Sprite stage;

        [Header("Nhân vật")]
        [Tooltip("Khách ghép từ nhiều phần (Art/Npc). Trống thì dùng hình tạm npcBodies.")]
        public NpcRig[] npcRigs;
        [Tooltip("Nhân viên quầy (người bán, người chào hàng), từ thư mục npc-banhang*")]
        public NpcRig[] staffRigs;
        public Sprite[] npcBodies;
        public Sprite manager;
        [Tooltip("Các phần hình nhân vật chính (Art/Player), pivot đặt ở khớp nối")]
        public Sprite playerHead, playerBody, playerArmLeft, playerArmRight, playerLegLeft, playerLegRight;
        [Tooltip("Vòng sáng dưới chân nhân vật chính")]
        public Sprite playerRing;
        public Sprite portraitMayor;
        public Sprite portraitMinji;
        public Sprite portraitOngBa;
        public Sprite portraitPlayer;

        [Header("Biểu tượng")]
        public Sprite coin;
        public Sprite gem;
        public Sprite moneyBag;
        [Tooltip("Túi tiền khi quỹ quầy đã đầy")]
        public Sprite moneyBagFull;
        public Sprite bubbleSad;
        public Sprite bubbleBuy;
        public Sprite arrow;
        public Sprite iconTrophy;
        public Sprite iconSettings;
        public Sprite iconGift;
        public Sprite tabStreet;
        public Sprite tabManager;
        public Sprite tabDecor;
        public Sprite tabShop;

        [Tooltip("Ô vuông trắng 1x1 đơn vị, pivot ở cạnh trái")]
        public Sprite white;

        [Header("Khung giao diện (9-slice, tự co giãn theo chữ)")]
        [Tooltip("Khung hộp thoại cốt truyện")]
        public Sprite dialogBox;
        [Tooltip("Khung giao diện 9-slice (ArtSource/Tools/make_ui_frames.py): bảng, nút (nhuộm màu), dòng danh sách, núm tròn")]
        public Sprite uiPanel, uiButton, uiRow, uiKnob;
        [Tooltip("Bảng tên 9-slice (tên người nói, biển tên trò chơi) và khung thông báo 9-slice; nền trắng để nhuộm")]
        public Sprite frameName, frameNotice;
        [Header("Khu ăn uống (Art/Dining, vẽ bằng ArtSource/Tools/make_dining_art.py)")]
        [Tooltip("Bàn nhựa phủ khăn caro, ghế nhựa đỏ (kiểu Việt)")]
        public Sprite diningTableVN, diningChairVN;
        [Tooltip("Bàn gỗ phủ khăn đỏ, ghế gỗ (kiểu Hàn)")]
        public Sprite diningTableKR, diningChairKR;
        [Tooltip("Góc ghế nhựa vỉa hè: bàn thấp không khăn, ghế đẩu đỏ và xanh (khách ngồi lộ chân)")]
        public Sprite diningTableLow, stoolRed, stoolBlue;
        [Tooltip("Hàng rào lát lặp dọc mép ngoài con đường (neo ở đáy) và cột gỗ ở hai đầu mỗi đoạn rào")]
        public Sprite fence, fencePost;

        [Header("Trò chơi dân gian")]
        [Tooltip("Quả cầu lông (Đá cầu) và bình tuho cắm tên (Ném tên vào bình); Đập niêu, bập bênh dùng hình đồ trang trí")]
        public Sprite folkDaCau, folkTuho;

        [Header("Âm thanh")]
        [Tooltip("Nhạc nền, phát lần lượt theo thứ tự xáo (Assets/_Game/Audio/Music)")]
        public AudioClip[] musicTracks;

        [Tooltip("Ô thoại trên đầu nhân vật (9-slice)")]
        public Sprite speechBubble;
        [Tooltip("Đuôi ô thoại, pivot ở cạnh trên")]
        public Sprite speechTail;
    }

    /// <summary>Một bộ khách: các phần hình (đầu, thân, áo, tay, chân), mỗi phần có pivot ở khớp nối.</summary>
    [System.Serializable]
    public class NpcRig
    {
        public string id;
        public NpcRigPart[] parts;

        /// <summary>Loài của bộ khách: id bỏ số thứ tự bộ đồ ở cuối (npc_ga3 → npc_ga).</summary>
        public string Species => id.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
    }

    [System.Serializable]
    public class NpcRigPart
    {
        [Tooltip("head, body, top, arm_left, arm_right, leg_left, leg_right")]
        public string role;
        public Sprite sprite;
        [Tooltip("Vị trí khớp so với chân nhân vật (đơn vị thế giới)")]
        public Vector2 position;
        public int order;
    }
}
