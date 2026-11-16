using UnityEngine;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>
    /// Trò chơi dân gian: bảng giới thiệu ý nghĩa (cũng là bảng mở khoá từng trò) và mini-game riêng cho mỗi trò.
    /// Chơi xong được thưởng xu theo điểm, đạt từ nửa điểm trở lên thì tăng thu nhập một lúc.
    /// </summary>
    public partial class UIManager
    {
        FolkView folkGame;

        static string OriginTag(Origin origin) =>
            origin == Origin.VN ? "<color=#E0574A><b>Việt Nam</b></color>" : "<color=#3D8ED8><b>Hàn Quốc</b></color>";

        /// <summary>Bảng mở trò chơi mới; mở xong một người trong phố kể về ý nghĩa của trò.</summary>
        public void OpenFolkUnlockPanel(FolkGameStation station)
        {
            var g = Game.I;
            var street = g.Street;
            if (station.Unlocked || !street.CanUnlockFolkGame(station.Index)) return;
            int index = station.Index;
            double cost = street.FolkGameCost(index);
            var content = OpenModal("Trò chơi mới: " + station.Title, 640f);

            var row = Row(content, 260f);
            var icon = Img(row, station.Icon, Color.white, "Icon");
            icon.raycastTarget = false;
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(200f, 200f));
            var text = Label(row, "Trò chơi dân gian " + OriginTag(station.Origin)
                + "\n<size=30>Mở trò để nghe kể về ý nghĩa của trò chơi. Khách ghé chơi sẽ trả phí cho bạn, còn bạn chơi để nhận xu và tăng thu nhập.</size>",
                36, Ink, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(260f, 16f), new Vector2(-30f, -16f));

            var button = Btn(content, "", Ok, () =>
            {
                if (!street.TryUnlockFolkGame(index))
                {
                    NotEnough("Chưa đủ xu để mở " + station.Title);
                    return;
                }
                CloseModal();
                ShowDialogue(station.Story, () => Toast("Chạm vào " + station.Title + " để chơi!"));
            });
            Height(button, 130f);
            refreshModal = () =>
            {
                SetText(button, "Mở trò chơi - " + NumberFormat.Short(cost) + " xu");
                Look(button, g.Economy.CanAfford(cost), Ok);
            };
            refreshModal();
        }

        /// <summary>Chơi một trò: lần đầu thì nghe hướng dẫn trước.</summary>
        public void StartFolkGame(FolkGameStation station)
        {
            var data = Game.I.Data;
            int bit = 1 << (int)station.Kind;
            if ((data.folkTutorialSeen & bit) != 0)
            {
                OpenFolkGame(station);
                return;
            }
            data.folkTutorialSeen |= bit;
            CloseModal();
            ShowDialogue(station.Tutorial, () => OpenFolkGame(station));
        }

        public void OpenFolkGame(FolkGameStation station)
        {
            var content = OpenModal(station.Title + "  ·  " + CountryName(station.Origin), 860f);
            var hint = Label(content, "", 28, Muted, TextAnchor.MiddleLeft);
            Height(hint, 56f);
            var area = Row(content, 384f);
            area.gameObject.AddComponent<RectMask2D>();
            var status = Label(content, "", 32, Accent, TextAnchor.MiddleCenter);
            Height(status, 56f);

            var buttons = HLayout(content, 120f, 16f);
            buttons.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            int seed = System.Environment.TickCount;
            // Đập niêu: hai nút bước trái, phải nằm hai bên nút ĐẬP.
            var pot = station.Kind == FolkGameKind.DapNieu ? new PotSmashModel(FolkView.Aspect, seed) : null;
            if (pot != null) Grow(Btn(buttons, "‹ TRÁI", GemBlue, () => pot.Walk(-1), 40), 1.4f);
            var action = Grow(Btn(buttons, "", Ok, () => { }, 48), 2f);
            if (pot != null) Grow(Btn(buttons, "PHẢI ›", GemBlue, () => pot.Walk(1), 40), 1.4f);
            Grow(Btn(buttons, "Hướng dẫn", Lavender, () =>
            {
                CloseModal();
                ShowDialogue(station.Tutorial, () => OpenFolkGame(station));
            }, 32), 1f);

            switch (station.Kind)
            {
                case FolkGameKind.DapNieu: folkGame = new PotSmashView(this, station, area, status, hint, action, pot); break;
                case FolkGameKind.DaCau: folkGame = new ShuttlecockView(this, station, area, status, hint, action, new ShuttlecockModel(FolkView.Aspect, seed)); break;
                default: folkGame = new TuhoView(this, station, area, status, hint, action, new TuhoModel(FolkView.Aspect, seed)); break;
            }
            action.onClick.AddListener(folkGame.Press);
        }

        static Button Grow(Button b, float weight)
        {
            b.gameObject.AddComponent<LayoutElement>().flexibleWidth = weight;
            return b;
        }

        /// <summary>
        /// Phần vẽ chung của mini-game: chạy logic (FolkModels.cs), chuyển chạm trên sân chơi vào logic, hiện chữ nổi,
        /// phát âm thanh, cập nhật dòng trạng thái, nút bấm và trao thưởng khi xong ván.
        /// </summary>
        abstract class FolkView
        {
            /// <summary>Tỉ lệ ngang/dọc của sân chơi (khoảng 1208 x 384): toạ độ mô hình x chạy 0..Aspect.</summary>
            public const float Aspect = 3.15f;

            protected static readonly Color Wood = new Color(0.55f, 0.36f, 0.22f);

            protected readonly UIManager ui;
            protected readonly FolkGameStation station;
            protected readonly RectTransform area;
            protected readonly Text status, hint;
            protected readonly Button action;
            readonly FolkModel model;
            readonly System.Collections.Generic.List<Text> floaters = new System.Collections.Generic.List<Text>();
            readonly System.Collections.Generic.List<float> floaterAge = new System.Collections.Generic.List<float>();
            bool rewarded;

            protected FolkView(UIManager ui, FolkGameStation station, RectTransform area, Text status, Text hint, Button action, FolkModel model)
            {
                this.ui = ui;
                this.station = station;
                this.area = area;
                this.status = status;
                this.hint = hint;
                this.action = action;
                this.model = model;
                var input = area.gameObject.AddComponent<FolkInputArea>();
                input.down = p => { if (!rewarded) model.PointerDown(p.x * Aspect, p.y); OnTap(p); };
                input.drag = p => { if (!rewarded) model.PointerDrag(p.x * Aspect, p.y); };
                input.up = p => { if (!rewarded) model.PointerUp(p.x * Aspect, p.y); };
            }

            public void Tick(float dt)
            {
                if (!rewarded) model.Update(dt);
                Sync(dt);
                foreach (var p in model.Pops) SpawnFloater(p);
                model.Pops.Clear();
                foreach (int s in model.Sounds) Game.I.Audio.Play(s == 2 ? Sfx.Chime : s > 0 ? Sfx.Coin : Sfx.Error);
                model.Sounds.Clear();
                for (int i = floaters.Count - 1; i >= 0; i--)
                {
                    float age = floaterAge[i] += dt;
                    var t = floaters[i];
                    var rt = t.rectTransform;
                    rt.anchoredPosition = new Vector2(0f, 20f + 80f * age);
                    var c = t.color;
                    c.a = 1f - Mathf.Clamp01((age - 0.6f) * 2.5f);
                    t.color = c;
                    if (age >= 1f)
                    {
                        Object.Destroy(t.gameObject);
                        floaters.RemoveAt(i);
                        floaterAge.RemoveAt(i);
                    }
                }
                if (rewarded) return;
                status.text = model.Status;
                hint.text = model.Hint;
                SetText(action, model.ActionLabel);
                Look(action, model.ActionEnabled, Ok);
                if (model.Finished) Reward();
            }

            public void Press()
            {
                if (rewarded) ui.CloseModal();
                else model.Action();
            }

            protected abstract void Sync(float dt);
            protected virtual void OnTap(Vector2 p) { }

            void SpawnFloater(FolkPop p)
            {
                var t = Label(area, p.text, 42, p.good ? Accent : Muted, TextAnchor.MiddleCenter);
                t.fontStyle = FontStyle.Bold;
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3f, -3f);
                var rt = t.rectTransform;
                float x = Mathf.Clamp(p.x / Aspect, 0.15f, 0.85f), y = Mathf.Clamp(p.y, 0.1f, 0.88f);
                Place(rt, new Vector2(x, y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 70f));
                floaters.Add(t);
                floaterAge.Add(0f);
            }

            void Reward()
            {
                rewarded = true;
                var g = Game.I;
                var cfg = g.config;
                float score01 = Mathf.Clamp01(model.Score01);
                double full = System.Math.Max(cfg.folkRewardMin, g.Street.IncomePerMinute * cfg.folkRewardMinutes);
                // Điểm thấp vẫn có chút quà để không mất công đi tới.
                double coins = System.Math.Round(score01 > 0f ? full * score01 : full * 0.1);
                g.Economy.Add(coins);
                g.Data.stats.coinsCollected += coins;
                g.Data.stats.folkGamesPlayed++;
                station.OnPlayerPlayed();
                ui.OnCoinsCollected(station.transform.position + Vector3.up * 1.5f, coins);

                string boost = "";
                float multiplier = score01 >= 0.999f ? cfg.folkBoostPerfect : score01 >= 0.5f ? cfg.folkBoostGood : 1f;
                if (multiplier > 1f)
                {
                    g.StartIncomeBoost(multiplier, cfg.folkBoostSeconds);
                    boost = "  ·  Thu nhập x" + multiplier.ToString("0.#") + " trong " + Mathf.RoundToInt(cfg.folkBoostSeconds) + " giây!";
                    g.Audio.Play(Sfx.Chime);
                }
                status.text = model.Summary + "  ·  +" + NumberFormat.Short(coins) + " xu" + boost;
                hint.text = score01 >= 0.999f ? "Trọn điểm! Tuyệt vời!" : score01 >= 0.5f ? "Chơi tốt lắm!" : "Lần sau sẽ tốt hơn!";
                SetText(action, "Nhận thưởng");
                Look(action, true, Gold);
            }

            // ---------- Dựng hình ----------

            protected RectTransform Rect(Color color, string name, RectTransform parent = null)
            {
                var img = Framed(Img(parent != null ? parent : area, null, color, name), RowSprite);
                img.raycastTarget = false;
                return img.rectTransform;
            }

            protected RectTransform Pic(Sprite sprite, Vector2 size, Vector2 pivot, string name, RectTransform parent = null)
            {
                var img = Img(parent != null ? parent : area, sprite, Color.white, name);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.pivot = pivot;
                rt.sizeDelta = size;
                return rt;
            }

            protected RectTransform Dot(Color color, float size, string name, RectTransform parent = null)
            {
                var img = Img(parent != null ? parent : area, KnobSprite, color, name);
                img.raycastTarget = false;
                img.preserveAspect = false;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                return img.rectTransform;
            }

            protected Text Caption(string text, int size, Color color, Vector2 anchor, Vector2 boxSize, RectTransform parent = null, bool bold = false)
            {
                var t = Label(parent != null ? parent : area, text, size, color, TextAnchor.MiddleCenter);
                if (bold) t.fontStyle = FontStyle.Bold;
                Place(t.rectTransform, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, boxSize);
                return t;
            }

            /// <summary>Đặt khối tại toạ độ mô hình (x 0..Aspect, y 0..1).</summary>
            protected static void Put(RectTransform rt, float x, float y)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(x / Aspect, y);
                rt.anchoredPosition = Vector2.zero;
            }

            protected static void Span(RectTransform rt, float x0, float x1, float y0, float y1) =>
                Stretch(rt, new Vector2(x0, y0), new Vector2(x1, y1), Vector2.zero, Vector2.zero);

            /// <summary>Số điểm ảnh của một đơn vị mô hình (bằng chiều cao sân chơi); 0 khi bố cục chưa dựng xong.</summary>
            protected float Unit => area.rect.height;
        }

        /// <summary>
        /// Nhân vật chính ghép từ 6 phần hình như trên phố (Art/Player), neo ở chân; tay, chân xoay quanh khớp.
        /// Vị trí khớp giống TamiBuilder.PlayerParts (đơn vị thế giới), nhân với số điểm ảnh mỗi đơn vị.
        /// </summary>
        sealed class PlayerFigure
        {
            public readonly RectTransform root, armFront, armBack, legFront, legBack, head;

            public PlayerFigure(RectTransform parent, float pxPerUnit, string name)
            {
                var art = Game.I.art;
                root = NewRect(parent, name);
                root.pivot = new Vector2(0.5f, 0f);
                root.sizeDelta = new Vector2(1.7f, 1.95f) * pxPerUnit;
                legBack = Part(art.playerLegRight, new Vector2(0.165f, 0.305f), pxPerUnit);
                legFront = Part(art.playerLegLeft, new Vector2(-0.142f, 0.338f), pxPerUnit);
                armBack = Part(art.playerArmRight, new Vector2(0.135f, 0.734f), pxPerUnit);
                Part(art.playerBody, new Vector2(0.013f, 0.19f), pxPerUnit);
                armFront = Part(art.playerArmLeft, new Vector2(-0.294f, 0.701f), pxPerUnit);
                head = Part(art.playerHead, new Vector2(-0.013f, 0.684f), pxPerUnit);
            }

            RectTransform Part(Sprite sprite, Vector2 joint, float pxPerUnit)
            {
                var img = Img(root, sprite, Color.white, sprite != null ? sprite.name : "Part");
                img.raycastTarget = false;
                img.preserveAspect = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                if (sprite != null)
                {
                    var size = sprite.rect.size;
                    rt.pivot = new Vector2(sprite.pivot.x / size.x, sprite.pivot.y / size.y);
                    rt.sizeDelta = size / sprite.pixelsPerUnit * pxPerUnit;
                }
                rt.anchoredPosition = joint * pxPerUnit;
                return rt;
            }

            /// <summary>Hình gốc nhìn sang phải.</summary>
            public void Face(bool right)
            {
                var s = root.localScale;
                root.localScale = new Vector3(Mathf.Abs(s.x) * (right ? 1f : -1f), s.y, 1f);
            }
        }

        // ------------------------------------------------------------------------------------------------
        sealed class PotSmashView : FolkView
        {
            const float PotY = 0.66f, WalkerY = 0.03f;
            readonly PotSmashModel m;
            readonly RectTransform[] pots, strings;
            readonly Image[] potImages;
            readonly Text[] tags;
            readonly PlayerFigure walker;
            readonly RectTransform band, blind, cheerFill;
            readonly Text shout, speaker, walkHint;

            public PotSmashView(UIManager ui, FolkGameStation station, RectTransform area, Text status, Text hint, Button action, PotSmashModel m)
                : base(ui, station, area, status, hint, action, m)
            {
                this.m = m;
                var rope = Rect(Wood, "Rope");
                Span(rope, 0.02f, 0.98f, 0.9f, 0.915f);
                int n = m.Pots.Count;
                pots = new RectTransform[n];
                strings = new RectTransform[n];
                potImages = new Image[n];
                tags = new Text[n];
                for (int i = 0; i < n; i++)
                {
                    strings[i] = Rect(Wood, "String");
                    pots[i] = Pic(station.Icon, new Vector2(84f, 98f), new Vector2(0.5f, 0.5f), "Pot");
                    potImages[i] = pots[i].GetComponent<Image>();
                    tags[i] = Caption("", 30, Accent, Vector2.zero, new Vector2(240f, 44f), null, true);
                }

                walker = new PlayerFigure(area, 78f, "Walker");
                // Gậy cầm ở tay trước; dải băng bịt mắt quấn ngang đầu.
                var stick = Rect(Wood, "Stick", walker.armFront);
                stick.anchorMin = stick.anchorMax = new Vector2(0.3f, 0.1f);
                stick.pivot = new Vector2(0.5f, 0.1f);
                stick.sizeDelta = new Vector2(9f, 92f);
                stick.anchoredPosition = Vector2.zero;
                band = Rect(new Color(0.85f, 0.25f, 0.25f), "Blindfold", walker.head);
                Stretch(band, new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.5f), Vector2.zero, Vector2.zero);

                // Bịt mắt: tối hoàn toàn, chỉ còn nghe tiếng hô và tiếng reo.
                var blindImage = Img(area, null, Color.black, "Blind");
                blindImage.raycastTarget = false;
                blind = blindImage.rectTransform;
                Span(blind, 0f, 1f, 0f, 1f);
                speaker = Caption("", 28, new Color(0.75f, 0.75f, 0.8f), new Vector2(0.5f, 0.84f), new Vector2(600f, 40f), blind);
                shout = Caption("", 56, Color.white, new Vector2(0.5f, 0.66f), new Vector2(1000f, 80f), blind, true);
                Caption("Tiếng reo của cả xóm", 26, new Color(0.75f, 0.75f, 0.8f), new Vector2(0.5f, 0.42f), new Vector2(500f, 36f), blind);
                var meter = Rect(new Color(0.22f, 0.21f, 0.25f), "CheerBack", blind);
                Span(meter, 0.2f, 0.8f, 0.24f, 0.34f);
                cheerFill = Rect(Gold, "CheerFill", meter);
                // Nhãn hai đầu thanh: đầy là đang ở gần niêu quà.
                Caption("xa", 24, new Color(0.6f, 0.6f, 0.65f), new Vector2(0.17f, 0.29f), new Vector2(80f, 36f), blind);
                Caption("gần niêu", 24, Gold, new Vector2(0.86f, 0.29f), new Vector2(140f, 36f), blind, true);
                walkHint = Caption("", 26, new Color(0.6f, 0.6f, 0.65f), new Vector2(0.5f, 0.1f), new Vector2(1150f, 36f), blind);
            }

            protected override void Sync(float dt)
            {
                bool over = m.Finished;
                for (int i = 0; i < m.Pots.Count; i++)
                {
                    var p = m.Pots[i];
                    float x = m.PotX(p);
                    Put(pots[i], x, PotY);
                    pots[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m.Time * 1.4f + p.phase) * 7f + (p.broken ? 14f : 0f));
                    pots[i].localScale = p.broken ? new Vector3(1f, 0.55f, 1f) : Vector3.one;
                    potImages[i].color = p.broken ? new Color(0.6f, 0.5f, 0.45f) : Color.white;
                    Span(strings[i], x / Aspect, x / Aspect, PotY + 0.12f, 0.9f);
                    strings[i].sizeDelta = new Vector2(4f, 0f);
                    string tag = "";
                    if (p.broken || over) tag = p.kind == PotSmashModel.PotKind.Prize ? "Quà to" : p.kind == PotSmashModel.PotKind.Candy ? "Bánh kẹo" : "Nước";
                    else if (m.State == PotSmashModel.Phase.Peek && p.kind == PotSmashModel.PotKind.Prize) tag = "QUÀ TO";
                    tags[i].text = tag;
                    tags[i].color = p.kind == PotSmashModel.PotKind.Water ? GemBlue : Accent;
                    Put(tags[i].rectTransform, x, PotY - 0.2f);
                }

                Put(walker.root, m.WalkerX, WalkerY);
                bool spinning = m.State == PotSmashModel.Phase.Spin && !over;
                walker.Face(!spinning || Mathf.Sin(m.Time * 18f) > 0f);
                band.gameObject.SetActive(m.State != PotSmashModel.Phase.Peek && !over);
                // Vung gậy: giơ tay ra trước rồi bổ xuống.
                float swing = m.SwingAnim > 0f ? Mathf.Sin(Mathf.PI * (1f - m.SwingAnim)) : 0f;
                walker.armFront.localRotation = Quaternion.Euler(0f, 0f, 150f * swing);

                blind.gameObject.SetActive(m.State == PotSmashModel.Phase.Blind && !over);
                speaker.text = m.HintSpeaker + " hô:";
                shout.text = m.HintText;
                var c = m.HintIsPrank ? new Color(1f, 0.72f, 0.42f) : Color.white;
                c.a = 1f - Mathf.Clamp01((m.HintAge - 0.45f) / 0.35f) * 0.55f;
                shout.color = c;
                Span(cheerFill, 0f, Mathf.Max(0.02f, m.Cheer), 0f, 1f);
                walkHint.text = (m.Cheer >= 0.8f ? "Thanh gần đầy: niêu ở rất gần, bấm ĐẬP!" : "Thanh càng đầy càng gần niêu")
                    + "  ·  Còn " + m.SwingsLeft + " lần đập  ·  ‹ TRÁI / PHẢI › để bước";
            }
        }

        // ------------------------------------------------------------------------------------------------
        sealed class ShuttlecockView : FolkView
        {
            const float FootY = 0.02f;
            // Nhân vật đứng lệch trái quả cầu bấy nhiêu đơn vị sân (≈ 30 điểm ảnh) để cầu rơi trước mũi chân đá.
            const float KickerOffset = 0.08f;
            readonly ShuttlecockModel m;
            readonly RectTransform cau, ring;
            readonly PlayerFigure me;
            readonly Text windText;
            float footX, ringAge = 1f;

            public ShuttlecockView(UIManager ui, FolkGameStation station, RectTransform area, Text status, Text hint, Button action, ShuttlecockModel m)
                : base(ui, station, area, status, hint, action, m)
            {
                this.m = m;
                var zone = Rect(new Color(0.56f, 0.78f, 0.65f, 0.28f), "KickZone");
                Span(zone, 0f, 1f, 0f, ShuttlecockModel.KickTop);
                var sweet = Rect(new Color(0.56f, 0.78f, 0.65f, 0.45f), "Sweet");
                Span(sweet, 0f, 1f, ShuttlecockModel.SweetLow, ShuttlecockModel.SweetHigh);
                Caption("Vùng chân", 24, new Color(0.3f, 0.5f, 0.38f), new Vector2(0.06f, ShuttlecockModel.KickTop - 0.05f), new Vector2(200f, 34f));

                me = new PlayerFigure(area, 72f, "Me");
                footX = m.X - KickerOffset;
                ring = Dot(new Color(1f, 1f, 1f, 0.6f), 70f, "TapRing");
                cau = Pic(Game.I.art.folkDaCau != null ? Game.I.art.folkDaCau : station.Icon, new Vector2(72f, 64f), new Vector2(0.5f, 0.5f), "Cau");
                windText = Caption("", 30, Muted, new Vector2(0.5f, 0.92f), new Vector2(400f, 40f), null, true);
            }

            protected override void OnTap(Vector2 p)
            {
                ringAge = 0f;
                ring.anchorMin = ring.anchorMax = p;
                ring.anchoredPosition = Vector2.zero;
            }

            protected override void Sync(float dt)
            {
                Put(cau, m.X, m.Y);
                // Cầu lông luôn lông hướng lên, nghiêng theo hướng bay.
                cau.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-m.VX * 30f, -35f, 35f) + Mathf.Sin(m.Time * 7f) * 6f);

                // Nhân vật luôn chạy theo quả cầu, đứng lệch trái một chút để cầu rơi ngay trước chân đá.
                // Cầu càng xuống thấp càng chạy nhanh để kịp đứng đúng chỗ.
                float run = m.VY < 0f && m.Y < 0.75f ? 4.5f : 2.5f;
                footX = Mathf.MoveTowards(footX, m.X - KickerOffset, dt * run);
                Put(me.root, footX, FootY);
                me.legFront.localRotation = Quaternion.Euler(0f, 0f, 90f * m.KickAnim);
                me.armBack.localRotation = Quaternion.Euler(0f, 0f, -35f * m.KickAnim);

                ringAge += dt;
                ring.localScale = Vector3.one * (0.5f + ringAge * 2f);
                ring.GetComponent<Image>().color = new Color(1f, 1f, 1f, Mathf.Max(0f, 0.6f - ringAge * 2f));
                windText.text = m.Wind > 0f ? "Gió thổi sang phải ›››" : m.Wind < 0f ? "‹‹‹ Gió thổi sang trái" : "";
            }
        }

        // ------------------------------------------------------------------------------------------------
        sealed class TuhoView : FolkView
        {
            const int PreviewDots = 16;
            readonly TuhoModel m;
            readonly RectTransform jar, arrow, flagPole, flag;
            readonly RectTransform[] dots = new RectTransform[PreviewDots];
            readonly System.Collections.Generic.List<RectTransform> stuck = new System.Collections.Generic.List<RectTransform>();
            readonly PlayerFigure thrower;
            readonly Text power;

            public TuhoView(UIManager ui, FolkGameStation station, RectTransform area, Text status, Text hint, Button action, TuhoModel m)
                : base(ui, station, area, status, hint, action, m)
            {
                this.m = m;
                var ground = Rect(new Color(0.88f, 0.84f, 0.78f), "Ground");
                Span(ground, 0f, 1f, 0f, TuhoModel.GroundY);
                flagPole = Rect(Wood, "FlagPole");
                flag = Rect(CloseColor, "Flag");
                var jarSprite = Game.I.art.folkTuho != null ? Game.I.art.folkTuho : station.Icon;
                jar = Pic(jarSprite, new Vector2(100f, 200f), new Vector2(0.5f, 0f), "Jar");
                thrower = new PlayerFigure(area, 74f, "Thrower");
                Put(thrower.root, m.ThrowX - 0.16f, TuhoModel.GroundY);
                // Đường chấm ngắm màu đậm, nhạt dần về cuối.
                for (int i = 0; i < PreviewDots; i++)
                {
                    var c = Accent;
                    c.a = 1f - i * 0.04f;
                    dots[i] = Dot(c, 15f - i * 0.4f, "Dot");
                }
                arrow = NewArrow("Arrow");
                power = Caption("", 26, Muted, Vector2.zero, new Vector2(200f, 36f));
            }

            RectTransform NewArrow(string name)
            {
                var a = Rect(Wood, name);
                a.pivot = new Vector2(0.5f, 1f);
                a.sizeDelta = new Vector2(8f, 70f);
                var tip = Rect(new Color(0.35f, 0.33f, 0.36f), "Tip", a);
                Span(tip, -0.3f, 1.3f, 0.86f, 1f);
                var fletch = Rect(CloseColor, "Fletch", a);
                Span(fletch, -0.9f, 1.9f, 0f, 0.22f);
                return a;
            }

            static float Angle(float vx, float vy) => Mathf.Atan2(vy, vx) * Mathf.Rad2Deg - 90f;

            protected override void Sync(float dt)
            {
                float unit = Unit;
                if (unit <= 0f) return;
                // Bình cao đúng JarHeight đơn vị, chân đứng trên mặt đất.
                var sprite = jar.GetComponent<Image>().sprite;
                float ratio = sprite != null ? sprite.rect.width / sprite.rect.height : 0.5f;
                jar.sizeDelta = new Vector2(TuhoModel.JarHeight * unit * ratio, TuhoModel.JarHeight * unit);
                Put(jar, m.JarX, TuhoModel.GroundY);

                // Cờ báo gió cạnh bình.
                Put(flagPole, m.JarX + 0.45f, TuhoModel.GroundY);
                flagPole.pivot = new Vector2(0.5f, 0f);
                flagPole.sizeDelta = new Vector2(6f, 0.38f * unit);
                float w = Mathf.Abs(m.Wind) * 140f;
                flag.gameObject.SetActive(w > 1f);
                Put(flag, m.JarX + 0.45f, TuhoModel.GroundY + 0.34f);
                flag.pivot = new Vector2(m.Wind > 0f ? 0f : 1f, 0.5f);
                flag.sizeDelta = new Vector2(w, 24f);

                // Đường ngắm (có tính gió) vẽ tới khi mũi tên đi được PreviewReach quãng đường tới bình.
                bool aim = m.Aiming && (m.AimVX * m.AimVX + m.AimVY * m.AimVY) > 0.04f;
                float reachT = aim ? m.PreviewDuration(m.AimVX, m.AimVY) : 0f;
                for (int i = 0; i < PreviewDots; i++)
                {
                    dots[i].gameObject.SetActive(aim);
                    if (!aim) continue;
                    m.PathPoint(m.AimVX, m.AimVY, reachT * (i + 1) / PreviewDots, out float x, out float y);
                    Put(dots[i], x, y);
                }
                power.gameObject.SetActive(m.Aiming);
                if (m.Aiming)
                {
                    float sp = Mathf.Sqrt(m.AimVX * m.AimVX + m.AimVY * m.AimVY) / TuhoModel.MaxSpeed;
                    power.text = "Lực " + Mathf.RoundToInt(sp * 100f) + "%";
                    Put(power.rectTransform, m.ThrowX, m.ThrowY + 0.38f);
                }
                thrower.armFront.localRotation = Quaternion.Euler(0f, 0f, m.Aiming ? -60f : m.Flying ? 70f : 0f);

                arrow.gameObject.SetActive(m.Flying);
                if (m.Flying)
                {
                    arrow.sizeDelta = new Vector2(8f, 0.2f * unit);
                    Put(arrow, m.ArrowX, m.ArrowY);
                    arrow.localRotation = Quaternion.Euler(0f, 0f, Angle(m.ArrowVX, m.ArrowVY));
                }
                // Mũi tên đã lọt bình cắm lại.
                while (stuck.Count < m.Stuck.Count)
                {
                    var s = m.Stuck[stuck.Count];
                    var a = NewArrow("Stuck");
                    a.sizeDelta = new Vector2(8f, 0.2f * unit);
                    Put(a, s.x, s.y);
                    a.localRotation = Quaternion.Euler(0f, 0f, s.angle - 90f);
                    a.SetSiblingIndex(jar.GetSiblingIndex());
                    stuck.Add(a);
                }
                // Bình lùi xa: dời các mũi tên đã cắm theo bình.
                for (int i = 0; i < stuck.Count; i++)
                {
                    var s = m.Stuck[i];
                    Put(stuck[i], s.x + (m.JarX - s.jarX), s.y);
                }
            }
        }
    }
}
