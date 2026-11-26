using System;
using Underpin.SlotMachine.Data;

namespace Underpin.SlotMachine.Logic
{
    public enum WinTier
    {
        None,
        Win,
        BigWin,
        Jackpot
    }

    /// <summary>A spin that has been paid for (or is free) and is waiting for its result.</summary>
    public readonly struct SpinTicket
    {
        public int Bet { get; }
        public bool IsFreeSpin { get; }

        public SpinTicket(int bet, bool isFreeSpin)
        {
            Bet = bet;
            IsFreeSpin = isFreeSpin;
        }
    }

    /// <summary>Everything the UI needs to show after a spin is settled.</summary>
    public readonly struct SpinSettlement
    {
        public PayoutResult Payout { get; }
        public int JackpotWon { get; }
        public int TotalWin { get; }
        public int FreeSpinsAwarded { get; }
        public WinTier Tier { get; }

        public SpinSettlement(PayoutResult payout, int jackpotWon, int freeSpinsAwarded, WinTier tier)
        {
            Payout = payout;
            JackpotWon = jackpotWon;
            TotalWin = payout.LinePayout + jackpotWon;
            FreeSpinsAwarded = freeSpinsAwarded;
            Tier = tier;
        }
    }

    /// <summary>
    /// The rules of a play session: bets, credits, free spins and the jackpot.
    /// It is plain C# (no MonoBehaviour), so the whole money flow is unit-testable,
    /// and the view layer can't accidentally change credits on its own.
    ///
    /// Flow per spin: TryStartSpin() -> (reels animate) -> Settle().
    /// </summary>
    public sealed class GameSession
    {
        private readonly PaytableDefinition paytable;

        public CreditWallet Wallet { get; }
        public ProgressiveJackpot Jackpot { get; }
        public int BetIndex { get; private set; }
        public int FreeSpinsRemaining { get; private set; }

        /// <summary>Bet that free spins are played at (locked to the bet that triggered them).</summary>
        public int FreeSpinBet { get; private set; }

        public int CurrentBet => paytable.BetOptions[BetIndex];
        public bool IsInFreeSpins => FreeSpinsRemaining > 0;
        public int MinimumBet => paytable.BetOptions[0];

        /// <summary>Raised when the bet or free-spin count changes.</summary>
        public event Action StateChanged;

        public GameSession(PaytableDefinition paytableDefinition)
        {
            paytable = paytableDefinition;
            Wallet = new CreditWallet(paytable.StartingCredits);
            Jackpot = new ProgressiveJackpot(paytable.JackpotSeed, paytable.JackpotContribution);
        }

        /// <summary>Moves through the bet options. Bets are locked during free spins.</summary>
        public bool ChangeBet(int direction)
        {
            if (IsInFreeSpins)
            {
                return false;
            }

            int count = paytable.BetOptions.Count;
            int next = Math.Max(0, Math.Min(count - 1, BetIndex + Math.Sign(direction)));
            if (next == BetIndex)
            {
                return false;
            }

            BetIndex = next;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Takes the bet (or uses up a free spin). Returns false if the player can't afford it.
        /// </summary>
        public bool TryStartSpin(out SpinTicket ticket)
        {
            if (IsInFreeSpins)
            {
                FreeSpinsRemaining--;
                ticket = new SpinTicket(FreeSpinBet, true);
                StateChanged?.Invoke();
                return true;
            }

            int bet = CurrentBet;
            if (!Wallet.TryDebit(bet))
            {
                ticket = default;
                return false;
            }

            Jackpot.Contribute(bet);
            ticket = new SpinTicket(bet, false);
            return true;
        }

        /// <summary>Pays out the spin result and awards any bonus features.</summary>
        public SpinSettlement Settle(SpinTicket ticket, SpinOutcome outcome)
        {
            PayoutResult payout = PayoutCalculator.Evaluate(outcome.PaylineSymbols, ticket.Bet);
            if (!payout.IsWin)
            {
                return new SpinSettlement(payout, 0, 0, WinTier.None);
            }

            int jackpotWon = payout.HitsJackpot ? Jackpot.Award() : 0;

            if (payout.FreeSpinsAwarded > 0)
            {
                // Retriggers add to the remaining count; the original bet stays locked.
                if (!IsInFreeSpins)
                {
                    FreeSpinBet = ticket.Bet;
                }

                FreeSpinsRemaining += payout.FreeSpinsAwarded;
                StateChanged?.Invoke();
            }

            Wallet.Credit(payout.LinePayout + jackpotWon);

            WinTier tier = payout.HitsJackpot ? WinTier.Jackpot
                : payout.LinePayout >= ticket.Bet * paytable.BigWinThreshold ? WinTier.BigWin
                : WinTier.Win;

            return new SpinSettlement(payout, jackpotWon, payout.FreeSpinsAwarded, tier);
        }

        /// <summary>Gives the player a fresh stack of credits when they run out (demo game, no real money).</summary>
        public void Refill()
        {
            Wallet.Credit(paytable.StartingCredits);
        }
    }
}
