using NUnit.Framework;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.Logic;

namespace Underpin.SlotMachine.Tests
{
    public class PayoutCalculatorTests
    {
        private TestSymbols s;

        [SetUp]
        public void SetUp() => s = new TestSymbols();

        [Test]
        public void ThreeMatchingSymbols_PayBetTimesMultiplier()
        {
            PayoutResult result = PayoutCalculator.Evaluate(new[] { s.Cherry, s.Cherry, s.Cherry }, 10);

            Assert.IsTrue(result.IsWin);
            Assert.AreEqual(40, result.LinePayout);
            Assert.AreSame(s.Cherry, result.WinningSymbol);
        }

        [Test]
        public void AnyMismatch_IsNotAWin()
        {
            Assert.IsFalse(PayoutCalculator.Evaluate(new[] { s.Bar, s.Bar, s.Cherry }, 10).IsWin);
            Assert.IsFalse(PayoutCalculator.Evaluate(new[] { s.Seven, s.Bar, s.Seven }, 10).IsWin);
            Assert.IsFalse(PayoutCalculator.Evaluate(new[] { s.Bell, s.Seven, s.Seven }, 10).IsWin);
        }

        [Test]
        public void ThreeSevens_HitTheJackpot()
        {
            PayoutResult result = PayoutCalculator.Evaluate(new[] { s.Seven, s.Seven, s.Seven }, 100);

            Assert.IsTrue(result.HitsJackpot);
            Assert.AreEqual(5000, result.LinePayout);
        }

        [Test]
        public void ThreeBells_AwardFreeSpins()
        {
            PayoutResult result = PayoutCalculator.Evaluate(new[] { s.Bell, s.Bell, s.Bell }, 10);

            Assert.AreEqual(5, result.FreeSpinsAwarded);
            Assert.AreEqual(100, result.LinePayout);
            Assert.IsFalse(result.HitsJackpot);
        }

        [Test]
        public void ZeroBet_NeverPays()
        {
            Assert.IsFalse(PayoutCalculator.Evaluate(new[] { s.Bar, s.Bar, s.Bar }, 0).IsWin);
        }
    }
}
