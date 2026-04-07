using System;
using System.Collections.Generic;
using Gameplay.UI;
using Rewired;
using Scenes;
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

        private static readonly List<string> ControlGroupButtons = new List<string> {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
        };
        private const string ShiftAction = "Shift";
        private const string ControlAction = "Control";

        private const string EscapeAction = "Escape";
        
        private const string MoveCameraLeftAction = "CameraLeft";
        private const string MoveCameraRightAction = "CameraRight";
        private const string MoveCameraUpAction = "CameraUp";
        private const string MoveCameraDownAction = "CameraDown";

        public CameraManager CameraManager;
        public InGamePauseMenu PauseMenu;

        public float ControlGroupCameraSnapWindowSeconds = .2f;
        
        private Player _playerInput;
        
        private int _lastSelectedControlGroup;
        private float _lastSelectedControlGroupTime;

        private static SelectionInterface SelectionInterface => GameManager.Instance.SelectionInterface;
        private static ControlGroupsManager ControlGroupsManager => GameManager.Instance.ControlGroupsManager;
        
        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }

        private void Update() {
            if (!GameManager.Instance.GameSetupManager.GameRunning) return;
            if (!GameTypeTracker.Instance.AllowInput) return;
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
                    SelectionInterface.HandleAbilityHotkey(input, true, _playerInput.GetButtonDown(input));
                } else if (_playerInput.GetButtonUp(input)) {
                    SelectionInterface.HandleAbilityHotkey(input, false, false);
                }
            }
            
            if (_playerInput.GetButton(ShiftAction) || _playerInput.GetButton(ControlAction)) {
                // Assigning control groups
                foreach (string input in ControlGroupButtons) {
                    if (_playerInput.GetButtonDown(input)) {
                        ControlGroupsManager.AssignControlGroup(Convert.ToInt32(input));
                    }
                }
            } else {
                // Selecting control groups
                foreach (string input in ControlGroupButtons) {
                    if (_playerInput.GetButtonDown(input)) {
                        HandleControlGroupSelection(input);
                    } else if (_playerInput.GetButtonUp(input)) {
                        ControlGroupsManager.SelectControlGroup(Convert.ToInt32(input), false, true);
                    }
                }
            }
        }

        private void HandleControlGroupSelection(string input) {
            int inputInt = Convert.ToInt32(input);
            ControlGroupsManager.SelectControlGroup(inputInt, true, true);

            if (inputInt == _lastSelectedControlGroup &&
                    Time.time < _lastSelectedControlGroupTime + ControlGroupCameraSnapWindowSeconds) {
                ControlGroupsManager.SnapCameraToControlGroup(inputInt);
            }

            _lastSelectedControlGroup = inputInt;
            _lastSelectedControlGroupTime = Time.time;
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