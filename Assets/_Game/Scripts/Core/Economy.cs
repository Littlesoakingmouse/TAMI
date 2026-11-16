namespace Tami
{
    /// <summary>Quỹ chung của người chơi: Xu và Ngọc (GDD mục 5.1).</summary>
    public class Economy
    {
        public double Coins { get; private set; }
        public int Gems { get; private set; }
        public event System.Action OnChanged;

        public Economy(double coins, int gems)
        {
            Coins = coins;
            Gems = gems;
        }

        public void Set(double coins, int gems)
        {
            Coins = coins;
            Gems = gems;
            OnChanged?.Invoke();
        }

        public bool CanAfford(double cost) => Coins >= cost;
        public bool CanAffordGems(int cost) => Gems >= cost;

        public void Add(double amount)
        {
            Coins += amount;
            OnChanged?.Invoke();
        }

        public bool TrySpend(double cost)
        {
            if (Coins < cost) return false;
            Coins -= cost;
            OnChanged?.Invoke();
            return true;
        }

        public void AddGems(int amount)
        {
            Gems += amount;
            OnChanged?.Invoke();
        }

        public bool TrySpendGems(int cost)
        {
            if (Gems < cost) return false;
            Gems -= cost;
            OnChanged?.Invoke();
            return true;
        }
    }
}
