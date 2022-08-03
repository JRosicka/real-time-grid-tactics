using System;
using System.Linq;
using Game.Gameplay;
using Mirror;
using Mirror.Examples.NetworkRoom;
using Unity.VisualScripting;
using UnityEngine;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

namespace Game.Network
{
    // [AddComponentMenu("")]
    public class GameNetworkManager : NetworkRoomManager
    {
        [Header("Spawner Setup")]
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        public override void Start() {
            base.Start();
            
            // Tell the steam lobby service that this is the new network manager to use
            SteamLobbyService.Instance.ReInitialize(this);
            
            // Listen for any updates to the Steam lobby metadata
            SteamLobbyService.Instance.OnCurrentLobbyMetadataChanged += GetUpdatedLobbyData;

            transport = SteamLobbyService.Instance.SteamTransport;
        }

        public override void OnDestroy() {
            SteamLobbyService.Instance.OnCurrentLobbyMetadataChanged -= GetUpdatedLobbyData;
        }

        // TODO: What to do when there are no players assigned yet?
        // public GameNetworkPlayer GetLocalPlayer() {
        //     return (GameNetworkPlayer)(roomSlots.First(p => p.isLocalPlayer));
        // }

        private bool _isHosting;
        public bool IsHosting() {
            return _isHosting;
        }

        public event Action<SteamLobbyService.Lobby> OnLobbyUpdated;
        private void GetUpdatedLobbyData() {
            SteamLobbyService.Lobby updatedLobby = SteamLobbyService.Instance.GetLobbyData(
                SteamLobbyService.Instance.CurrentLobbyID, null);
            // TODO update room with update lobby info
            OnLobbyUpdated.SafeInvoke(updatedLobby);
        }
        
        // Events invoked if this is the server
        #region Server events

        public event Action RoomStartHostAction;
        public override void OnRoomStartHost() {
            DebugLog(nameof(OnRoomStartHost));
            RoomStartHostAction.SafeInvoke();
            _isHosting = true;
            base.OnRoomStartHost();
        }

        public event Action RoomStartServerAction;
        public override void OnRoomStartServer() {
            DebugLog(nameof(OnRoomStartServer));
            RoomStartServerAction.SafeInvoke();
            base.OnRoomStartServer();
        }

        public event Action RoomStopHostAction;
        public override void OnRoomStopHost() {
            DebugLog(nameof(OnRoomStopHost));
            RoomStopHostAction.SafeInvoke();
            _isHosting = false;
            base.OnRoomStopHost();
        }
        
        public event Action RoomStopServerAction;
        public override void OnRoomStopServer() {
            DebugLog(nameof(OnRoomStopServer));
            RoomStopServerAction.SafeInvoke();
            base.OnRoomStopServer();
        }

        public event Action ServerReadyAction;
        public override void OnServerReady(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnServerReady));
            ServerReadyAction.SafeInvoke();
            base.OnServerReady(conn);
        }
        
        public event Action RoomServerAddPlayerAction;
        public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerAddPlayer));
            RoomServerAddPlayerAction.SafeInvoke();
            base.OnRoomServerAddPlayer(conn);
        }

        public event Action RoomServerConnectAction;
        public override void OnRoomServerConnect(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerConnect));
            RoomServerConnectAction.SafeInvoke();
            base.OnRoomServerConnect(conn);
        }

        public event Action RoomServerDisconnectAction;
        public override void OnRoomServerDisconnect(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerDisconnect));
            RoomServerDisconnectAction.SafeInvoke();
            base.OnRoomServerDisconnect(conn);
        }
        
        public event Action RoomServerPlayersNotReadyAction;
        public override void OnRoomServerPlayersNotReady() {
            DebugLog(nameof(OnRoomServerPlayersNotReady));
            RoomServerPlayersNotReadyAction.SafeInvoke();
            base.OnRoomServerPlayersNotReady();
        }
        
        public event Action RoomServerPlayersReadyAction;
        public override void OnRoomServerPlayersReady() {
            DebugLog(nameof(OnRoomServerPlayersReady));
            RoomServerPlayersReadyAction.SafeInvoke();

            // calling the base method calls ServerChangeScene as soon as all players are in Ready state.
#if UNITY_SERVER
            base.OnRoomServerPlayersReady();
#endif
        }

        public event Action ServerChangeSceneAction;
        public override void OnServerChangeScene(string newSceneName) {
            DebugLog(nameof(OnServerChangeScene));
            ServerChangeSceneAction.SafeInvoke();
            base.OnServerChangeScene(newSceneName);
        }

        public event Action RoomServerSceneChangedAction;
        /// <summary>
        /// This is called on the server when a networked scene finishes loading.
        /// </summary>
        /// <param name="sceneName">Name of the new scene.</param>
        public override void OnRoomServerSceneChanged(string sceneName) {
            DebugLog(nameof(OnRoomServerSceneChanged));
            RoomServerSceneChangedAction.SafeInvoke();
            // spawn the initial batch of Rewards
            if (sceneName == GameplayScene)
                TempSpawner.InitialSpawn();
        }

        public event Action RoomServerSceneLoadedForPlayerAction;
        /// <summary>
        /// Called just after GamePlayer object is instantiated and just before it replaces RoomPlayer object.
        /// This is the ideal point to pass any data like player name, credentials, tokens, colors, etc.
        /// into the GamePlayer object as it is about to enter the Online scene.
        /// </summary>
        /// <param name="roomPlayer"></param>
        /// <param name="gamePlayer"></param>
        /// <returns>true unless some code in here decides it needs to abort the replacement</returns>
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer) {
            DebugLog(nameof(OnRoomServerSceneLoadedForPlayer));
            RoomServerSceneLoadedForPlayerAction.SafeInvoke();
            PlayerScore playerScore = gamePlayer.GetComponent<PlayerScore>();
            playerScore.index = roomPlayer.GetComponent<NetworkRoomPlayer>().index;
            return true;
        }
        
        public event Action ServerErrorAction;
        public override void OnServerError(NetworkConnectionToClient conn, Exception exception) {
            DebugLog($"{nameof(OnServerError)}: Exception: {exception}");
            ServerErrorAction.SafeInvoke();
            base.OnServerError(conn, exception);
        }
        
        #endregion

        // Events invoked on all clients
        #region Client events

        public event Action RoomStartClientAction;
        public override void OnRoomStartClient() {
            DebugLog(nameof(OnRoomStartClient));
            RoomStartClientAction.SafeInvoke();
            base.OnRoomStartClient();
        }

        public event Action RoomStopClientAction;
        public override void OnRoomStopClient() {
            DebugLog(nameof(OnRoomStopClient));
            
            // This is us leaving a lobby, so leave the steam lobby
            SteamLobbyService.Instance.ExitLobby();
            
            RoomStopClientAction.SafeInvoke();
            base.OnRoomStopClient();
        }
        
        public event Action RoomClientConnectAction;
        public override void OnRoomClientConnect() {
            DebugLog(nameof(OnRoomClientConnect));
            RoomClientConnectAction.SafeInvoke();
            base.OnRoomClientConnect();
        }

        public event Action RoomClientDisconnectAction;
        public override void OnRoomClientDisconnect() {
            DebugLog(nameof(OnRoomClientDisconnect));
            RoomClientDisconnectAction.SafeInvoke();
            base.OnRoomClientDisconnect();
        }

        public event Action RoomClientEnterAction;
        public override void OnRoomClientEnter() {
            DebugLog(nameof(OnRoomClientEnter));
            RoomClientEnterAction.SafeInvoke();
            base.OnRoomClientEnter();
            
            // Get whatever lobby data is there when we first join the room
            GetUpdatedLobbyData();
        }

        public event Action RoomClientExitAction;
        public override void OnRoomClientExit() {
            DebugLog(nameof(OnRoomClientExit));
            
            // See if the local player is still in the lobby. If not, then this is us as the lobby host leaving,
            // so let's leave the steam lobby as well
            if (!roomSlots.Any(p => p.isLocalPlayer)) {
                SteamLobbyService.Instance.ExitLobby();
            }

            RoomClientExitAction.SafeInvoke();
            base.OnRoomClientExit();
        }

        public event Action ClientNotReadyAction;
        public override void OnClientNotReady() {
            DebugLog(nameof(OnClientNotReady));
            ClientNotReadyAction.SafeInvoke();
            base.OnClientNotReady();
        }

        public event Action ClientChangeSceneAction;
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) {
            DebugLog(nameof(OnClientChangeScene));
            ClientChangeSceneAction.SafeInvoke();
            base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        }

        public event Action RoomClientSceneChangedAction;
        public override void OnRoomClientSceneChanged() {
            DebugLog(nameof(OnRoomClientSceneChanged));
            RoomClientSceneChangedAction.SafeInvoke();
            base.OnRoomClientSceneChanged();
        }
        
        public event Action RoomClientAddPlayerFailedAction;
        public override void OnRoomClientAddPlayerFailed() {
            DebugLog(nameof(OnRoomClientAddPlayerFailed));
            RoomClientAddPlayerFailedAction.SafeInvoke();
            base.OnRoomClientAddPlayerFailed();
        }

        public event Action ClientErrorAction;
        public override void OnClientError(Exception exception) {
            DebugLog($"{nameof(OnClientError)}: Exception: {exception}");
            ClientErrorAction.SafeInvoke();
            base.OnClientError(exception);
        }

        #endregion
        
        private void DebugLog(string message) {
            Debug.Log(message);
        }
    }
}
