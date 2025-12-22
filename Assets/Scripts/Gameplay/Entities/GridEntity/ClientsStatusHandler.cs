using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using Scenes;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Keeps track of some arbitrary event as it occurs across all clients in a game, executing an arbitrary action
    /// on the server once the event has occurred on all clients. 
    ///
    /// Considers SP and MP games. 
    /// </summary>
    public class ClientsStatusHandler : MonoBehaviour {
        private const int SecondsToWaitForPlayersBeforeComplaining = 10;
        
        public MPClientsStatusHandler MPClientsStatusHandler;
        private Action _performWhenAllPlayersReady;
        private Dictionary<int, MPGamePlayer> AllPlayers => GameManager.Instance.GameSetupManager.AllPlayers;
        private bool _localClientReady;
        private bool _done;
        private bool _complainTimerStarted;
        private string _handlerName;
        // 'True' indicates that the player is connected and ready OR that they are disconnected (so we don't care to wait for them)
        // ReSharper disable once CollectionNeverUpdated.Local  Yeah it is.
        private readonly Dictionary<int, bool> _playerReadyStatuses = new();
        
        private GameSetupManager GameSetupManager => GameManager.Instance.GameSetupManager;
        private int LocalIndex => GameManager.Instance.LocalPlayerIndex;

        public void Initialize(Action performWhenAllPlayersReady, string handlerName) {
            _handlerName = handlerName;

            foreach (KeyValuePair<int, MPGamePlayer> kvp in AllPlayers) {
                _playerReadyStatuses[kvp.Key] = false;
            }
            
            if (!GameNetworkStateTracker.Instance.GameIsNetworked) {
                // SP
                _performWhenAllPlayersReady = performWhenAllPlayersReady;
            } else if (GameNetworkStateTracker.Instance.HostForNetworkedGame) {
                // MP server
                _performWhenAllPlayersReady = performWhenAllPlayersReady;
                MPClientsStatusHandler.ClientReadyEvent += MarkClientReady;
                GameSetupManager.PlayerDisconnected += CheckForDisconnectedClients;
                CheckForDisconnectedClients();
            }
        }

        public void SetLocalClientReady() {
            if (_localClientReady || _done) return;

            _localClientReady = true;
            // TODO could add a utility method for this if else block since we do it in a bunch of places (and another one for the if else-if version of this for running on server)
            if (!GameNetworkStateTracker.Instance.GameIsNetworked) {
                // SP
                PerformAction();
            } else {
                // MP
                MPClientsStatusHandler.CmdSetClientReady(LocalIndex);
            }
        }

        private void MarkClientReady(int index) {
            if (_done) return;
            
            _playerReadyStatuses[index] = true;
            DetermineIfAllClientsReady();

            if (!_complainTimerStarted) {
                ComplainIfMissingPlayers();
            }
        }

        private void CheckForDisconnectedClients() {
            foreach (KeyValuePair<int,MPGamePlayer> kvp in AllPlayers) {
                if (!kvp.Value.Connected) {
                    _playerReadyStatuses[kvp.Key] = true;
                }
            }
            
            DetermineIfAllClientsReady();
        }

        private void DetermineIfAllClientsReady() {
            if (_playerReadyStatuses.Values.All(b => b)) {
                PerformAction();
            }
        }

        private void PerformAction() {
            if (_done) return;
            
            _performWhenAllPlayersReady?.Invoke();
            _done = true;
        }

        private async void ComplainIfMissingPlayers() {
            _complainTimerStarted = true;
            
            await Task.Delay(SecondsToWaitForPlayersBeforeComplaining * 1000);
            if (_done) return;

            string reportedIndices = "";
            foreach (KeyValuePair<int, bool> readyStatus in _playerReadyStatuses) {
                reportedIndices += $"Index {readyStatus.Key}: ";
                reportedIndices += readyStatus.Value ? "ready. " : "unready. ";
            }
            Debug.LogWarning($"{_handlerName} waited {SecondsToWaitForPlayersBeforeComplaining}s for all players to report! Reported indices: [{reportedIndices}]!");
        }
    }
}