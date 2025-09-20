using Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Pause button for opening <see cref="InGamePauseMenu"/>
    /// </summary>
    public class InGamePauseButton : MonoBehaviour {
        public InGamePauseMenu PauseMenu;
        public Image PauseButtonImage;
        public Button PauseButton;
        
        private static GameSetupManager GameSetupManager => GameManager.Instance.GameSetupManager;
        
        public void Initialize(ColoredButtonData buttonData) {
            PauseButtonImage.sprite = buttonData.Normal;
            PauseButton.spriteState = new SpriteState {
                highlightedSprite = buttonData.Hovered,
                pressedSprite = buttonData.Pressed,
                selectedSprite = buttonData.Normal,
                disabledSprite = buttonData.Pressed
            };
        }
        
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