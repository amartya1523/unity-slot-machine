using System;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>The player's credit balance. All credit changes go through here.</summary>
    public sealed class CreditWallet
    {
        public int Balance { get; private set; }

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        public CreditWallet(int startingBalance)
        {
            Balance = Math.Max(0, startingBalance);
        }

        public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;

        public bool TryDebit(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Credit(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }
    }
}
