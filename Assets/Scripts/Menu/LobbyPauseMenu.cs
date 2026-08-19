using Gameplay.UI;
using UnityEngine;

namespace Menu {
    public class LobbyPauseMenu : MonoBehaviour {
        [SerializeField] private RoomMenu _roomMenu;
        [SerializeField] private SettingsMenu _settingsMenu;

        public bool Active { get; private set; }
        
        public void Show() {
            Active = true;
            gameObject.SetActive(true);
        }

        public void Close() {
            Active = false;
            gameObject.SetActive(false);
        }
        
        public void OpenSettingsMenu() {
            _settingsMenu.Open(null);
        }
        
        public void LeaveLobby() {
            _roomMenu.ExitRoom();
        }
    }
}