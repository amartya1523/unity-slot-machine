using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// Shows the paytable and the rules. The rows are filled from the PaytableDefinition
    /// when the scene is built, so the numbers on screen always match the real payouts.
    /// </summary>
    public sealed class PaytableView : PopupView
    {
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }

        public void Show() => Open();
    }
}
