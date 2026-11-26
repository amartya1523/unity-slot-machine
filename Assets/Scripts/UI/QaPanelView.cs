using System;
using System.Collections.Generic;
using Underpin.SlotMachine.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// QA / demo tool: forces the next spin to land three of a chosen symbol, so testers
    /// (and reviewers) can see the jackpot and free spins without spinning hundreds of times.
    /// Hidden behind the small "QA" button. Normal spins are never affected.
    /// </summary>
    public sealed class QaPanelView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private List<Button> forceButtons = new List<Button>();
        [SerializeField] private List<SymbolDefinition> forceSymbols = new List<SymbolDefinition>();

        public event Action<SymbolDefinition> ForceRequested;

        private void Awake()
        {
            panelRoot.SetActive(false);
            toggleButton.onClick.AddListener(() => panelRoot.SetActive(!panelRoot.activeSelf));

            for (int i = 0; i < forceButtons.Count && i < forceSymbols.Count; i++)
            {
                SymbolDefinition symbol = forceSymbols[i];
                forceButtons[i].onClick.AddListener(() =>
                {
                    ForceRequested?.Invoke(symbol);
                    panelRoot.SetActive(false);
                });
            }
        }
    }
}
