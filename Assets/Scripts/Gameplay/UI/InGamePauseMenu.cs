using Gameplay.Entities;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Controls the in-game pause menu
    /// </summary>
    public class InGamePauseMenu : MonoBehaviour {
        public Button SurrenderButton;
        public Button ReturnToMenuButton;
        public RectTransform RootRectTransform;
        public RectTransform ButtonsRectTransform;
        public CanvasGroup CanvasGroup;
        
        private GameManager GameManager => GameManager.Instance;
        
        public bool Paused { get; private set; }
        
        public void TogglePauseMenu() {
            Paused = !Paused;
            if (Paused) {
                // When pausing, we need to hide the menu at first while we wait for the layout groups to rebuild. 
                CanvasGroup.alpha = 0;
            }
            gameObject.SetActive(Paused);

            if (!NetworkClient.active) {
                // SP, so show both buttons
                SurrenderButton.gameObject.SetActive(true);
                ReturnToMenuButton.gameObject.SetActive(true);
            } else if (GameManager.LocalTeam == GameTeam.Spectator) {
                // Can't surrender if we are just a spectator. Instead, add a return-to-menu button. 
                SurrenderButton.gameObject.SetActive(false);
                ReturnToMenuButton.gameObject.SetActive(true);
            } else {
                // MP game player. Just show a surrender button
                SurrenderButton.gameObject.SetActive(true);
                ReturnToMenuButton.gameObject.SetActive(false);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(ButtonsRectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(RootRectTransform);

            CanvasGroup.alpha = 1;
        }

        public void ReturnToGame() {
            TogglePauseMenu();
        }

        public void Surrender() {
            IGamePlayer winner = GameManager.GetPlayerForTeam(GameManager.LocalTeam.OpponentTeam());
            GameManager.Instance.GameEndManager.EndGame(winner);
            TogglePauseMenu();
        }

        public void ReturnToMainMenu() {
            GameManager.Instance.ReturnToMainMenu();
        }
    }
}