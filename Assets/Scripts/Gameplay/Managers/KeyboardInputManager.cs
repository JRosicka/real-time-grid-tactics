using System.Collections.Generic;
using Gameplay.UI;
using Rewired;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles player keyboard input
    /// </summary>
    public class KeyboardInputManager : MonoBehaviour {
        private static readonly List<string> AbilityButtons = new List<string> {
            "Q", "W", "E", "R", 
            "A", "S", "D", "F", 
            "Z", "X", "C", "V"
        };

        private const string EscapeAction = "Escape";
        
        private const string MoveCameraLeftAction = "CameraLeft";
        private const string MoveCameraRightAction = "CameraRight";
        private const string MoveCameraUpAction = "CameraUp";
        private const string MoveCameraDownAction = "CameraDown";

        public CameraManager CameraManager;
        public InGamePauseMenu PauseMenu;
        
        private Player _playerInput;

        private static SelectionInterface SelectionInterface => GameManager.Instance.SelectionInterface;
        
        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }

        private void Update() {
            if (!GameManager.Instance.GameSetupManager.GameInitialized) return;
            if (GameManager.Instance.GameSetupManager.GameOver) return;

            HandleEscape();

            if (PauseMenu.Paused) return;

            // Camera
            if (_playerInput.GetButton(MoveCameraLeftAction)) {
                CameraManager.MoveCameraOrthogonally(CameraManager.CameraDirection.Left);
            }
            if (_playerInput.GetButton(MoveCameraRightAction)) {
                CameraManager.MoveCameraOrthogonally(CameraManager.CameraDirection.Right);
            }
            if (_playerInput.GetButton(MoveCameraUpAction)) {
                CameraManager.MoveCameraOrthogonally(CameraManager.CameraDirection.Up);
            }
            if (_playerInput.GetButton(MoveCameraDownAction)) {
                CameraManager.MoveCameraOrthogonally(CameraManager.CameraDirection.Down);
            }
            
            // Abilities
            foreach (string input in AbilityButtons) {
                if (_playerInput.GetButtonRepeating(input)) {
                    SelectionInterface.HandleAbilityHotkey(input, true);
                } else if (_playerInput.GetButtonUp(input)) {
                    SelectionInterface.HandleAbilityHotkey(input, false);
                }
            }
        }

        private void HandleEscape() {
            if (!_playerInput.GetButtonDown(EscapeAction)) return;
            if (PauseMenu.SettingsMenu.Active) {
                PauseMenu.SettingsMenu.Close();
                return;
            }
            PauseMenu.TogglePauseMenu();
        }
    }
}