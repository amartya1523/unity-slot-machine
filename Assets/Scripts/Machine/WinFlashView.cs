using System.Collections;
using Underpin.SlotMachine.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Machine
{
    /// <summary>
    /// Flashes a glow over the reel windows on a win. Bigger wins flash longer and hotter.
    /// </summary>
    public sealed class WinFlashView : MonoBehaviour
    {
        [SerializeField] private Image glow;
        [SerializeField, Min(0.05f)] private float flashPeriod = 0.25f;

        private Coroutine flashRoutine;

        private void Awake()
        {
            SetAlpha(0f);
        }

        public void Flash(WinTier tier)
        {
            if (tier == WinTier.None)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            int flashes = tier == WinTier.Jackpot ? 10 : tier == WinTier.BigWin ? 6 : 3;
            Color colour = tier == WinTier.Jackpot ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.3f);
            flashRoutine = StartCoroutine(FlashRoutine(flashes, colour));
        }

        private IEnumerator FlashRoutine(int flashes, Color colour)
        {
            glow.color = new Color(colour.r, colour.g, colour.b, 0f);
            for (int n = 0; n < flashes; n++)
            {
                float elapsed = 0f;
                while (elapsed < flashPeriod)
                {
                    elapsed += Time.deltaTime;
                    SetAlpha(0.45f * Mathf.Sin(Mathf.Clamp01(elapsed / flashPeriod) * Mathf.PI));
                    yield return null;
                }
            }

            SetAlpha(0f);
            flashRoutine = null;
        }

        private void SetAlpha(float alpha)
        {
            Color c = glow.color;
            c.a = alpha;
            glow.color = c;
        }
    }
}
