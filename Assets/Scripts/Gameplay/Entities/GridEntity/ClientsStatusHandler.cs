using System;
using System.Threading.Tasks;
using Mirror;
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
        private int _readyPlayerCount;
        private int TotalPlayerCount => GameManager.Instance.GameSetupManager.MPSetupHandler.PlayerCount;
        private bool _localClientReady;
        private bool _done;
        private bool _complainTimerStarted;
        private string _handlerName;

        public void Initialize(Action performWhenAllPlayersReady, string handlerName) {
            _handlerName = handlerName; 
            if (!NetworkClient.active) {
                // SP
                _performWhenAllPlayersReady = performWhenAllPlayersReady;
            } else if (NetworkServer.active) {
                // MP server
                _performWhenAllPlayersReady = performWhenAllPlayersReady;
                MPClientsStatusHandler.ClientReadyEvent += MarkClientReady;
            }
        }

        public void SetLocalClientReady() {
            if (_localClientReady || _done) return;

            _localClientReady = true;
            // TODO could add a utility method for this if else block since we do it in a bunch of places (and another one for the if else-if version of this for running on server)
            if (!NetworkClient.active) {
                // SP
                PerformAction();
            } else {
                // MP
                MPClientsStatusHandler.CmdSetClientReady();
            }
        }

        private void MarkClientReady() {
            if (_done) return;
            
            _readyPlayerCount++;
            if (_readyPlayerCount >= TotalPlayerCount) {
                PerformAction();
            }

            if (!_complainTimerStarted) {
                ComplainIfMissingPlayers();
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
            
            Debug.LogWarning($"{_handlerName} waited {SecondsToWaitForPlayersBeforeComplaining}s for all players to report, but only {_readyPlayerCount} players reported!");
        }
    }
}