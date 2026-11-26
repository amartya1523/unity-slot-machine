using System.Collections;
using Underpin.SlotMachine.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// Celebrates a win: title by tier (WIN / BIG WIN / JACKPOT), a counting-up amount,
    /// and a detail line. Closes on click or by itself after a short time.
    /// </summary>
    public sealed class WinPopupView : PopupView
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text amountText;
        [SerializeField] private Text detailText;
        [SerializeField] private Image symbolIcon;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("Plays the looping title glow clip from Assets/Animations.")]
        private Animation titleAnimation;
        [SerializeField, Min(0.1f)] private float countDuration = 1.1f;
        [SerializeField, Min(0.5f)] private float autoCloseAfter = 3.2f;

        private static readonly Color WinColour = new Color(1f, 0.85f, 0.3f);
        private static readonly Color BigWinColour = new Color(1f, 0.55f, 0.15f);
        private static readonly Color JackpotColour = new Color(1f, 0.3f, 0.25f);

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }

        /// <summary>Shows the popup and waits until it is closed (click or timeout).</summary>
        public IEnumerator ShowAndWait(SpinSettlement settlement, bool quick)
        {
            titleText.text = settlement.Tier == WinTier.Jackpot ? "JACKPOT!"
                : settlement.Tier == WinTier.BigWin ? "BIG WIN!"
                : "WIN!";
            titleText.color = settlement.Tier == WinTier.Jackpot ? JackpotColour
                : settlement.Tier == WinTier.BigWin ? BigWinColour
                : WinColour;

            symbolIcon.sprite = settlement.Payout.WinningSymbol.Sprite;
            detailText.text = BuildDetail(settlement);
            amountText.text = "0";

            Open();
            if (titleAnimation != null)
            {
                titleAnimation.Play();
            }

            float elapsed = 0f;
            float limit = quick ? autoCloseAfter * 0.6f : autoCloseAfter;
            while (IsOpen && elapsed < limit)
            {
                elapsed += Time.deltaTime;
                int shown = Mathf.RoundToInt(settlement.TotalWin * Mathf.Clamp01(elapsed / countDuration));
                amountText.text = shown.ToString("N0");
                yield return null;
            }

            amountText.text = settlement.TotalWin.ToString("N0");
            Close();

            // Wait for the fade-out to finish so the next spin doesn't start under the popup.
            yield return new WaitForSeconds(0.2f);
        }

        private static string BuildDetail(SpinSettlement s)
        {
            string line = $"3 x {s.Payout.WinningSymbol.DisplayName} pays {s.Payout.LinePayout:N0}";
            if (s.JackpotWon > 0)
            {
                line += $"\n+ progressive jackpot {s.JackpotWon:N0}";
            }

            if (s.FreeSpinsAwarded > 0)
            {
                line += $"\n+ {s.FreeSpinsAwarded} FREE SPINS";
            }

            return line;
        }
    }
}
