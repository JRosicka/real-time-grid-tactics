using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls the in-game pause menu
    /// </summary>
    public class InGamePauseMenu : MonoBehaviour {
        public Button SurrenderButton;
        
        private GameManager GameManager => GameManager.Instance;
        
        public bool Paused { get; private set; }
        
        public void TogglePauseMenu() {
            Paused = !Paused;
            gameObject.SetActive(Paused);

            if (GameManager.LocalTeam == GameTeam.Spectator) {
                // Can't surrender if we are just a spectator
                SurrenderButton.gameObject.SetActive(false);
            }
        }

        public void ReturnToGame() {
            TogglePauseMenu();
        }

        public void Surrender() {
            IGamePlayer winner = GameManager.GetPlayerForTeam(GameManager.LocalTeam.OpponentTeam());
            GameManager.Instance.GameEndManager.EndGame(winner);
            TogglePauseMenu();
        }
    }
}