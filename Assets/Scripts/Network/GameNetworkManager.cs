using System;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Game.Network {
    public class GameNetworkManager : NetworkRoomManager {
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

        private bool _isHosting;
        public bool IsHosting() {
            return _isHosting;
        }

        public event Action<SteamLobbyService.Lobby> OnLobbyUpdated;
        private void GetUpdatedLobbyData() {
            SteamLobbyService.Lobby updatedLobby = SteamLobbyService.Instance.GetLobbyData(
                SteamLobbyService.Instance.CurrentLobbyID, null);
            // TODO update room with update lobby info
            OnLobbyUpdated?.Invoke(updatedLobby);
        }
        
        // Events invoked if this is the server
        #region Server events

        public event Action RoomStartHostAction;
        public override void OnRoomStartHost() {
            DebugLog(nameof(OnRoomStartHost));
            RoomStartHostAction?.Invoke();
            _isHosting = true;
            base.OnRoomStartHost();
        }

        public event Action RoomStartServerAction;
        public override void OnRoomStartServer() {
            DebugLog(nameof(OnRoomStartServer));
            RoomStartServerAction?.Invoke();
            base.OnRoomStartServer();
        }

        public event Action RoomStopHostAction;
        public override void OnRoomStopHost() {
            DebugLog(nameof(OnRoomStopHost));
            RoomStopHostAction?.Invoke();
            _isHosting = false;
            base.OnRoomStopHost();
        }
        
        public event Action RoomStopServerAction;
        public override void OnRoomStopServer() {
            DebugLog(nameof(OnRoomStopServer));
            RoomStopServerAction?.Invoke();
            base.OnRoomStopServer();
        }

        public event Action ServerReadyAction;
        public override void OnServerReady(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnServerReady));
            ServerReadyAction?.Invoke();
            base.OnServerReady(conn);
        }
        
        public event Action RoomServerAddPlayerAction;
        public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerAddPlayer));
            RoomServerAddPlayerAction?.Invoke();
            base.OnRoomServerAddPlayer(conn);
        }

        public event Action RoomServerConnectAction;
        public override void OnRoomServerConnect(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerConnect));
            RoomServerConnectAction?.Invoke();
            base.OnRoomServerConnect(conn);
        }

        public event Action RoomServerDisconnectAction;
        public override void OnRoomServerDisconnect(NetworkConnectionToClient conn) {
            DebugLog(nameof(OnRoomServerDisconnect));
            RoomServerDisconnectAction?.Invoke();
            base.OnRoomServerDisconnect(conn);
        }
        
        public event Action RoomServerPlayersNotReadyAction;
        public override void OnRoomServerPlayersNotReady() {
            DebugLog(nameof(OnRoomServerPlayersNotReady));
            RoomServerPlayersNotReadyAction?.Invoke();
            base.OnRoomServerPlayersNotReady();
        }
        
        public event Action RoomServerPlayersReadyAction;
        public override void OnRoomServerPlayersReady() {
            DebugLog(nameof(OnRoomServerPlayersReady));
            RoomServerPlayersReadyAction?.Invoke();

            // calling the base method calls ServerChangeScene as soon as all players are in Ready state.
#if UNITY_SERVER
            base.OnRoomServerPlayersReady();
#endif
        }

        public event Action ServerChangeSceneAction;
        public override void OnServerChangeScene(string newSceneName) {
            DebugLog(nameof(OnServerChangeScene));
            ServerChangeSceneAction?.Invoke();
            base.OnServerChangeScene(newSceneName);
        }

        public event Action RoomServerSceneChangedAction;
        /// <summary>
        /// This is called on the server when a networked scene finishes loading.
        /// </summary>
        /// <param name="sceneName">Name of the new scene.</param>
        public override void OnRoomServerSceneChanged(string sceneName) {
            DebugLog(nameof(OnRoomServerSceneChanged));
            RoomServerSceneChangedAction?.Invoke();
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
            RoomServerSceneLoadedForPlayerAction?.Invoke();
            GameManager gameManager = FindObjectOfType<GameManager>();
            MPGamePlayer mpGamePlayer = gamePlayer.GetComponent<MPGamePlayer>();
            GameNetworkPlayer gameNetworkPlayer = roomPlayer.GetComponent<GameNetworkPlayer>();
            gameManager.GameSetupManager.SetupMPPlayer(gameNetworkPlayer, mpGamePlayer, numPlayers);
            return true;
        }

        protected override bool IsTryingToJoinLobbyWhileAlreadyInLobby(string newSceneName) {
            return IsSceneActive(newSceneName) && newSceneName == RoomScene;
        }
        
        public event Action ServerErrorAction;
        public override void OnServerError(NetworkConnectionToClient conn, Exception exception) {
            DebugLog($"{nameof(OnServerError)}: Exception: {exception}");
            ServerErrorAction?.Invoke();
            base.OnServerError(conn, exception);
        }
        
        #endregion

        // Events invoked on all clients
        #region Client events

        public event Action RoomStartClientAction;
        public override void OnRoomStartClient() {
            DebugLog(nameof(OnRoomStartClient));
            RoomStartClientAction?.Invoke();
            base.OnRoomStartClient();
        }

        public event Action RoomStopClientAction;
        public override void OnRoomStopClient() {
            DebugLog(nameof(OnRoomStopClient));
            
            // This is us leaving a lobby, so leave the steam lobby
            SteamLobbyService.Instance.ExitLobby();
            
            RoomStopClientAction?.Invoke();
            base.OnRoomStopClient();
        }
        
        public event Action RoomClientConnectAction;
        public override void OnRoomClientConnect() {
            DebugLog(nameof(OnRoomClientConnect));
            RoomClientConnectAction?.Invoke();
            base.OnRoomClientConnect();
        }

        public event Action RoomClientDisconnectAction;
        public override void OnRoomClientDisconnect() {
            DebugLog(nameof(OnRoomClientDisconnect));
            RoomClientDisconnectAction?.Invoke();
            base.OnRoomClientDisconnect();
        }

        public event Action RoomClientEnterAction;
        public override void OnRoomClientEnter() {
            DebugLog(nameof(OnRoomClientEnter));
            RoomClientEnterAction?.Invoke();
            base.OnRoomClientEnter();
            
            // Get whatever lobby data is there when we first join the room
            GetUpdatedLobbyData();
        }

        public event Action RoomClientExitAction;
        public override void OnRoomClientExit() {
            DebugLog(nameof(OnRoomClientExit));

            DisconnectFeedbackService.SetDisconnected();
            
            // See if the local player is still in the lobby. If not, then this is us as the lobby host leaving,
            // so let's leave the steam lobby as well
            if (!roomSlots.Any(p => p.isLocalPlayer)) {
                SteamLobbyService.Instance.ExitLobby();
            }

            RoomClientExitAction?.Invoke();
            base.OnRoomClientExit();
        }

        public event Action ClientNotReadyAction;
        public override void OnClientNotReady() {
            DebugLog(nameof(OnClientNotReady));
            ClientNotReadyAction?.Invoke();
            base.OnClientNotReady();
        }

        public event Action ClientChangeSceneAction;
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) {
            DebugLog(nameof(OnClientChangeScene));
            ClientChangeSceneAction?.Invoke();
            base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        }

        public event Action RoomClientSceneChangedAction;
        public override void OnRoomClientSceneChanged() {
            DebugLog(nameof(OnRoomClientSceneChanged));
            RoomClientSceneChangedAction?.Invoke();
            base.OnRoomClientSceneChanged();
        }
        
        public event Action RoomClientAddPlayerFailedAction;
        public override void OnRoomClientAddPlayerFailed() {
            DebugLog(nameof(OnRoomClientAddPlayerFailed));
            RoomClientAddPlayerFailedAction?.Invoke();
            base.OnRoomClientAddPlayerFailed();
        }

        public event Action ClientErrorAction;
        public override void OnClientError(Exception exception) {
            DebugLog($"{nameof(OnClientError)}: Exception: {exception}");
            ClientErrorAction?.Invoke();
            base.OnClientError(exception);
        }

        #endregion
        
        private void DebugLog(string message) {
            Debug.Log(message);
        }
    }
}
