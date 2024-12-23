using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Shows a visual for the game over screen
    /// </summary>
    public class GameOverView : MonoBehaviour {
        public GameObject VictoryText;
        public GameObject DefeatText;
        public GameObject TieText;
        public TMP_Text ResultTextForSpectator;
        public string ResultTextForSpectatorText = "Game over. {0} won!";
        
        public void ShowVictory() {
            gameObject.SetActive(true);
            VictoryText.SetActive(true);
            DefeatText.SetActive(false);
            TieText.SetActive(false);
            ResultTextForSpectator.gameObject.SetActive(false);
        }
        
        public void ShowDefeat() {
            gameObject.SetActive(true);
            VictoryText.SetActive(false);
            DefeatText.SetActive(true);
            TieText.SetActive(false);
            ResultTextForSpectator.gameObject.SetActive(false);
        }

        public void ShowTie() {
            gameObject.SetActive(true);
            VictoryText.SetActive(false);
            DefeatText.SetActive(false);
            TieText.SetActive(true);
            ResultTextForSpectator.gameObject.SetActive(false);
        }

        public void ShowSpectatorThatPlayerWon(IGamePlayer winner) {
            gameObject.SetActive(true);
            VictoryText.SetActive(false);
            DefeatText.SetActive(false);
            TieText.SetActive(false);
            ResultTextForSpectator.gameObject.SetActive(true);
            ResultTextForSpectator.text = string.Format(ResultTextForSpectatorText, winner.DisplayName);
        }
    }
}