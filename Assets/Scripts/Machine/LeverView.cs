using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Machine
{
    /// <summary>
    /// The side lever. Clicking it spins the machine, same as the SPIN button.
    /// The pull is a two-frame sprite swap (up -> down -> up) from the provided art.
    /// </summary>
    public sealed class LeverView : MonoBehaviour
    {
        [SerializeField] private Image leverImage;
        [SerializeField] private Sprite leverUp;
        [SerializeField] private Sprite leverDown;
        [SerializeField, Tooltip("Invisible button over the lever, so only the lever itself is clickable.")]
        private Button hitArea;
        [SerializeField, Min(0.01f)] private float holdDownTime = 0.22f;

        public event Action Pulled;

        private Coroutine pullRoutine;

        private void Awake()
        {
            if (hitArea != null)
            {
                hitArea.onClick.AddListener(() => Pulled?.Invoke());
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (hitArea != null)
            {
                hitArea.interactable = interactable;
            }
        }

        public void PlayPull()
        {
            if (pullRoutine != null)
            {
                StopCoroutine(pullRoutine);
            }

            pullRoutine = StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            leverImage.sprite = leverDown;
            yield return new WaitForSeconds(holdDownTime);
            leverImage.sprite = leverUp;
            pullRoutine = null;
        }
    }
}
