using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Pause button for opening <see cref="InGamePauseMenu"/>
    /// </summary>
    public class InGamePauseButton : MonoBehaviour {
        public InGamePauseMenu PauseMenu;
        public Image PauseButtonImage;
        public ListenerButton PauseButton;
        public Transform HamburgerLinesGroup;
        public List<Image> HamburgerLines;
        public Vector2 HamburgerLinesUpPosition;
        public Vector2 HamburgerLinesDownPosition;
        public Color HamburgerSelectedColor;
        public Color HamburgerDeselectedColor;
        
        private static GameSetupManager GameSetupManager => GameManager.Instance.GameSetupManager;
        
        public void Initialize(ColoredButtonData buttonData) {
            PauseButtonImage.sprite = buttonData.Normal;
            PauseButton.spriteState = new SpriteState {
                highlightedSprite = buttonData.Hovered,
                pressedSprite = buttonData.Pressed,
                selectedSprite = buttonData.Normal,
                disabledSprite = buttonData.Pressed
            };
            PauseButton.Pressed += ToggleClicked;
            PauseButton.NoLongerPressed += ToggleUnClicked;
        }
        
        public void TogglePauseMenu() {
            if (!GameSetupManager.GameInitialized) return;
            if (GameSetupManager.GameOver) return;

            PauseMenu.TogglePauseMenu();
            if (PauseMenu.SettingsMenu.Active) {
                PauseMenu.SettingsMenu.Close();
            }
        }

        private void ToggleClicked() {
            HamburgerLines.ForEach(l => l.color = HamburgerSelectedColor);
            HamburgerLinesGroup.localPosition = HamburgerLinesDownPosition;
        }
        
        private void ToggleUnClicked() {
            HamburgerLines.ForEach(l => l.color = HamburgerDeselectedColor);
            HamburgerLinesGroup.localPosition = HamburgerLinesUpPosition;
        }
    }
}