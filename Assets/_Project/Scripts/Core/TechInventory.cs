namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// Running total of alien tech the hillbilly has scavenged this session.
    /// Packet 2.1 only collects and counts it; the upgrade system (Packet 2.3)
    /// will spend it. Reset by the GameManager on scene load.
    /// </summary>
    public static class TechInventory
    {
        public static int Count { get; private set; }

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Count += amount;
            GameEvents.RaiseTechChanged(Count);
        }

        /// <summary>Spend tech if affordable. Returns false (and spends nothing) otherwise.</summary>
        public static bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Count) return false;
            Count -= amount;
            GameEvents.RaiseTechChanged(Count);
            return true;
        }

        public static void Reset()
        {
            Count = 0;
        }
    }
}
