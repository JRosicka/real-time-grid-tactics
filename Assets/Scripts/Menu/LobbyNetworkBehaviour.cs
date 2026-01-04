using Mirror;
using Scenes;

namespace Menu {
    /// <summary>
    /// Networked logic for the lobby
    /// </summary>
    public class LobbyNetworkBehaviour : NetworkBehaviour {
        [SyncVar(hook = nameof(OnMapChanged))]
        public string MapID = SceneLoader.Instance.LastLobbyMap;
        
        /// <summary>
        /// Set the map and update the clients 
        /// </summary>
        [Server]
        public void SwitchMap(string mapID) {
            MapID = mapID;
        }
        
        private void OnMapChanged(string _, string newMapID) {
            SceneLoader.Instance.SwitchLoadedMap(newMapID, null, true);
        }

        /// <summary>
        /// Load the map that the lobby is set to
        /// </summary>
        public void SetUpCurrentMapOnLobbyJoin() {
            GameTypeTracker.Instance.SetMap(MapID);
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