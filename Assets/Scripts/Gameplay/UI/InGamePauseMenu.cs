using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Controls the in-game pause menu
    /// </summary>
    public class InGamePauseMenu : MonoBehaviour {
        public bool Paused { get; private set; }
        
        public void TogglePauseMenu() {
            Paused = !Paused;
            gameObject.SetActive(Paused);
        }

        public void ReturnToGame() {
            TogglePauseMenu();
        }

        public void Surrender() {
            GameManager.Instance.GameEndManager.EndGame(GameManager.Instance.OpponentPlayer);
            TogglePauseMenu();
        }
    }
}