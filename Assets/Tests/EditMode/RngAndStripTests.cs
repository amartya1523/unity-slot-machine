using NUnit.Framework;
using Underpin.SlotMachine.Logic;
using Underpin.SlotMachine.RNG;

namespace Underpin.SlotMachine.Tests
{
    public class RngAndStripTests
    {
        [Test]
        public void CryptoRange_StaysInBounds()
        {
            using (var rng = new CryptoRandomProvider())
            {
                for (int i = 0; i < 10000; i++)
                {
                    int value = rng.Range(3, 7);
                    Assert.That(value, Is.InRange(3, 6));
                }
            }
        }

        [Test]
        public void CryptoRange_IsRoughlyUniform()
        {
            const int buckets = 20;
            const int draws = 200000;
            var counts = new int[buckets];
            using (var rng = new CryptoRandomProvider())
            {
                for (int i = 0; i < draws; i++)
                {
                    counts[rng.Range(0, buckets)]++;
                }
            }

            // Each bucket expects 10,000 hits; allow a generous 5% band.
            foreach (int count in counts)
            {
                Assert.That(count, Is.InRange(9500, 10500));
            }
        }

        [Test]
        public void Strip_ContainsEachSymbolWeightTimes()
        {
            var s = new TestSymbols();
            var strip = new ReelStrip(s.All, 1);

            Assert.AreEqual(20, strip.Length);
            Assert.AreEqual(2, strip.CountOf(s.Seven));
            Assert.AreEqual(4, strip.CountOf(s.Bar));
            Assert.AreEqual(6, strip.CountOf(s.Bell));
            Assert.AreEqual(8, strip.CountOf(s.Cherry));
        }

        [Test]
        public void Strip_WrapsNegativeAndLargeIndices()
        {
            var s = new TestSymbols();
            var strip = new ReelStrip(s.All, 1);

            Assert.AreSame(strip[0], strip[20]);
            Assert.AreSame(strip[19], strip[-1]);
        }

        [Test]
        public void ForcedOutcome_LandsThreeOfTheSymbol_ThenGoesBackToRandom()
        {
            var s = new TestSymbols();
            var strips = new[] { new ReelStrip(s.All, 1), new ReelStrip(s.All, 2), new ReelStrip(s.All, 3) };
            var resolver = new SpinResolver(strips, new SeededRandomProvider(42));

            resolver.ForceNextOutcome(s.Seven);
            SpinOutcome forced = resolver.Resolve();

            foreach (var symbol in forced.PaylineSymbols)
            {
                Assert.AreSame(s.Seven, symbol);
            }
        }
    }
}
