using System.Collections;
using System.Collections.Generic;
using Underpin.SlotMachine.Audio;
using Underpin.SlotMachine.Data;
using Underpin.SlotMachine.Logic;
using Underpin.SlotMachine.Reels;
using Underpin.SlotMachine.RNG;
using Underpin.SlotMachine.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Machine
{
    /// <summary>
    /// The conductor. It owns no rules itself:
    ///  - GameSession decides money, bets and bonuses,
    ///  - SpinResolver decides where the reels stop (crypto RNG),
    ///  - ReelView animates, the UI views display.
    /// This class just runs the spin sequence in the right order and keeps the
    /// buttons locked while a spin is in progress.
    /// </summary>
    public sealed class SlotMachineController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PaytableDefinition paytable;
        [SerializeField, Tooltip("One seed per reel for the fixed strip layout (not for results).")]
        private int[] reelLayoutSeeds = { 1103, 2207, 3301 };

        [Header("Machine")]
        [SerializeField] private ReelView[] reels = new ReelView[PaytableDefinition.ReelCount];
        [SerializeField] private LeverView lever;
        [SerializeField] private WinFlashView winFlash;

        [Header("UI")]
        [SerializeField] private HudView hud;
        [SerializeField] private WinPopupView winPopup;
        [SerializeField] private ConfirmDialogView confirmDialog;
        [SerializeField] private PaytableView paytableView;
        [SerializeField] private QaPanelView qaPanel;
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button paytableButton;

        [Header("Audio")]
        [SerializeField] private SfxPlayer sfx;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] private float reelStartStagger = 0.07f;
        [SerializeField, Min(0.1f)] private float firstStopDelay = 0.85f;
        [SerializeField, Min(0.05f)] private float stopStagger = 0.38f;
        [SerializeField, Min(0f), Tooltip("Extra spin time on the last reel when the first two already match.")]
        private float nearMissExtraDelay = 1.0f;
        [SerializeField, Min(0f)] private float freeSpinAutoDelay = 0.7f;

        private GameSession session;
        private SpinResolver resolver;
        private CryptoRandomProvider rng;
        private bool spinInProgress;

        private void Awake()
        {
            rng = new CryptoRandomProvider();
            session = new GameSession(paytable);

            var strips = new List<ReelStrip>();
            for (int i = 0; i < reels.Length; i++)
            {
                var strip = new ReelStrip(paytable.Symbols, reelLayoutSeeds[i % reelLayoutSeeds.Length]);
                strips.Add(strip);
                reels[i].Initialise(strip, rng.Range(0, strip.Length));
                reels[i].Landed += _ => sfx.PlayReelStop();
            }

            resolver = new SpinResolver(strips, rng);

            session.Wallet.BalanceChanged += balance => hud.SetCredits(balance, true);
            session.Jackpot.AmountChanged += hud.SetJackpot;
            session.StateChanged += RefreshBetAndFreeSpins;
            hud.CountUpTick = sfx.PlayCoinTick;

            spinButton.onClick.AddListener(RequestSpin);
            lever.Pulled += RequestSpin;
            betUpButton.onClick.AddListener(() => ChangeBet(+1));
            betDownButton.onClick.AddListener(() => ChangeBet(-1));
            paytableButton.onClick.AddListener(() =>
            {
                sfx.PlayClick();
                paytableView.Show();
            });
            qaPanel.ForceRequested += symbol =>
            {
                resolver.ForceNextOutcome(symbol);
                hud.ShowMessage($"QA: next spin forced to 3 x {symbol.DisplayName}");
            };
        }

        private void Start()
        {
            hud.SetCredits(session.Wallet.Balance, false);
            hud.SetJackpot(session.Jackpot.Amount);
            hud.SetLastWin(0);
            RefreshBetAndFreeSpins();
            hud.ShowMessage("Pull the lever or press SPIN!");
            if (!rng.IsUsingCryptoSource)
            {
                Debug.LogWarning("Crypto RNG not available on this platform, using seeded System.Random fallback.");
            }
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RequestSpin();
            }
#endif
        }

        private void OnDestroy()
        {
            rng?.Dispose();
        }

        public void RequestSpin()
        {
            if (spinInProgress || AnyPopupOpen())
            {
                return;
            }

            StartCoroutine(SpinSequence());
        }

        private IEnumerator SpinSequence()
        {
            if (!session.TryStartSpin(out SpinTicket ticket))
            {
                HandleCannotAfford();
                yield break;
            }

            spinInProgress = true;
            SetControlsInteractable(false);
            hud.SetLastWin(0);

            // The result is fixed right here, before anything moves.
            SpinOutcome outcome = resolver.Resolve();

            lever.PlayPull();
            sfx.PlayLever();
            sfx.StartSpinLoop();
            hud.ShowMessage(ticket.IsFreeSpin
                ? $"FREE SPIN! {session.FreeSpinsRemaining} left after this one"
                : "Good luck!");

            foreach (ReelView reel in reels)
            {
                reel.StartSpin();
                yield return new WaitForSeconds(reelStartStagger);
            }

            yield return new WaitForSeconds(firstStopDelay);

            for (int i = 0; i < reels.Length; i++)
            {
                bool isLastReel = i == reels.Length - 1;
                if (isLastReel && IsNearMiss(outcome))
                {
                    // Classic anticipation: the first reels match, so let the last one run longer.
                    hud.ShowMessage("So close...!");
                    yield return new WaitForSeconds(nearMissExtraDelay);
                }

                reels[i].RequestStop(outcome.StopIndices[i]);
                if (!isLastReel)
                {
                    yield return new WaitForSeconds(stopStagger);
                }
            }

            foreach (ReelView reel in reels)
            {
                while (reel.IsSpinning)
                {
                    yield return null;
                }
            }

            sfx.StopSpinLoop();

            SpinSettlement settlement = session.Settle(ticket, outcome);
            yield return PresentResult(settlement);

            spinInProgress = false;

            if (session.IsInFreeSpins)
            {
                // Free spins play themselves.
                yield return new WaitForSeconds(freeSpinAutoDelay);
                RequestSpin();
                yield break;
            }

            SetControlsInteractable(true);
            if (session.Wallet.Balance < session.MinimumBet)
            {
                HandleCannotAfford();
            }
        }

        private IEnumerator PresentResult(SpinSettlement settlement)
        {
            if (settlement.Tier == WinTier.None)
            {
                hud.ShowMessage(session.IsInFreeSpins ? "No win - next free spin coming" : "No luck this time. Spin again!");
                yield break;
            }

            hud.SetLastWin(settlement.TotalWin);
            foreach (ReelView reel in reels)
            {
                reel.PlayWinPulse();
            }

            winFlash.Flash(settlement.Tier);

            if (settlement.Tier == WinTier.Jackpot || settlement.Tier == WinTier.BigWin)
            {
                sfx.PlayJackpot();
            }
            else
            {
                sfx.PlaySmallWin();
            }

            if (settlement.FreeSpinsAwarded > 0)
            {
                sfx.PlayFreeSpins();
            }

            hud.ShowMessage(settlement.FreeSpinsAwarded > 0
                ? $"{settlement.FreeSpinsAwarded} FREE SPINS awarded!"
                : $"You won {settlement.TotalWin:N0}!");

            yield return winPopup.ShowAndWait(settlement, session.IsInFreeSpins);
        }

        private void HandleCannotAfford()
        {
            if (session.Wallet.Balance >= session.MinimumBet)
            {
                hud.ShowMessage("Not enough credits for this bet - lower it with the arrows.");
                return;
            }

            confirmDialog.Show(
                "Out of credits!\nTake another 1,000 free credits?",
                () =>
                {
                    session.Refill();
                    hud.ShowMessage("Credits refilled. Good luck!");
                },
                () => hud.ShowMessage("Come back any time!"));
        }

        private void ChangeBet(int direction)
        {
            if (spinInProgress)
            {
                return;
            }

            if (session.ChangeBet(direction))
            {
                sfx.PlayClick();
            }
        }

        private void RefreshBetAndFreeSpins()
        {
            bool locked = session.IsInFreeSpins;
            hud.SetBet(locked ? session.FreeSpinBet : session.CurrentBet, locked);
            hud.SetFreeSpins(session.FreeSpinsRemaining);
            UpdateBetButtons();
        }

        private void SetControlsInteractable(bool interactable)
        {
            spinButton.interactable = interactable;
            paytableButton.interactable = interactable;
            lever.SetInteractable(interactable);
            UpdateBetButtons(interactable);
        }

        private void UpdateBetButtons(bool controlsEnabled = true)
        {
            bool canChange = controlsEnabled && !spinInProgress && !session.IsInFreeSpins;
            betDownButton.interactable = canChange && session.BetIndex > 0;
            betUpButton.interactable = canChange && session.BetIndex < paytable.BetOptions.Count - 1;
        }

        /// <summary>True when every reel except the last already shows the same symbol.</summary>
        private static bool IsNearMiss(SpinOutcome outcome)
        {
            IReadOnlyList<SymbolDefinition> line = outcome.PaylineSymbols;
            for (int i = 1; i < line.Count - 1; i++)
            {
                if (line[i] != line[0])
                {
                    return false;
                }
            }

            return line.Count > 1;
        }

        private bool AnyPopupOpen() => winPopup.IsOpen || confirmDialog.IsOpen || paytableView.IsOpen;
    }
}
