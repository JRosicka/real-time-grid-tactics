using System;
using Game.Gameplay;
using Mirror;
using Mirror.Examples.NetworkRoom;
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
            
            // Listen for any updates to the Steam lobby metadata
            SteamLobbyService.Instance.OnCurrentLobbyMetadataChanged += GetUpdatedLobbyData;
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
        
        /*
        This code below is to demonstrate how to do a Start button that only appears for the Host player
        showStartButton is a local bool that's needed because OnRoomServerPlayersReady is only fired when
        all players are ready, but if a player cancels their ready state there's no callback to set it back to false
        Therefore, allPlayersReady is used in combination with showStartButton to show/hide the Start button correctly.
        Setting showStartButton false when the button is pressed hides it in the game scene since NetworkRoomManager
        is set as DontDestroyOnLoad = true.
        */
        bool showStartButton;

        public event Action RoomServerPlayersReadyAction;
        public override void OnRoomServerPlayersReady() {
            DebugLog(nameof(OnRoomServerPlayersReady));
            RoomServerPlayersReadyAction.SafeInvoke();

            // calling the base method calls ServerChangeScene as soon as all players are in Ready state.
#if UNITY_SERVER
            base.OnRoomServerPlayersReady();
#else
            showStartButton = true;
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
            // Debug.Log(message);
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (allPlayersReady && showStartButton && GUI.Button(new Rect(150, 300, 120, 20), "START GAME"))
            {
                // set to false to hide it in the game scene
                showStartButton = false;

                ServerChangeScene(GameplayScene);
            }
        }
    }
}
