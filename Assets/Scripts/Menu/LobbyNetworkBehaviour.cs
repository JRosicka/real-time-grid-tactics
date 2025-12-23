using Mirror;
using Scenes;

namespace Menu {
    /// <summary>
    /// Networked logic for the lobby
    /// </summary>
    public class LobbyNetworkBehaviour : NetworkBehaviour {
        private string _mapID = "origins";

        /// <summary>
        /// Set the map and update the clients 
        /// </summary>
        [Server]
        public void SwitchMap(string mapID) {
            RpcSwitchMap(mapID);
        }
        
        [ClientRpc]
        private void RpcSwitchMap(string mapID) {
            _mapID = mapID;
            SceneLoader.Instance.SwitchLoadedMap(_mapID);
        }
        
        /// <summary>
        /// No longer allow for map loading in the lobby, as a precaution to ensure that we don't try to reload the
        /// game scene when we are entering a match
        /// </summary>
        [Server]
        public void LockMapLoading() {
            RpcLockMapLoading();
        }
        
        [ClientRpc]
        private void RpcLockMapLoading() {
            SceneLoader.Instance.LockMapLoading();
        }
    }
}