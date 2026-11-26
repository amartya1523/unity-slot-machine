using UnityEngine;

namespace Underpin.SlotMachine.Reels
{
    /// <summary>
    /// Tuning values for how a reel moves. Distances are in "symbols" (1 = one symbol height),
    /// so the feel stays the same if the art is resized.
    /// The asset lives in Assets/Animations so it can be tweaked live in Play mode.
    /// </summary>
    [CreateAssetMenu(fileName = "ReelAnimationProfile", menuName = "Slot Machine/Reel Animation Profile")]
    public sealed class ReelAnimationProfile : ScriptableObject
    {
        [Header("Wind-up (small pull back before the spin, like a real reel)")]
        [Min(0f)] public float windUpDistance = 0.3f;
        [Min(0.01f)] public float windUpDuration = 0.14f;

        [Header("Spin")]
        [Min(0.01f)] public float accelerationDuration = 0.25f;
        [Min(1f), Tooltip("Top speed in symbols per second.")]
        public float maxSpeed = 22f;
        public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Stop")]
        [Min(1f), Tooltip("Minimum symbols that still scroll past after a stop is requested.")]
        public float minStopDistance = 4f;
        [Min(0f), Tooltip("How far the reel overshoots the payline before settling back.")]
        public float overshoot = 0.22f;
        [Min(0.01f)] public float settleDuration = 0.2f;

        [Header("Motion blur")]
        [Range(0f, 0.6f), Tooltip("Vertical stretch of symbols at full speed, a cheap motion-blur effect.")]
        public float blurStretch = 0.22f;
        [Range(0f, 1f), Tooltip("How transparent symbols get at full speed.")]
        public float blurFade = 0.15f;

        [Header("Win highlight")]
        [Min(0f)] public float winPulseScale = 0.25f;
        [Min(0.01f)] public float winPulseDuration = 0.35f;
        [Min(1)] public int winPulseCount = 3;
    }
}
