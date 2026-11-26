using UnityEngine;

namespace Underpin.SlotMachine.Data
{
    /// <summary>
    /// What a symbol does when three of it land on the payline, on top of its normal payout.
    /// </summary>
    public enum SymbolRole
    {
        /// <summary>Pays its multiplier, nothing else.</summary>
        Standard,

        /// <summary>Pays its multiplier and also awards the progressive jackpot pot.</summary>
        Jackpot,

        /// <summary>Pays its multiplier and also awards free spins.</summary>
        FreeSpins
    }

    /// <summary>
    /// One symbol on the reels. All tuning (how often it shows up, what it pays)
    /// lives here so a designer can balance the game without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "Symbol", menuName = "Slot Machine/Symbol Definition")]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used in logs and tests.")]
        private string symbolId = "symbol";

        [SerializeField]
        private string displayName = "Symbol";

        [SerializeField]
        private Sprite sprite;

        [SerializeField, Min(1), Tooltip("How many copies of this symbol are placed on every reel strip. Higher = more common.")]
        private int reelWeight = 1;

        [SerializeField, Min(0), Tooltip("Payout for three of a kind, as a multiple of the bet.")]
        private int threeOfAKindMultiplier = 1;

        [SerializeField]
        private SymbolRole role = SymbolRole.Standard;

        [SerializeField, Min(0), Tooltip("Free spins awarded on three of a kind (only used when Role = FreeSpins).")]
        private int freeSpinsAwarded;

        public string SymbolId => symbolId;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public int ReelWeight => reelWeight;
        public int ThreeOfAKindMultiplier => threeOfAKindMultiplier;
        public SymbolRole Role => role;
        public int FreeSpinsAwarded => role == SymbolRole.FreeSpins ? freeSpinsAwarded : 0;

        /// <summary>
        /// Sets every field in one go. Used by the editor scene builder and by unit tests,
        /// which create symbols in code instead of loading assets.
        /// </summary>
        public void Configure(string id, string nameForDisplay, Sprite symbolSprite, int weight,
            int multiplier, SymbolRole symbolRole = SymbolRole.Standard, int freeSpins = 0)
        {
            symbolId = id;
            displayName = nameForDisplay;
            sprite = symbolSprite;
            reelWeight = Mathf.Max(1, weight);
            threeOfAKindMultiplier = Mathf.Max(0, multiplier);
            role = symbolRole;
            freeSpinsAwarded = Mathf.Max(0, freeSpins);
        }

        public override string ToString() => symbolId;
    }
}
