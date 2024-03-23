using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Shows a visual for the game over screen
    /// </summary>
    public class GameOverView : MonoBehaviour {
        public GameObject VictoryText;
        public GameObject DefeatText;
        public GameObject TieText;
        
        public void ShowVictory() {
            gameObject.SetActive(true);
            VictoryText.SetActive(true);
            DefeatText.SetActive(false);
            TieText.SetActive(false);
        }
        
        public void ShowDefeat() {
            gameObject.SetActive(true);
            VictoryText.SetActive(false);
            DefeatText.SetActive(true);
            TieText.SetActive(false);
        }

        public void ShowTie() {
            gameObject.SetActive(true);
            VictoryText.SetActive(false);
            DefeatText.SetActive(false);
            TieText.SetActive(true);
        }
    }
}