using System;
using UnityEngine;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>
    /// Bonus feature: a pot that grows by a small share of every paid bet and is paid out
    /// in full when three Sevens land. It then resets to its seed value.
    /// </summary>
    public sealed class ProgressiveJackpot
    {
        private readonly int seedAmount;
        private readonly float contributionRate;

        // Fractions of a credit are kept here so small bets still grow the pot correctly.
        private float pendingFraction;

        public int Amount { get; private set; }

        public event Action<int> AmountChanged;

        public ProgressiveJackpot(int seed, float contribution)
        {
            seedAmount = Math.Max(0, seed);
            contributionRate = Mathf.Clamp01(contribution);
            Amount = seedAmount;
        }

        public void Contribute(int bet)
        {
            pendingFraction += bet * contributionRate;
            int whole = (int)pendingFraction;
            if (whole <= 0)
            {
                return;
            }

            pendingFraction -= whole;
            Amount += whole;
            AmountChanged?.Invoke(Amount);
        }

        /// <summary>Pays out the whole pot and resets it.</summary>
        public int Award()
        {
            int won = Amount;
            Amount = seedAmount;
            pendingFraction = 0f;
            AmountChanged?.Invoke(Amount);
            return won;
        }
    }
}
