using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tami
{
    /// <summary>
    /// Một quầy hàng với 4 nâng cấp: sạp hàng (4 cấp), mở món (3 món, mỗi món giá riêng),
    /// người bán sau quầy (tự thu tiền) và doanh thu (tăng giá bán mọi món).
    /// Khách xếp hàng, mua ngẫu nhiên một món đã mở; tiền dồn vào quỹ chờ thu.
    /// </summary>
    public class Stall : MonoBehaviour, IPointerClickHandler, IInteractable
    {
        [Header("Thành phần (prefab Stall)")]
        public SpriteRenderer body;
        [Tooltip("Hình người bán dự phòng khi chưa có bộ hình khách")]
        public SpriteRenderer managerSprite;
        public TextMesh nameLabel;
        public SpriteRenderer progressBg;
        public SpriteRenderer progressFill;
        public MoneyBag bag;
        [Tooltip("Chiều dài thanh tiến độ phục vụ")]
        public float barWidth = 1.6f;

        public const int DishSlots = 3;

        public StallData Data { get; private set; }
        public StallSlot Slot { get; private set; }
        /// <summary>Cấp sạp hàng (1-4).</summary>
        public int Level { get; private set; } = 1;
        /// <summary>Số món đã mở (1-3).</summary>
        public int DishCount { get; private set; } = 1;
        /// <summary>Cấp nâng doanh thu (0 = chưa nâng).</summary>
        public int RevenueLevel { get; private set; }
        public double Vault { get; private set; }
        /// <summary>Đã thuê người bán sau quầy (tự thu tiền, kiếm tiền khi offline).</summary>
        public bool HasManager { get; private set; }
        /// <summary>Đã thuê người chào hàng đứng trước quầy (mời khách đi ngang ghé quầy).</summary>
        public bool HasBarker { get; private set; }
        /// <summary>Thu nhập trung bình gần đây (xu/giây), dùng để tính thu nhập offline.</summary>
        public double IncomeRate { get; private set; }
        public event System.Action OnChanged;

        GameConfig Cfg => Game.I.config;

        // ---------- Sạp hàng ----------
        public int MaxLevel => Cfg.levels.Length;
        public bool IsMaxLevel => Level >= MaxLevel;
        public double NextUpgradeCost => IsMaxLevel ? 0 : Data.unlockCost * Cfg.levels[Level].upgradeCostMultiplier;
        public float ServiceTimeAt(int level) => Data.baseServiceTime * Cfg.levels[level - 1].serviceTimeMultiplier;
        public float ServiceTime => ServiceTimeAt(Level);

        // ---------- Món ăn ----------
        public bool AllDishesOpen => DishCount >= DishSlots;
        public double NextDishCost => AllDishesOpen ? 0 : Data.unlockCost * Cfg.dishUnlockCostMultipliers[DishCount];
        public string DishName(int i) =>
            Data.products != null && i < Data.products.Length && !string.IsNullOrEmpty(Data.products[i]) ? Data.products[i] : Data.displayName;
        public Sprite DishSprite(int i) => Data.dishSprites != null && i < Data.dishSprites.Length ? Data.dishSprites[i] : null;

        /// <summary>Giá bán một món theo cấp sạp và cấp doanh thu cho trước.</summary>
        public double DishPriceAt(int dish, int level, int revenue) =>
            Data.baseIncome * Cfg.dishPriceMultipliers[dish] * Cfg.levels[level - 1].incomeMultiplier * RevenueMultiplierAt(revenue);
        public double DishPrice(int dish) => DishPriceAt(dish, Level, RevenueLevel);

        // ---------- Doanh thu ----------
        public int MaxRevenueLevel => Cfg.revenueMaxLevel;
        public bool IsMaxRevenue => RevenueLevel >= MaxRevenueLevel;
        public double RevenueMultiplierAt(int revenue) => 1.0 + Cfg.revenuePerLevel * revenue;
        public double NextRevenueCost => IsMaxRevenue ? 0
            : Data.unlockCost * Cfg.revenueCostMultiplier * System.Math.Pow(Cfg.revenueCostGrowth, RevenueLevel);

        // ---------- Người bán ----------
        public double ManagerCost => Data.unlockCost * Cfg.managerCostMultiplier;
        public double BarkerCost => Data.unlockCost * Cfg.barkerCostMultiplier;

        /// <summary>Giá bán trung bình các món đã mở (khách mua ngẫu nhiên một món).</summary>
        public double IncomePerSale
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < DishCount; i++) sum += DishPrice(i);
                return sum / DishCount;
            }
        }
        public double VaultCapacityAt(int level) => DishPriceAt(DishCount - 1, level, RevenueLevel) * Cfg.levels[level - 1].vaultCapacitySales;
        public double VaultCapacity => VaultCapacityAt(Level);
        public bool VaultFull => Vault >= VaultCapacity;
        public bool CanAcceptCustomer => !VaultFull && queue.Count < Cfg.queueCapacity;
        public double OfflineRate => System.Math.Max(IncomeRate, 0.1 * IncomePerSale / ServiceTime);
        bool IsServing => queue.Count > 0 && queue[0].IsAtCounter && !VaultFull;

        readonly List<Npc> queue = new List<Npc>();
        readonly SpriteRenderer[] dishes = new SpriteRenderer[DishSlots];
        readonly float[] dishBounce = new float[DishSlots];
        Transform seller, barker;
        StaffIdle barkerAnim;
        SpeechBubble barkerSpeech;
        float barkerTimer, barkerSpeechUntil;
        float serveTimer, bounce, collectTimer, rateTimer;
        double earnedWindow;

        /// <summary>
        /// Đổi ô tên tối trong prefab sang bảng tên 9-slice nền trắng (chữ màu tối), rộng theo độ dài tên quầy.
        /// </summary>
        void UseNamePlate()
        {
            var frame = Game.I.art.frameName;
            var plate = transform.Find("NamePlate");
            if (frame == null || plate == null || !plate.TryGetComponent<SpriteRenderer>(out var sr)) return;
            var center = nameLabel.transform.localPosition;
            sr.sprite = frame;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.color = Color.white;
            plate.localScale = Vector3.one;
            plate.localPosition = new Vector3(center.x, center.y, plate.localPosition.z);
            // Lưới chữ TextMesh chỉ dựng lại khi vẽ, nên ước bề ngang theo số ký tự (cỡ chữ 0,036 x 64 ~ 0,13 mỗi ký tự).
            float width = Mathf.Max(1.6f, nameLabel.text.Length * 0.13f + 0.5f);
            sr.size = new Vector2(width, 0.5f);
            nameLabel.color = new Color(0.36f, 0.22f, 0.18f);
        }

        /// <summary>Dựng quầy từ prefab vào ô sạp.</summary>
        public static Stall Create(Stall prefab, StallData data, StallSlot slot)
        {
            var st = Instantiate(prefab, slot.transform);
            st.name = "Stall_" + data.id;
            st.transform.localPosition = Vector3.zero;
            st.Data = data;
            st.Slot = slot;

            if (st.nameLabel != null)
            {
                st.nameLabel.text = data.displayName;
                st.UseNamePlate();
            }
            st.managerSprite.enabled = false;
            st.progressBg.enabled = false;
            st.progressFill.enabled = false;
            for (int i = 0; i < DishSlots; i++)
            {
                // Món bày trên mặt quầy, vẽ trước thân sạp.
                st.dishes[i] = Game.I.NewSprite(st.transform, null, 2, "Dish" + (i + 1));
                st.dishes[i].transform.localPosition = new Vector3((i - 1) * data.dishSpacing, data.counterTopY, 0f);
                st.dishes[i].transform.localScale = Vector3.one * data.dishScale;
            }
            st.bag.Init(st);
            st.RefreshVisuals();
            st.GetComponent<YSort>()?.Rebase();
            return st;
        }

        /// <summary>Khôi phục trạng thái từ save. dishes = 0 là save cũ (3 cấp, mỗi cấp một món).</summary>
        public void Restore(int level, int dishCount, int revenue, double vault, bool manager, bool hasBarker, double rate)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
            DishCount = Mathf.Clamp(dishCount > 0 ? dishCount : level, 1, DishSlots);
            RevenueLevel = Mathf.Clamp(revenue, 0, MaxRevenueLevel);
            Vault = System.Math.Min(System.Math.Max(0, vault), VaultCapacity);
            IncomeRate = rate;
            SetManager(manager);
            SetBarker(hasBarker);
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            var sprite = SpriteForLevel(Level);
            if (sprite != null) body.sprite = sprite;
            for (int i = 0; i < DishSlots; i++)
            {
                var s = DishSprite(i);
                dishes[i].sprite = s;
                dishes[i].enabled = s != null && i < DishCount;
            }
            // Túi tiền nằm trên nóc sạp nhưng không chạm thanh giao diện trên cùng.
            if (body.sprite != null) bag.SetBaseY(Mathf.Min(body.sprite.bounds.max.y - 0.35f, 2.75f));
            bag.Refresh();
        }

        Sprite SpriteForLevel(int level)
        {
            var sprites = Data.levelSprites;
            if (sprites == null) return null;
            // Thiếu hình cấp cao thì dùng hình cấp gần nhất đã có.
            for (int i = Mathf.Min(level, sprites.Length) - 1; i >= 0; i--)
            {
                if (sprites[i] != null) return sprites[i];
            }
            return null;
        }

        public void AddOfflineVault(double amount)
        {
            Vault = System.Math.Min(VaultCapacity, Vault + amount);
            bag.Refresh();
        }

        public void SetVaultForTest(double amount)
        {
            Vault = System.Math.Min(VaultCapacity, amount);
            bag.Refresh();
        }

        public void Enqueue(Npc npc) => queue.Add(npc);

        public int QueueIndexOf(Npc npc) => queue.IndexOf(npc);

        public void Leave(Npc npc)
        {
            int i = queue.IndexOf(npc);
            if (i < 0) return;
            queue.RemoveAt(i);
            if (i == 0) serveTimer = 0f;
        }

        void Update()
        {
            bool serving = IsServing;
            if (serving)
            {
                serveTimer += Time.deltaTime;
                if (serveTimer >= ServiceTime) CompleteSale();
            }

            progressBg.enabled = serving;
            progressFill.enabled = serving;
            if (serving)
            {
                var s = progressFill.transform.localScale;
                s.x = barWidth * Mathf.Clamp01(serveTimer / ServiceTime);
                progressFill.transform.localScale = s;
            }

            if (bounce > 0f)
            {
                bounce = Mathf.Max(0f, bounce - Time.deltaTime * 4f);
                float k = Mathf.Sin(bounce * Mathf.PI) * 0.08f;
                body.transform.localScale = new Vector3(1f + k, 1f - k, 1f);
            }
            for (int i = 0; i < DishSlots; i++)
            {
                if (dishBounce[i] <= 0f) continue;
                dishBounce[i] = Mathf.Max(0f, dishBounce[i] - Time.deltaTime * 4f);
                dishes[i].transform.localScale = Vector3.one * (Data.dishScale * (1f + Mathf.Sin(dishBounce[i] * Mathf.PI) * 0.25f));
            }

            rateTimer += Time.deltaTime;
            if (rateTimer >= 1f)
            {
                rateTimer -= 1f;
                IncomeRate = IncomeRate * 0.98 + earnedWindow * 0.02;
                earnedWindow = 0;
            }

            if (HasBarker) UpdateBarker();

            if (HasManager && Vault > 0)
            {
                collectTimer += Time.deltaTime;
                if (collectTimer >= Cfg.managerCollectInterval)
                {
                    collectTimer = 0f;
                    Collect(true);
                }
            }
        }

        void CompleteSale()
        {
            serveTimer = 0f;
            var npc = queue[0];
            queue.RemoveAt(0);

            // Khách chọn ngẫu nhiên một món đã mở, mỗi món một giá.
            int dish = Random.Range(0, DishCount);
            double earned = DishPrice(dish) * npc.PayMultiplier * Game.I.IncomeMultiplier;
            Vault = System.Math.Min(VaultCapacity, Vault + earned);
            earnedWindow += earned;
            Game.I.Data.stats.AddSale(Data.id);
            bounce = 1f;
            dishBounce[dish] = 1f;

            npc.OnServed(DishSprite(dish));
            if (Data.customerLines != null && Data.customerLines.Length > 0 && Random.value < 0.25f)
            {
                npc.Say(Data.customerLines[Random.Range(0, Data.customerLines.Length)]);
            }

            bag.Refresh();
            OnChanged?.Invoke();
        }

        /// <summary>Chuyển tiền trong quỹ quầy về quỹ chung. quiet = người bán tự thu, hiệu ứng nhỏ hơn.</summary>
        public double Collect(bool quiet = false)
        {
            if (Vault <= 0) return 0;
            double amount = Vault;
            Vault = 0;
            Game.I.Economy.Add(amount);
            Game.I.Data.stats.coinsCollected += amount;

            Vector3 at = bag.transform.position + Vector3.up * 0.6f;
            if (quiet)
            {
                FloatingText.Spawn(at, "+" + NumberFormat.Short(amount), new Color(1f, 0.85f, 0.25f), 0.05f);
            }
            else
            {
                Game.I.UI.OnCoinsCollected(at, amount);
                Game.I.Audio.Play(Sfx.Coin);
            }

            bag.Refresh();
            OnChanged?.Invoke();
            return amount;
        }

        /// <summary>Nâng cấp sạp hàng: đổi hình sạp, phục vụ nhanh hơn, quỹ lớn hơn.</summary>
        public bool TryUpgrade()
        {
            if (IsMaxLevel || !Game.I.Economy.TrySpend(NextUpgradeCost)) return false;
            Level++;
            bounce = 1f;
            RefreshVisuals();
            Celebrate("Sạp cấp " + Level + "!", Level >= MaxLevel ? Sfx.Gong : Sfx.Upgrade);
            return true;
        }

        /// <summary>Mở món tiếp theo: món xuất hiện trên mặt quầy, khách bắt đầu mua món này.</summary>
        public bool TryUnlockDish()
        {
            if (AllDishesOpen || !Game.I.Economy.TrySpend(NextDishCost)) return false;
            DishCount++;
            RefreshVisuals();
            dishBounce[DishCount - 1] = 1f;
            Celebrate("Món mới: " + DishName(DishCount - 1) + "!", Sfx.Upgrade);
            return true;
        }

        /// <summary>Nâng doanh thu: tăng giá bán mọi món.</summary>
        public bool TryUpgradeRevenue()
        {
            if (IsMaxRevenue || !Game.I.Economy.TrySpend(NextRevenueCost)) return false;
            RevenueLevel++;
            bag.Refresh();
            Celebrate("Doanh thu +" + Mathf.RoundToInt(Cfg.revenuePerLevel * 100f) + "%", Sfx.Upgrade);
            return true;
        }

        /// <summary>Thuê người bán đứng sau quầy: tự thu tiền và kiếm tiền khi offline.</summary>
        public bool HireManager()
        {
            if (HasManager) return false;
            var data = Game.I.Data;
            if (data.managerTokens > 0) data.managerTokens--;
            else if (!Game.I.Economy.TrySpend(ManagerCost)) return false;
            SetManager(true);
            Celebrate("Có người bán!", Sfx.Chime);
            return true;
        }

        void Celebrate(string text, Sfx sfx)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 3.4f, text, new Color(1f, 0.9f, 0.3f));
            Game.I.Audio.Play(sfx);
            Game.I.Vibrate();
            OnChanged?.Invoke();
        }

        /// <summary>Thuê người chào hàng: đứng góc phải trước quầy, mời khách đi ngang ghé quầy.</summary>
        public bool TryHireBarker()
        {
            if (HasBarker || !Game.I.Economy.TrySpend(BarkerCost)) return false;
            SetBarker(true);
            barkerTimer = 0.5f;
            Celebrate("Có người chào hàng!", Sfx.Chime);
            return true;
        }

        void SetManager(bool on)
        {
            HasManager = on;
            if (on && seller == null)
            {
                // Người bán đứng sau quầy, lộ nửa người qua ô cửa sạp.
                seller = BuildStaff("Seller", -10, new Vector3(0.45f, Data.counterTopY - 0.2f, 0f));
            }
            if (seller != null) seller.gameObject.SetActive(on);
            managerSprite.enabled = on && seller == null;
        }

        void SetBarker(bool on)
        {
            HasBarker = on;
            if (on && barker == null)
            {
                // Người chào hàng đứng ở góc phải phía trước quầy, vẽ trước thân sạp.
                barker = BuildStaff("Barker", 8, new Vector3(1.45f, -0.1f, 0f));
                if (barker != null)
                {
                    barkerAnim = barker.GetComponent<StaffIdle>();
                    var g = Game.I;
                    barkerSpeech = new SpeechBubble(transform, new Vector3(1.45f, 2.0f, 0f), 0.78f, 40, "BarkerSpeech");
                    GetComponent<YSort>()?.Rebase();
                }
            }
            if (barker != null) barker.gameObject.SetActive(on);
            if (!on) barkerSpeech?.Hide();
        }

        /// <summary>Dựng nhân viên của quầy (cùng một bộ hình cho người bán và người chào hàng; thiếu thì lấy một bộ khách).</summary>
        Transform BuildStaff(string name, int orderOffset, Vector3 localPos)
        {
            var art = Game.I.art;
            NpcRig rig = null;
            if (art.staffRigs != null && art.staffRigs.Length > 0)
            {
                rig = System.Array.Find(art.staffRigs, r => r.id == Data.staffRig) ?? art.staffRigs[Slot.Index % art.staffRigs.Length];
            }
            else if (art.npcRigs != null && art.npcRigs.Length > 0) rig = art.npcRigs[(Slot.Index * 7 + 3) % art.npcRigs.Length];
            if (rig == null) return null;
            var root = RigView.Build(transform, rig, orderOffset, name);
            root.localPosition = localPos;
            root.localScale = Vector3.one * (Cfg.npcScale / Mathf.Max(0.01f, Slot.transform.lossyScale.x));
            root.gameObject.AddComponent<StaffIdle>();
            GetComponent<YSort>()?.Rebase();
            return root;
        }

        void UpdateBarker()
        {
            if (barkerSpeech == null) return;
            if (barkerSpeech.Visible && Time.time > barkerSpeechUntil) barkerSpeech.Hide();
            barkerTimer -= Time.deltaTime;
            if (barkerTimer > 0f) return;
            barkerTimer = Cfg.barkerTalkInterval * Random.Range(0.8f, 1.3f);

            barkerSpeech.Show(BarkerLine());
            barkerSpeechUntil = Time.time + 2.6f;
            barkerAnim?.Wave(2.6f);
        }

        string BarkerLine()
        {
            var lines = Data.barkerLines;
            if (lines != null && lines.Length > 0 && Random.value < 0.6f) return lines[Random.Range(0, lines.Length)];
            switch (Random.Range(0, 3))
            {
                case 0: return "Mời ghé " + Data.displayName + " ạ!";
                case 1: return DishName(Random.Range(0, DishCount)) + " ngon lắm đây!";
                default: return "Ghé mua ủng hộ nha!";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Interaction.Request(this);
        }

        public Vector3 InteractPoint => Slot.InteractPoint;

        /// <summary>Đứng gần quầy: mở bảng quầy (4 nâng cấp, thu tiền).</summary>
        public void Interact()
        {
            Game.I.UI.OpenStallPanel(this);
        }
    }
}
