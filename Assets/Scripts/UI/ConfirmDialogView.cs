using System;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>YES / NO dialog, used when the player runs out of credits.</summary>
    public sealed class ConfirmDialogView : PopupView
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onYes;
        private Action onNo;

        protected override void Awake()
        {
            base.Awake();
            yesButton.onClick.AddListener(() => Answer(onYes));
            noButton.onClick.AddListener(() => Answer(onNo));
        }

        public void Show(string message, Action yes, Action no)
        {
            messageText.text = message;
            onYes = yes;
            onNo = no;
            Open();
        }

        private void Answer(Action callback)
        {
            Close();
            callback?.Invoke();
        }
    }
}
