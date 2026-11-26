using System.Collections.Generic;
using Underpin.SlotMachine.Data;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>What a single payline is worth, before credits are actually moved.</summary>
    public readonly struct PayoutResult
    {
        public static readonly PayoutResult NoWin = new PayoutResult(null, 0, false, 0);

        public SymbolDefinition WinningSymbol { get; }
        public int LinePayout { get; }
        public bool HitsJackpot { get; }
        public int FreeSpinsAwarded { get; }
        public bool IsWin => WinningSymbol != null;

        public PayoutResult(SymbolDefinition winningSymbol, int linePayout, bool hitsJackpot, int freeSpinsAwarded)
        {
            WinningSymbol = winningSymbol;
            LinePayout = linePayout;
            HitsJackpot = hitsJackpot;
            FreeSpinsAwarded = freeSpinsAwarded;
        }
    }

    /// <summary>
    /// Pure win rules, no Unity objects or state, so it is easy to unit test.
    ///
    /// Rule (from the brief): the player wins only when every reel shows the same symbol
    /// on the payline. The payout is bet x that symbol's multiplier. Special symbols
    /// (jackpot, free spins) add their bonus on top.
    /// </summary>
    public static class PayoutCalculator
    {
        public static PayoutResult Evaluate(IReadOnlyList<SymbolDefinition> payline, int bet)
        {
            if (payline == null || payline.Count == 0 || bet <= 0)
            {
                return PayoutResult.NoWin;
            }

            SymbolDefinition first = payline[0];
            for (int i = 1; i < payline.Count; i++)
            {
                if (payline[i] != first)
                {
                    return PayoutResult.NoWin;
                }
            }

            return new PayoutResult(
                first,
                bet * first.ThreeOfAKindMultiplier,
                first.Role == SymbolRole.Jackpot,
                first.FreeSpinsAwarded);
        }
    }
}
