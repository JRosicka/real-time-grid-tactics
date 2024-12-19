using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// View for displaying current resources for a single player. Also displays their name. 
    /// </summary>
    public class PlayerResourcesView : MonoBehaviour {
        public TMP_Text PlayerName;
        public Image PlayerNameBackground;
        
        public TMP_Text BasicResourcesAmount;
        public TMP_Text AdvancedResourcesAmount;

        public void SetPlayerDetails(string playerName, Color playerColor) {
            PlayerName.text = playerName;
            PlayerNameBackground.color = playerColor;
        }

        public void UpdateAmounts(int gold, int amber) {
            BasicResourcesAmount.text = gold.ToString();
            AdvancedResourcesAmount.text = amber.ToString();
        }
    }
}