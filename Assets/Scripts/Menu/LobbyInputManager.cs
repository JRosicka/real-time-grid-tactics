using Rewired;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// Handles input when in the lobby
    /// </summary>
    public class LobbyInputManager : MonoBehaviour {
        private const string EscapeAction = "Escape";

        [SerializeField] private LobbyPauseMenu _pauseMenu;

        private Player _playerInput;

        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }

        private void Update() {
            HandleEscape();
        }
        
        private void HandleEscape() {
            if (!_playerInput.GetButtonDown(EscapeAction)) return;
            if (_pauseMenu.Active) {
                _pauseMenu.Close();
                return;
            }
            _pauseMenu.Show();
        }
    }
}