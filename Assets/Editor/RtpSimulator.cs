using System.Collections.Generic;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.Logic;
using Underpin.SlotMachine.RNG;
using UnityEditor;
using UnityEngine;

namespace Underpin.SlotMachine.EditorTools
{
    /// <summary>
    /// Plays a million spins with the real GameSession and reel strips (no animation) and logs
    /// the Return To Player, hit rate and how often each feature triggers. This is how the
    /// paytable numbers were balanced. Menu: Underpin > Simulate RTP.
    /// </summary>
    public static class RtpSimulator
    {
        private const int Spins = 1_000_000;

        [MenuItem("Underpin/Simulate RTP (1M spins)", priority = 40)]
        public static void Simulate()
        {
            var paytable = AssetDatabase.LoadAssetAtPath<PaytableDefinition>("Assets/Data/Paytable.asset");
            if (paytable == null)
            {
                Debug.LogError("Build the scene first (Underpin > Rebuild Slot Machine Scene).");
                return;
            }

            var strips = new List<ReelStrip>
            {
                new ReelStrip(paytable.Symbols, 1103),
                new ReelStrip(paytable.Symbols, 2207),
                new ReelStrip(paytable.Symbols, 3301)
            };
            var resolver = new SpinResolver(strips, new SeededRandomProvider(12345));
            var session = new GameSession(paytable);

            long wagered = 0;
            long returned = 0;
            int hits = 0;
            int jackpots = 0;
            int freeSpinTriggers = 0;
            var winsBySymbol = new Dictionary<string, int>();

            for (int i = 0; i < Spins; i++)
            {
                if (session.Wallet.Balance < session.CurrentBet && !session.IsInFreeSpins)
                {
                    session.Refill();
                }

                session.TryStartSpin(out SpinTicket ticket);
                if (!ticket.IsFreeSpin)
                {
                    wagered += ticket.Bet;
                }

                SpinSettlement result = session.Settle(ticket, resolver.Resolve());
                if (result.Tier == WinTier.None)
                {
                    continue;
                }

                hits++;
                returned += result.TotalWin;
                if (result.JackpotWon > 0) jackpots++;
                if (result.FreeSpinsAwarded > 0) freeSpinTriggers++;
                string id = result.Payout.WinningSymbol.SymbolId;
                winsBySymbol[id] = winsBySymbol.TryGetValue(id, out int n) ? n + 1 : 1;
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine($"[RTP] {Spins:N0} spins at bet {paytable.BetOptions[0]}");
            report.AppendLine($"  RTP: {100.0 * returned / wagered:F2}%");
            report.AppendLine($"  Hit rate: 1 in {(double)Spins / hits:F1} spins");
            report.AppendLine($"  Jackpots: {jackpots:N0}, free-spin triggers: {freeSpinTriggers:N0}");
            foreach (KeyValuePair<string, int> pair in winsBySymbol)
            {
                report.AppendLine($"  3 x {pair.Key}: {pair.Value:N0}");
            }

            Debug.Log(report.ToString());
        }
    }
}
