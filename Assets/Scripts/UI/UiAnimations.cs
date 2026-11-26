using System.Collections;
using UnityEngine;

namespace Underpin.SlotMachine.UI
{
    /// <summary>Small reusable UI tweens, written as coroutines to avoid a tween library dependency.</summary>
    public static class UiAnimations
    {
        /// <summary>Back-out easing: overshoots a little then settles. Feels "poppy" for popups.</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        public static IEnumerator PopIn(RectTransform target, CanvasGroup group, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(t));
                target.localScale = new Vector3(scale, scale, 1f);
                group.alpha = Mathf.Clamp01(t * 2f);
                yield return null;
            }

            target.localScale = Vector3.one;
            group.alpha = 1f;
        }

        public static IEnumerator FadeOut(CanvasGroup group, float duration)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }

            group.alpha = 0f;
        }
    }
}
