using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Pause button for opening <see cref="InGamePauseMenu"/>
    /// </summary>
    public class InGamePauseButton : MonoBehaviour {
        public InGamePauseMenu PauseMenu;

        private static GameSetupManager GameSetupManager => GameManager.Instance.GameSetupManager;

        public void TogglePauseMenu() {
            if (!GameSetupManager.GameInitialized) return;
            if (GameSetupManager.GameOver) return;

            PauseMenu.TogglePauseMenu();
            if (PauseMenu.SettingsMenu.Active) {
                PauseMenu.SettingsMenu.Close();
            }
        }
    }
}