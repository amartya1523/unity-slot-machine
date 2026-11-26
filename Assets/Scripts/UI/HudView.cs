using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// Read-only displays: credits, bet, jackpot, free spins, last win and the message line.
    /// It only shows values; it never changes game state.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private Text creditsText;
        [SerializeField] private Text betText;
        [SerializeField] private Text jackpotText;
        [SerializeField] private Text lastWinText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject freeSpinsBadge;
        [SerializeField] private Text freeSpinsText;
        [SerializeField, Min(0.05f)] private float countUpDuration = 0.8f;

        private int shownCredits;
        private Coroutine creditsRoutine;

        /// <summary>Optional callback fired on each count-up step (used for the coin tick sound).</summary>
        public System.Action CountUpTick;

        public void SetCredits(int credits, bool animate)
        {
            if (creditsRoutine != null)
            {
                StopCoroutine(creditsRoutine);
                creditsRoutine = null;
            }

            if (!animate || !isActiveAndEnabled)
            {
                shownCredits = credits;
                creditsText.text = credits.ToString("N0");
                return;
            }

            creditsRoutine = StartCoroutine(CountUp(shownCredits, credits));
        }

        public void SetBet(int bet, bool locked)
        {
            betText.text = locked ? $"{bet} (locked)" : bet.ToString("N0");
        }

        public void SetJackpot(int amount) => jackpotText.text = amount.ToString("N0");

        public void SetLastWin(int amount) => lastWinText.text = amount > 0 ? $"WIN {amount:N0}" : "WIN 0";

        public void SetFreeSpins(int remaining)
        {
            freeSpinsBadge.SetActive(remaining > 0);
            freeSpinsText.text = $"FREE SPINS: {remaining}";
        }

        public void ShowMessage(string message) => messageText.text = message;

        private IEnumerator CountUp(int from, int to)
        {
            float elapsed = 0f;
            int lastShown = from;
            float nextTick = 0f;
            while (elapsed < countUpDuration)
            {
                elapsed += Time.deltaTime;
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / countUpDuration));
                if (value != lastShown)
                {
                    lastShown = value;
                    creditsText.text = value.ToString("N0");

                    // Throttle the tick sound so it reads as coins, not as noise.
                    if (elapsed >= nextTick)
                    {
                        nextTick = elapsed + 0.07f;
                        CountUpTick?.Invoke();
                    }
                }

                yield return null;
            }

            shownCredits = to;
            creditsText.text = to.ToString("N0");
            creditsRoutine = null;
        }
    }
}
