using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Controls the menu view for when an unexpected disconnection occurs
    /// </summary>
    public class DisconnectionDialog : MonoBehaviour {
        private DisconnectionHandler _disconnectionHandler;

        public void Initialize(DisconnectionHandler disconnectionHandler) {
            gameObject.SetActive(false);
            disconnectionHandler.OnDisconnected += ShowDisconnectDialog;
            _disconnectionHandler = disconnectionHandler;
        }
        
        private void OnDestroy() {
            _disconnectionHandler.OnDisconnected -= ShowDisconnectDialog;
        }

        private void ShowDisconnectDialog() {
            gameObject.SetActive(true);
        }
    }
}