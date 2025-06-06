using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Stores config info about game currencies
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/CurrencyConfiguration", fileName = "CurrencyConfiguration")]
    public class CurrencyConfiguration : ScriptableObject {
        [Serializable]
        public class Currency {
            public ResourceType Type;
            public string DisplayName;
            public Sprite Icon;
            public string TextIconGlyph;
            public int StartingAmount;
        }
        
        public List<Currency> Currencies;
        
        
        public int StartingGoldAmount => GameManager.Instance?.Cheats.PlayerMoneyFromCheats != null 
                    ? GameManager.Instance.Cheats.PlayerMoneyFromCheats.Value 
                    : Currencies.First(c => c.Type == ResourceType.Basic).StartingAmount;

        public int StartingAmberAmount => GameManager.Instance?.Cheats.PlayerMoneyFromCheats != null 
                    ? GameManager.Instance.Cheats.PlayerMoneyFromCheats.Value 
                    : Currencies.First(c => c.Type == ResourceType.Advanced).StartingAmount;
    }
}