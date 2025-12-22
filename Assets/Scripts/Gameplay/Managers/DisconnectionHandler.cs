using System;
using System.Threading.Tasks;
using Game.Network;
using Mirror;
using Scenes;
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
            if (!GameNetworkStateTracker.Instance.GameIsNetworked) return;

            MessagePacking.DisconnectedFromException += DisconnectDetected;
        }

        public void UnregisterListeners() {
            MessagePacking.DisconnectedFromException -= DisconnectDetected;
        }

        private async void DisconnectDetected() {
            Disconnected = true;
            OnDisconnected?.Invoke();

            await Task.Delay(TimeSpan.FromSeconds(3));
            
            GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
            gameNetworkManager.ServerChangeScene(gameNetworkManager.RoomScene); 

            DisconnectFeedbackService.SetDisconnectReason(DisconnectFeedbackService.DisconnectReason.Unknown);
            if (NetworkServer.active) {
                // We're the host, so stop the whole server
                gameNetworkManager.StopHost();
            } else {
                // We're just a little baby client, so just stop the client
                gameNetworkManager.StopClient();
            }

            SceneLoader.Instance.LoadMainMenu();
        }
    }
}