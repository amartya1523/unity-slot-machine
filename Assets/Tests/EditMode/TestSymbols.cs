using System.Collections.Generic;
using Underpin.SlotMachine.Data;
using UnityEngine;

namespace Underpin.SlotMachine.Tests
{
    /// <summary>Builds the same symbol set as the real game, in code, for tests.</summary>
    internal sealed class TestSymbols
    {
        public readonly SymbolDefinition Seven = Make("seven", 2, 50, SymbolRole.Jackpot);
        public readonly SymbolDefinition Bar = Make("bar", 4, 25);
        public readonly SymbolDefinition Bell = Make("bell", 6, 10, SymbolRole.FreeSpins, 5);
        public readonly SymbolDefinition Cherry = Make("cherry", 8, 4);

        public List<SymbolDefinition> All => new List<SymbolDefinition> { Seven, Bar, Bell, Cherry };

        public PaytableDefinition Paytable()
        {
            var table = ScriptableObject.CreateInstance<PaytableDefinition>();
            table.Configure(All, new[] { 10, 50, 100 }, 1000, 1000, 0.05f, 20);
            return table;
        }

        private static SymbolDefinition Make(string id, int weight, int multiplier,
            SymbolRole role = SymbolRole.Standard, int freeSpins = 0)
        {
            var symbol = ScriptableObject.CreateInstance<SymbolDefinition>();
            symbol.Configure(id, id, null, weight, multiplier, role, freeSpins);
            return symbol;
        }
    }
}
