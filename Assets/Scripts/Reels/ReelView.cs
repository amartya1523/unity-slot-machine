using System;
using System.Collections;
using Underpin.SlotMachine.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Reels
{
    /// <summary>
    /// Draws and animates one reel.
    ///
    /// How it works: the reel has a single float, <c>position</c>, measured in symbols along
    /// its strip. A handful of Image "cells" are recycled to show the symbols around that
    /// position, inside a masked window. Moving the reel = changing <c>position</c>; the cells
    /// are re-laid out every frame. This keeps the reel endless with only 5 Images.
    ///
    /// When position is a whole number N, strip[N] sits exactly on the payline.
    /// Increasing position scrolls the symbols downward, like a real slot reel.
    ///
    /// The result is decided before the spin (see SpinResolver); this class only animates
    /// to that stop and never changes the outcome.
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        [SerializeField, Tooltip("Recycled symbol images, ordered bottom to top. Middle one is the payline.")]
        private Image[] cells = Array.Empty<Image>();

        [SerializeField, Tooltip("Distance between two symbol centres, in UI units.")]
        private float symbolPitch = 104f;

        [SerializeField]
        private ReelAnimationProfile profile;

        private ReelStrip strip;
        private float position;
        private float velocity;
        private bool stopRequested;
        private int targetStopIndex;
        private Coroutine spinRoutine;
        private Coroutine pulseRoutine;

        public bool IsSpinning { get; private set; }

        /// <summary>Raised the moment the reel lands (before the settle bounce finishes), for the "thunk" sound.</summary>
        public event Action<ReelView> Landed;

        /// <summary>Raised once the reel has fully stopped.</summary>
        public event Action<ReelView> Stopped;

        private int HalfCells => cells.Length / 2;

        public void Initialise(ReelStrip reelStrip, int startIndex)
        {
            strip = reelStrip;
            position = startIndex;
            velocity = 0f;
            Render();
        }

        public void StartSpin()
        {
            if (IsSpinning || strip == null)
            {
                return;
            }

            stopRequested = false;
            IsSpinning = true;
            spinRoutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>Asks the reel to slow down and land on the given strip index.</summary>
        public void RequestStop(int stopIndex)
        {
            targetStopIndex = stopIndex;
            stopRequested = true;
        }

        /// <summary>Bounces the payline symbol to show it is part of a win.</summary>
        public void PlayWinPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(WinPulseRoutine(cells[HalfCells].rectTransform));
        }

        private IEnumerator SpinRoutine()
        {
            // 1) Wind-up: pull back a little against the spin direction (anticipation).
            float start = position;
            yield return Animate(profile.windUpDuration, t =>
            {
                position = start - profile.windUpDistance * Mathf.Sin(t * Mathf.PI * 0.5f);
            });

            // 2) Accelerate up to full speed.
            float elapsed = 0f;
            while (elapsed < profile.accelerationDuration)
            {
                elapsed += Time.deltaTime;
                float k = profile.accelerationCurve.Evaluate(Mathf.Clamp01(elapsed / profile.accelerationDuration));
                Advance(profile.maxSpeed * k);
                yield return null;
            }

            // 3) Spin at full speed until the controller asks us to stop.
            while (!stopRequested)
            {
                Advance(profile.maxSpeed);
                yield return null;
            }

            // 4) Find the first point at least minStopDistance ahead where the target symbol
            //    sits on the payline. Keep spinning at full speed until we are exactly
            //    minStopDistance away from it, so every reel brakes over the same distance
            //    and the stop always looks the same.
            float landing = NextAlignedPosition(position + profile.minStopDistance, targetStopIndex);
            float brakeStart = landing - profile.minStopDistance;
            while (position < brakeStart)
            {
                Advance(profile.maxSpeed);
                yield return null;
            }

            // 5) Brake with an ease-out-quad. Its starting speed is 2 * distance / duration;
            //    picking the duration so that equals maxSpeed means there is no jerk when
            //    braking begins.
            float from = position;
            float overshootTarget = landing + profile.overshoot;
            float distance = overshootTarget - from;
            float brakeDuration = 2f * distance / profile.maxSpeed;

            yield return Animate(brakeDuration, t =>
            {
                float eased = 1f - (1f - t) * (1f - t);
                position = from + distance * eased;
            });

            Landed?.Invoke(this);

            // 6) Settle back from the overshoot onto the payline (the little "bounce").
            yield return Animate(profile.settleDuration, t =>
            {
                float eased = t * t * (3f - 2f * t); // smoothstep
                position = Mathf.Lerp(overshootTarget, landing, eased);
            });

            // Snap exactly and keep the number small so float precision never drifts.
            position = strip.Wrap(Mathf.RoundToInt(landing));
            velocity = 0f;
            Render();

            IsSpinning = false;
            spinRoutine = null;
            Stopped?.Invoke(this);
        }

        /// <summary>
        /// Runs <paramref name="step"/> every frame with t going 0 -> 1 over <paramref name="duration"/>,
        /// and keeps <c>velocity</c> up to date so the motion blur follows the real speed.
        /// </summary>
        private IEnumerator Animate(float duration, Action<float> step)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float before = position;
                step(Mathf.Clamp01(elapsed / duration));
                velocity = Time.deltaTime > 0f ? Mathf.Abs(position - before) / Time.deltaTime : 0f;
                Render();
                yield return null;
            }
        }

        private void Advance(float speed)
        {
            velocity = speed;
            position += speed * Time.deltaTime;
            Render();
        }

        /// <summary>Smallest whole position >= minimum whose payline symbol is strip[stopIndex].</summary>
        private float NextAlignedPosition(float minimum, int stopIndex)
        {
            int baseIndex = Mathf.CeilToInt(minimum);
            int stepsForward = strip.Wrap(stopIndex - baseIndex);
            return baseIndex + stepsForward;
        }

        /// <summary>Places the recycled cells around the current position and applies motion blur.</summary>
        private void Render()
        {
            if (strip == null)
            {
                return;
            }

            int baseIndex = Mathf.FloorToInt(position);
            float fraction = position - baseIndex;
            float speed01 = Mathf.Clamp01(velocity / profile.maxSpeed);
            var stretch = new Vector3(1f, 1f + profile.blurStretch * speed01, 1f);
            var tint = new Color(1f, 1f, 1f, 1f - profile.blurFade * speed01);

            for (int i = 0; i < cells.Length; i++)
            {
                int offset = i - HalfCells; // -2..2, 0 is the payline
                Image cell = cells[i];
                cell.sprite = strip[baseIndex + offset].Sprite;
                cell.rectTransform.anchoredPosition = new Vector2(0f, (offset - fraction) * symbolPitch);

                if (pulseRoutine == null || i != HalfCells)
                {
                    cell.rectTransform.localScale = stretch;
                }

                cell.color = tint;
            }
        }

        private IEnumerator WinPulseRoutine(RectTransform target)
        {
            for (int n = 0; n < profile.winPulseCount; n++)
            {
                float elapsed = 0f;
                while (elapsed < profile.winPulseDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / profile.winPulseDuration);
                    float scale = 1f + profile.winPulseScale * Mathf.Sin(t * Mathf.PI);
                    target.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
            }

            target.localScale = Vector3.one;
            pulseRoutine = null;
        }

        private void OnDisable()
        {
            // Never leave a reel half-spun if the object gets disabled mid-spin.
            if (spinRoutine != null)
            {
                StopCoroutine(spinRoutine);
                spinRoutine = null;
                IsSpinning = false;
            }
        }
    }
}
