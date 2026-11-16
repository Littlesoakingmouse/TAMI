using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>Thứ tự cũng là thứ tự mở khoá và thứ tự đặt trong khu, từ trái sang phải.</summary>
    public enum FolkGameKind { DapNieu, DaCau, Tuho }

    /// <summary>
    /// Trò chơi dân gian ở khu bên dưới. Mở khu xong, từng trò phải mở riêng theo thứ tự; mở xong một người trong phố
    /// kể về ý nghĩa của trò, lần đầu chơi thì được hướng dẫn cách chơi. Khách ghé chơi trò đã mở rồi trả phí vào
    /// thẳng tiền của người chơi; người chơi chạm vào để chơi mini-game nhận xu và tăng thu nhập, rồi trò chơi hồi chiêu.
    /// </summary>
    public class FolkGameStation : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        public FolkGameKind Kind { get; private set; }
        /// <summary>Vị trí trong khu, cũng là thứ tự mở khoá.</summary>
        public int Index { get; private set; }
        public bool Unlocked { get; private set; }
        public string Title => Titles[(int)Kind];
        public Origin Origin => Kind == FolkGameKind.Tuho ? Origin.KR : Origin.VN;
        /// <summary>Lời kể ý nghĩa trò chơi, nói ngay sau khi mở trò.</summary>
        public DialogueLine[] Story => Stories[(int)Kind];
        /// <summary>Lời hướng dẫn cách chơi, nói trước lần chơi đầu tiên.</summary>
        public DialogueLine[] Tutorial => Tutorials[(int)Kind];
        public Sprite Icon => sr.sprite;
        public float CooldownUntil { get; private set; } = -1f;
        public bool Ready => Time.time >= CooldownUntil;
        public float CooldownLeft => Mathf.Max(0f, CooldownUntil - Time.time);

        static readonly string[] Titles = { "Đập niêu", "Đá cầu", "Ném tên vào bình Tuho" };

        static DialogueLine L(string speaker, string text) => new DialogueLine(speaker, text);

        static readonly DialogueLine[][] Stories =
        {
            new[]
            {
                L("ongba", "Đập niêu đây! Hồi xưa hội làng nào, Tết nhà nào cũng có trò này."),
                L("ongba", "Người chơi bị bịt mắt, cầm gậy, nghe cả xóm hô mà dò tới đập vỡ chiếc niêu đất. Trong niêu thường có quà."),
                L("ongba", "Niêu vỡ là xua hết điều xui, đón may mắn cho năm mới. Tiếng reo hò cổ vũ làm cả xóm gần nhau hơn đấy!"),
            },
            new[]
            {
                L("mayor", "Đá cầu là trò chơi có từ lâu đời, từ sân đình tới sân trường đâu cũng thấy."),
                L("mayor", "Dùng chân tâng quả cầu lông, giữ cho cầu không chạm đất. Rèn sự khéo léo, dẻo dai và bền bỉ."),
                L("mayor", "Ngày nay đá cầu đã thành môn thể thao thi đấu hẳn hoi. Cháu thử xem tâng được bao nhiêu quả!"),
            },
            new[]
            {
                L("minji", "Đây là Tuho, trò ném tên vào bình của Hàn Quốc, có từ thời Joseon."),
                L("minji", "Ban đầu là trò của cung đình và giới quý tộc, sau lan ra dân gian, nhà nào cũng chơi dịp Tết Seollal."),
                L("minji", "Người xưa xem Tuho là cách tu dưỡng: giữ tâm tĩnh, tập trung, thắng thua đều nhã nhặn. Bạn thử nhé!"),
            },
        };

        static readonly DialogueLine[][] Tutorials =
        {
            new[]
            {
                L("ongba", "Cách chơi nhé: lúc đầu cháu được nhìn ba chiếc niêu, nhớ kỹ niêu có chữ QUÀ TO."),
                L("ongba", "Rồi cháu bị bịt mắt, không thấy gì cả. Bấm nút TRÁI, PHẢI để bước. Chóng mặt nên người cứ lệch đi đấy."),
                L("ongba", "Nghe cả xóm hô hướng đi, nhưng coi chừng thằng Cu Tí hay hô sai. Càng tới gần niêu quà, cả xóm reo càng to."),
                L("ongba", "Để ý thanh vàng \"Tiếng reo của cả xóm\" giữa màn hình nhé: thanh càng đầy là cháu càng gần niêu quà, thanh vơi đi là đang đi xa dần."),
                L("ongba", "Thanh gần đầy thì bấm ĐẬP! Có 3 lần đập, tránh niêu nước kẻo ướt hết!"),
            },
            new[]
            {
                L("mayor", "Quả cầu sẽ rơi xuống. Khi nó xuống tới vùng xanh dưới chân, cháu chạm vào quả cầu để tâng lên."),
                L("mayor", "Chạm lệch sang bên nào thì cầu bay về bên kia, nhớ giữ cầu ở giữa. Đá đúng dải xanh đậm được điểm gấp đôi."),
                L("mayor", "Lúc đầu cầu rơi chậm, nhưng càng tâng nhiều, chơi càng lâu thì cầu rơi càng nhanh. Có 2 lượt rơi, cố giữ cầu tới hết giờ nhé!"),
            },
            new[]
            {
                L("minji", "Chạm vào sân, kéo ngược về phía sau rồi thả ra, giống kéo ná vậy. Đường chấm sẽ cho bạn thấy mũi tên bay đi đâu."),
                L("minji", "Kéo càng xa ném càng mạnh. Cho đường chấm rơi đúng vào miệng bình rồi thả tay."),
                L("minji", "Vào miệng bình 2 điểm, ngay giữa miệng 3 điểm, lọt tai bình hai bên cũng 3 điểm. Nhớ nhìn cờ gió nhé!"),
            },
        };

        static readonly Color NamePlateInk = new Color(0.36f, 0.22f, 0.18f);

        SpriteRenderer sr;
        TextMesh label;
        Vector2[] spots;
        Npc[] players;
        float labelTimer, artBaseY;

        public static FolkGameStation Create(Transform parent, FolkGameKind kind, int index, Vector2 position)
        {
            var g = Game.I;
            var root = new GameObject("FolkGame_" + kind);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var station = root.AddComponent<FolkGameStation>();
            station.Kind = kind;
            station.Index = index;

            Sprite art;
            float scale;
            switch (kind)
            {
                case FolkGameKind.DapNieu:
                    art = DecorSprite("hu_sanh");
                    scale = 1.4f;
                    station.spots = new[] { new Vector2(-1.0f, -0.05f), new Vector2(1.0f, -0.05f) };
                    break;
                case FolkGameKind.DaCau:
                    art = g.art.folkDaCau;
                    scale = 0.55f;
                    station.spots = new[] { new Vector2(-1.1f, -0.05f), new Vector2(1.1f, -0.05f) };
                    break;
                default:
                    art = g.art.folkTuho;
                    scale = 0.95f;
                    station.spots = new[] { new Vector2(-1.3f, -0.05f), new Vector2(1.3f, -0.05f) };
                    break;
            }
            station.sr = g.NewSprite(root.transform, art, 0, "Art");
            station.sr.transform.localScale = Vector3.one * scale;
            station.players = new Npc[station.spots.Length];

            // Biển tên dưới chân trò chơi.
            var plate = g.NewNamePlate(root.transform, new Vector2(0f, -0.5f), new Vector2(3.5f, 0.72f), 2);
            station.label = g.NewLabel(root.transform, "", 3, 0.04f, plate.drawMode == SpriteDrawMode.Sliced ? NamePlateInk : new Color(1f, 0.93f, 0.75f), "Label");
            station.label.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            var col = root.AddComponent<BoxCollider2D>();
            var b = station.sr.bounds;
            col.size = new Vector2(Mathf.Max(2f, b.size.x), Mathf.Max(1.6f, b.size.y) + 0.6f);
            col.offset = new Vector2(0f, b.size.y * 0.5f - 0.3f);
            root.AddComponent<YSort>().isStatic = true;
            station.SetLocked();
            return station;
        }

        static Sprite DecorSprite(string id)
        {
            var data = Game.I.config.FindDecoration(id);
            return data != null ? data.sprite : null;
        }

        /// <summary>Trò chưa mở: tô tối như bóng đen, khách không vào chơi.</summary>
        public void SetLocked()
        {
            Unlocked = false;
            sr.color = new Color(0.32f, 0.29f, 0.35f, 0.9f);
            RefreshLabel();
        }

        public void Unlock()
        {
            Unlocked = true;
            sr.color = Color.white;
            RefreshLabel();
        }

        public bool TryReserve(Npc npc, out int spot)
        {
            if (Unlocked)
            {
                for (spot = 0; spot < players.Length; spot++)
                {
                    if (players[spot] != null) continue;
                    players[spot] = npc;
                    return true;
                }
            }
            spot = -1;
            return false;
        }

        public Vector2 SpotPoint(int spot) => (Vector2)transform.position + spots[spot];

        /// <summary>Khách bên trái quay sang phải (nhìn vào trò chơi) và ngược lại.</summary>
        public bool SpotFacesRight(int spot) => spots[spot].x < 0f;

        public void Free(int spot)
        {
            if (spot >= 0 && spot < players.Length) players[spot] = null;
        }

        /// <summary>Khách chơi xong: trả phí vào tiền của người chơi.</summary>
        public void FinishPlay(int spot)
        {
            Free(spot);
            var g = Game.I;
            double fee = System.Math.Max(g.config.folkGameFeeMin, g.Street.IncomePerMinute * g.config.folkGameFeeMinutes);
            g.Economy.Add(fee);
            g.Data.stats.coinsCollected += fee;
            g.Data.stats.npcGamesPlayed++;
            FloatingText.Spawn(transform.position + Vector3.up * 1.9f, "+" + NumberFormat.Short(fee), new Color(1f, 0.85f, 0.25f), 0.05f);
            g.Audio.Play(Sfx.Coin);
        }

        /// <summary>Người chơi vừa chơi xong mini-game: trò chơi hồi chiêu.</summary>
        public void OnPlayerPlayed()
        {
            CooldownUntil = Time.time + Game.I.config.folkGameCooldown;
            RefreshLabel();
        }

        void Update()
        {
            // Đá cầu: có khách chơi thì quả cầu tâng lên xuống giữa hai người.
            if (Kind == FolkGameKind.DaCau)
            {
                bool busy = players[0] != null || players[1] != null;
                float y = busy ? 0.4f + Mathf.Abs(Mathf.Sin(Time.time * 2.6f)) * 1.1f : 0f;
                artBaseY = Mathf.MoveTowards(artBaseY, y, Time.deltaTime * 4f);
                sr.transform.localPosition = new Vector3(0f, artBaseY, 0f);
            }
            labelTimer -= Time.unscaledDeltaTime;
            if (labelTimer > 0f) return;
            labelTimer = 0.5f;
            RefreshLabel();
        }

        void RefreshLabel()
        {
            string line;
            if (!Unlocked)
            {
                var street = Game.I.Street;
                line = street.CanUnlockFolkGame(Index) ? "Mở: " + NumberFormat.Short(street.FolkGameCost(Index)) + " xu" : "Mở sau";
            }
            else line = Ready ? "Chạm để chơi" : "Chơi lại sau " + NumberFormat.Clock(CooldownLeft);
            label.text = Title + "\n<size=40>" + line + "</size>";
        }

        public Vector3 InteractPoint => transform.position + new Vector3(0f, -1.1f, 0f);

        public void OnPointerClick(PointerEventData eventData) => Interaction.Request(this);

        public void Interact()
        {
            var g = Game.I;
            if (!Unlocked)
            {
                if (g.Street.CanUnlockFolkGame(Index)) g.UI.OpenFolkUnlockPanel(this);
                else g.UI.Toast("Hãy mở trò " + g.Street.FolkGames[g.Street.FolkGamesUnlocked].Title + " trước");
                return;
            }
            if (!Ready)
            {
                g.UI.Toast(Title + " đang nghỉ, chơi lại sau " + NumberFormat.Clock(CooldownLeft));
                return;
            }
            g.UI.StartFolkGame(this);
        }
    }
}
