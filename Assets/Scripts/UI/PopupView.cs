using System.Collections;
using UnityEngine;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// Base class for every popup (win, paytable, out-of-credits). Handles show/hide
    /// animation and blocks clicks to the machine behind it while open.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupView : MonoBehaviour
    {
        [SerializeField] protected RectTransform panel;
        [SerializeField, Min(0.01f)] private float popInDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.18f;

        private CanvasGroup canvasGroup;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        protected void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            SetVisible(true);
            StopAllCoroutines();
            StartCoroutine(UiAnimations.PopIn(panel, canvasGroup, popInDuration));
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            canvasGroup.interactable = false;
            yield return UiAnimations.FadeOut(canvasGroup, fadeOutDuration);
            SetVisible(false);
            OnClosed();
        }

        protected virtual void OnClosed() { }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? canvasGroup.alpha : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
