using System.Collections.Generic;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.RNG;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>
    /// Picks where each reel stops. Every reel is independent and every stop on a strip is
    /// equally likely, so the chance of a symbol is exactly its share of the strip.
    /// </summary>
    public sealed class SpinResolver
    {
        private readonly IReadOnlyList<ReelStrip> strips;
        private readonly IRandomProvider rng;

        // QA tool only: when set, the next spin lands this symbol on every reel.
        private SymbolDefinition forcedSymbol;

        public IReadOnlyList<ReelStrip> Strips => strips;

        public SpinResolver(IReadOnlyList<ReelStrip> reelStrips, IRandomProvider randomProvider)
        {
            strips = reelStrips;
            rng = randomProvider;
        }

        /// <summary>
        /// Forces the next spin to land three of <paramref name="symbol"/>. Used only by the
        /// QA panel to show bonus features on demand; normal play never calls it.
        /// </summary>
        public void ForceNextOutcome(SymbolDefinition symbol)
        {
            forcedSymbol = symbol;
        }

        public SpinOutcome Resolve()
        {
            int reelCount = strips.Count;
            var stops = new int[reelCount];
            var symbols = new SymbolDefinition[reelCount];

            for (int reel = 0; reel < reelCount; reel++)
            {
                ReelStrip strip = strips[reel];
                stops[reel] = forcedSymbol != null ? PickForcedStop(strip, forcedSymbol) : rng.Range(0, strip.Length);
                symbols[reel] = strip[stops[reel]];
            }

            forcedSymbol = null;
            return new SpinOutcome(stops, symbols);
        }

        private int PickForcedStop(ReelStrip strip, SymbolDefinition symbol)
        {
            List<int> candidates = strip.IndicesOf(symbol);
            return candidates.Count == 0 ? rng.Range(0, strip.Length) : candidates[rng.Range(0, candidates.Count)];
        }
    }
}
