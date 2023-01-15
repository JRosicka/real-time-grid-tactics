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

        private Player _playerInput;

        private SelectionInterface SelectionInterface => GameManager.Instance.SelectionInterface;
        
        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }

        private void Update() {
            foreach (string input in AbilityButtons) {
                if (_playerInput.GetButtonDown(input)) {
                    SelectionInterface.HandleAbilityHotkey(input);
                }
            }
        }
    }
}