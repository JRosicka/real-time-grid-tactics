using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
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

        private SelectionInterface SelectionInterface => GameManager.Instance.SelectionInterface;
        private EntitySelectionManager EntitySelectionManager => GameManager.Instance.EntitySelectionManager;
        private ICommandManager CommandManager => GameManager.Instance.CommandManager;
        
        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }

        private void Update() {
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
                if (_playerInput.GetButtonDown(input)) {
                    SelectionInterface.HandleAbilityHotkey(input);
                }
            }
        }

        private void HandleEscape() {
            if (!_playerInput.GetButtonDown(EscapeAction)) return;

            // If paused, prioritize resuming
            if (PauseMenu.Paused) {
                PauseMenu.TogglePauseMenu();
                return;
            }
            
            // Otherwise clear the selected targetable ability if there is one
            if (EntitySelectionManager.DeselectTargetableAbility()) {
                return;
            }
            
            // Otherwise move out of a nested build menu if we are in one
            if (SelectionInterface.BuildMenuOpenFromSelection) {
                SelectionInterface.DeselectBuildAbility();
                return;
            }

            if (EntitySelectionManager.SelectedEntity != null) {
                // Otherwise clear the last build in the selected entity's build queue if there is one
                IBuildQueue buildQueue = EntitySelectionManager.SelectedEntity.BuildQueue;
                if (buildQueue != null && buildQueue.Queue.Count > 0) {
                    buildQueue.CancelBuild(buildQueue.Queue.Last());
                    return;
                }
            
                // Otherwise cancel all selected entity's in progress/queued abilities if there are any
                List<IAbility> cancelableAbilities = EntitySelectionManager.SelectedEntity.GetCancelableAbilities();
                if (cancelableAbilities.Count > 0) {
                    cancelableAbilities.ForEach(a => CommandManager.CancelAbility(a));
                    return;
                }

                // Otherwise deselect the current entity if there is one selected
                EntitySelectionManager.SelectEntity(null);
                return;
            }

            // Otherwise pause
            PauseMenu.TogglePauseMenu();
        }
    }
}