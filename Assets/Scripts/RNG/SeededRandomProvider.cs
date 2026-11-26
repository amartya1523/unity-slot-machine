using System;

namespace Underpin.SlotMachine.RNG
{
    /// <summary>
    /// Repeatable RNG. The same seed always gives the same sequence, which is what we want for
    /// unit tests, the RTP simulator and for building the fixed reel strip layouts.
    /// Never used to pick live spin results.
    /// </summary>
    public sealed class SeededRandomProvider : IRandomProvider
    {
        private readonly Random random;

        public SeededRandomProvider(int seed)
        {
            random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
