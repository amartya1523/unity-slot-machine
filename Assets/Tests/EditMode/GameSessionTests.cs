using NUnit.Framework;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.Logic;

namespace Underpin.SlotMachine.Tests
{
    public class GameSessionTests
    {
        private TestSymbols s;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            s = new TestSymbols();
            session = new GameSession(s.Paytable());
        }

        private static SpinOutcome Line(SymbolDefinition a, SymbolDefinition b, SymbolDefinition c)
            => new SpinOutcome(new[] { 0, 0, 0 }, new[] { a, b, c });

        [Test]
        public void StartingSpin_TakesTheBet()
        {
            Assert.IsTrue(session.TryStartSpin(out SpinTicket ticket));
            Assert.AreEqual(10, ticket.Bet);
            Assert.AreEqual(990, session.Wallet.Balance);
        }

        [Test]
        public void Win_IsCreditedToTheWallet()
        {
            session.TryStartSpin(out SpinTicket ticket);
            SpinSettlement result = session.Settle(ticket, Line(s.Bar, s.Bar, s.Bar));

            Assert.AreEqual(250, result.TotalWin);
            Assert.AreEqual(990 + 250, session.Wallet.Balance);
            Assert.AreEqual(WinTier.BigWin, result.Tier);
        }

        [Test]
        public void CannotSpin_WithoutEnoughCredits()
        {
            session.ChangeBet(+1);
            session.ChangeBet(+1); // bet 100
            for (int i = 0; i < 10; i++)
            {
                session.TryStartSpin(out _);
            }

            Assert.AreEqual(0, session.Wallet.Balance);
            Assert.IsFalse(session.TryStartSpin(out _));
        }

        [Test]
        public void ThreeSevens_PayTheJackpotPotAndResetIt()
        {
            session.TryStartSpin(out SpinTicket ticket); // pot grows by 5% of 10 = 0.5 -> stays 1000
            SpinSettlement result = session.Settle(ticket, Line(s.Seven, s.Seven, s.Seven));

            Assert.AreEqual(WinTier.Jackpot, result.Tier);
            Assert.AreEqual(1000, result.JackpotWon);
            Assert.AreEqual(500 + 1000, result.TotalWin);
            Assert.AreEqual(1000, session.Jackpot.Amount);
        }

        [Test]
        public void JackpotPot_GrowsWithPaidBets()
        {
            session.ChangeBet(+1);
            session.ChangeBet(+1); // bet 100 -> 5 credits per spin into the pot
            session.TryStartSpin(out _);
            session.TryStartSpin(out _);

            Assert.AreEqual(1010, session.Jackpot.Amount);
        }

        [Test]
        public void ThreeBells_StartFreeSpinsThatCostNothing()
        {
            session.TryStartSpin(out SpinTicket ticket);
            session.Settle(ticket, Line(s.Bell, s.Bell, s.Bell));
            int balanceAfterWin = session.Wallet.Balance;

            Assert.AreEqual(5, session.FreeSpinsRemaining);
            Assert.IsTrue(session.TryStartSpin(out SpinTicket free));
            Assert.IsTrue(free.IsFreeSpin);
            Assert.AreEqual(10, free.Bet);
            Assert.AreEqual(balanceAfterWin, session.Wallet.Balance);
            Assert.AreEqual(4, session.FreeSpinsRemaining);
        }

        [Test]
        public void BetIsLocked_DuringFreeSpins()
        {
            session.TryStartSpin(out SpinTicket ticket);
            session.Settle(ticket, Line(s.Bell, s.Bell, s.Bell));

            Assert.IsFalse(session.ChangeBet(+1));
        }
    }
}
