using System.Collections.Generic;
using Underpin.SlotMachine.Data;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>
    /// Result of one spin, decided by the RNG before any reel starts moving.
    /// The reels only animate towards these stop positions; animation never changes the result.
    /// </summary>
    public sealed class SpinOutcome
    {
        public IReadOnlyList<int> StopIndices { get; }
        public IReadOnlyList<SymbolDefinition> PaylineSymbols { get; }

        public SpinOutcome(int[] stopIndices, SymbolDefinition[] paylineSymbols)
        {
            StopIndices = stopIndices;
            PaylineSymbols = paylineSymbols;
        }

        public override string ToString() => string.Join(" | ", PaylineSymbols);
    }
}
