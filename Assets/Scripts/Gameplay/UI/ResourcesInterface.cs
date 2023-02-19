using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// View for displaying current resources
    /// </summary>
    public class ResourcesInterface : MonoBehaviour {
        public TMP_Text BasicResourcesAmount;
        public TMP_Text AdvancedResourcesAmount;
        
        // TODO display income rate

        public void Initialize(PlayerResourcesController resourcesController) {
            resourcesController.BalanceChangedEvent += UpdateBalancesView;
            BasicResourcesAmount.text = "0";
            AdvancedResourcesAmount.text = "0";
        }

        private void UpdateBalancesView(List<ResourceAmount> resourceAmounts) {
            BasicResourcesAmount.text = resourceAmounts.First(r => r.Type == ResourceType.Basic).Amount.ToString();
            AdvancedResourcesAmount.text = resourceAmounts.First(r => r.Type == ResourceType.Advanced).Amount.ToString();
        }
    }
}