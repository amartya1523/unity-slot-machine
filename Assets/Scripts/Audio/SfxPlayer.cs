using UnityEngine;

namespace Underpin.SlotMachine.Audio
{
    /// <summary>
    /// Plays every sound in the game from one place, so gameplay code just says
    /// "play the reel stop" and doesn't deal with AudioSources.
    /// One source handles one-shots; a second one loops the reel spin sound.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Clips")]
        [SerializeField] private AudioClip leverPull;
        [SerializeField] private AudioClip spinLoop;
        [SerializeField] private AudioClip reelStop;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip winSmall;
        [SerializeField] private AudioClip winJackpot;
        [SerializeField] private AudioClip coinTick;
        [SerializeField] private AudioClip freeSpins;

        public void PlayLever() => Play(leverPull);
        public void PlayReelStop() => Play(reelStop, 0.9f);
        public void PlayClick() => Play(buttonClick, 0.7f);
        public void PlaySmallWin() => Play(winSmall);
        public void PlayJackpot() => Play(winJackpot);
        public void PlayCoinTick() => Play(coinTick, 0.35f);
        public void PlayFreeSpins() => Play(freeSpins);

        public void StartSpinLoop()
        {
            if (loopSource == null || spinLoop == null)
            {
                return;
            }

            loopSource.clip = spinLoop;
            loopSource.loop = true;
            loopSource.Play();
        }

        public void StopSpinLoop()
        {
            if (loopSource != null)
            {
                loopSource.Stop();
            }
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && oneShotSource != null)
            {
                oneShotSource.PlayOneShot(clip, volume);
            }
        }
    }
}
