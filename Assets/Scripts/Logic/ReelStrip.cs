using System;
using System.Collections.Generic;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.RNG;

namespace Underpin.SlotMachine.Logic
{
    /// <summary>
    /// The looping band of symbols printed on one physical reel.
    ///
    /// Each symbol appears <see cref="SymbolDefinition.ReelWeight"/> times, so a rare symbol
    /// simply has fewer copies on the strip. The RNG then picks a stop position uniformly,
    /// which makes the odds easy to reason about: P(symbol) = weight / strip length.
    ///
    /// The order is shuffled once with a fixed seed so every reel looks different but the
    /// layout stays the same between sessions (just like a real machine).
    /// </summary>
    public sealed class ReelStrip
    {
        private readonly SymbolDefinition[] stops;

        public int Length => stops.Length;

        public ReelStrip(IReadOnlyList<SymbolDefinition> symbols, int layoutSeed)
        {
            if (symbols == null || symbols.Count == 0)
            {
                throw new ArgumentException("A reel strip needs at least one symbol.", nameof(symbols));
            }

            var list = new List<SymbolDefinition>();
            foreach (SymbolDefinition symbol in symbols)
            {
                for (int i = 0; i < symbol.ReelWeight; i++)
                {
                    list.Add(symbol);
                }
            }

            stops = list.ToArray();
            Shuffle(stops, new SeededRandomProvider(layoutSeed));
        }

        /// <summary>Symbol at a stop index. Wraps around in both directions, because the strip is a loop.</summary>
        public SymbolDefinition this[int index] => stops[Wrap(index)];

        public int Wrap(int index)
        {
            int wrapped = index % stops.Length;
            return wrapped < 0 ? wrapped + stops.Length : wrapped;
        }

        /// <summary>All stop indices showing the given symbol. Used by the QA "force outcome" tool.</summary>
        public List<int> IndicesOf(SymbolDefinition symbol)
        {
            var result = new List<int>();
            for (int i = 0; i < stops.Length; i++)
            {
                if (stops[i] == symbol)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        public int CountOf(SymbolDefinition symbol) => IndicesOf(symbol).Count;

        // Fisher-Yates shuffle: every ordering is equally likely.
        private static void Shuffle(SymbolDefinition[] array, IRandomProvider rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
