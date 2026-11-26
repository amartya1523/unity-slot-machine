using System.Collections.Generic;
using UnityEngine;

namespace Underpin.SlotMachine.Data
{
    /// <summary>
    /// Game-wide balance settings: which symbols exist, the bets the player can pick,
    /// starting credits and the progressive jackpot rules.
    /// </summary>
    [CreateAssetMenu(fileName = "Paytable", menuName = "Slot Machine/Paytable Definition")]
    public sealed class PaytableDefinition : ScriptableObject
    {
        public const int ReelCount = 3;

        [SerializeField]
        private List<SymbolDefinition> symbols = new List<SymbolDefinition>();

        [SerializeField, Tooltip("Bets the player can cycle through with the arrow buttons.")]
        private int[] betOptions = { 10, 50, 100 };

        [SerializeField, Min(1)]
        private int startingCredits = 1000;

        [SerializeField, Min(0), Tooltip("Value the jackpot pot starts at (and resets to after it is won).")]
        private int jackpotSeed = 250;

        [SerializeField, Range(0f, 0.25f), Tooltip("Share of every paid bet that goes into the jackpot pot.")]
        private float jackpotContribution = 0.05f;

        [SerializeField, Min(1), Tooltip("A win of at least this many times the bet counts as a Big Win.")]
        private int bigWinThreshold = 20;

        public IReadOnlyList<SymbolDefinition> Symbols => symbols;
        public IReadOnlyList<int> BetOptions => betOptions;
        public int StartingCredits => startingCredits;
        public int JackpotSeed => jackpotSeed;
        public float JackpotContribution => jackpotContribution;
        public int BigWinThreshold => bigWinThreshold;

        /// <summary>Used by the editor scene builder and by tests.</summary>
        public void Configure(List<SymbolDefinition> symbolList, int[] bets, int credits,
            int seed, float contribution, int bigWinMultiplier)
        {
            symbols = symbolList;
            betOptions = bets;
            startingCredits = credits;
            jackpotSeed = seed;
            jackpotContribution = contribution;
            bigWinThreshold = bigWinMultiplier;
        }

        private void OnValidate()
        {
            if (betOptions == null || betOptions.Length == 0)
            {
                betOptions = new[] { 10 };
            }
        }
    }
}
