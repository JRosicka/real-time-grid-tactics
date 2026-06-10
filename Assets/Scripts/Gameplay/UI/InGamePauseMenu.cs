using Gameplay.Entities;
using Mirror;
using Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls the in-game pause menu
    /// </summary>
    public class InGamePauseMenu : MonoBehaviour {
        public Button SurrenderButton;
        public Button ReturnToMenuButton;
        public Button EndGameButton;
        public CanvasGroup CanvasGroup;
        public SettingsMenu SettingsMenu;
        
        private GameManager GameManager => GameManager.Instance;
        
        public bool Paused { get; private set; }
        
        public void TogglePauseMenu() {
            Paused = !Paused;
            if (Paused) {
                // When pausing, we need to hide the menu at first while we wait for the layout groups to rebuild. 
                CanvasGroup.alpha = 0;
            }
            gameObject.SetActive(Paused);

            if (GameTypeTracker.Instance.GameIsNetworked && NetworkServer.active && GameManager.LocalTeam == GameTeam.Spectator) {
                // Spectator host, so show a force-end-game button
                SurrenderButton.gameObject.SetActive(false);
                ReturnToMenuButton.gameObject.SetActive(false);
                EndGameButton.gameObject.SetActive(true);
            }
            else if (!GameTypeTracker.Instance.GameIsNetworked || GameManager.LocalTeam == GameTeam.Spectator) {
                // SP or spectator, so show a return-to-menu button
                SurrenderButton.gameObject.SetActive(false);
                ReturnToMenuButton.gameObject.SetActive(true);
                EndGameButton.gameObject.SetActive(false);
            } else {
                // MP game player. Just show a surrender button
                SurrenderButton.gameObject.SetActive(true);
                ReturnToMenuButton.gameObject.SetActive(false);
                EndGameButton.gameObject.SetActive(false);
            }

            CanvasGroup.alpha = 1;
        }

        public void ReturnToGame() {
            TogglePauseMenu();
        }

        public void Settings() {
            SettingsMenu.Open(null);
        }

        public void Surrender() {
            IGamePlayer winner = GameManager.GetPlayerForTeam(GameManager.LocalTeam.OpponentTeam());
            GameManager.Instance.GameEndManager.EndGame(winner);
            TogglePauseMenu();
        }

        public void ReturnToMainMenu() {
            GameManager.Instance.ReturnToMainMenu();
        }
        
        public void ForceEndGame() {
            GameManager.Instance.GameEndManager.ForceEndGame();
        }
    }
}