using System;
using System.Threading.Tasks;
using Mirror;
using UnityEngine.SceneManagement;

namespace Gameplay.Managers {
    /// <summary>
    /// Listens for disconnects due to uncaught exceptions and gracefully handles returning to the main menu. Client-only. 
    /// Does nothing for SP. 
    /// </summary>
    public class DisconnectionHandler {
        public event Action OnDisconnected;
        public bool Disconnected { get; private set; }
        
        public DisconnectionHandler() {
            if (!NetworkClient.active) return;

            // TODO listen for disconnects, call DisconnectDetected when found
        }

        private async void DisconnectDetected() {
            Disconnected = true;
            OnDisconnected?.Invoke();

            await Task.Delay(TimeSpan.FromSeconds(3));
            SceneManager.LoadScene("GamePlay");
        }
    }
}