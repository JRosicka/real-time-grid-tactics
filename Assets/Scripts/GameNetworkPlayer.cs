using System;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Network
{
    // [AddComponentMenu("")]
    public class GameNetworkPlayer : NetworkRoomPlayer
    {
        // Assume that we are using Steam for lobbying
        private SteamLobbyService steamLobbyService => SteamLobbyService.Instance;

        public bool IsHostPlayer => isLocalPlayer && isServer;
        
        [SyncVar(hook = nameof(OnDisplayNameSet))]
        public string DisplayName;
        [SyncVar]
        private CSteamID _steamID;
        [SyncVar(hook = nameof(OnReadyStatusChanged))]
        public bool ReadyToPlay;

        public static event Action PlayerReadyStatusChanged;
        public static event Action PlayerSteamInfoDetermined;

        [Command]
        private void CmdSetSteamIDs(CSteamID newSteamID, string newDisplayName) {
            _steamID = newSteamID;
            DisplayName = newDisplayName;
        }

        [Command]
        public void CmdUpdateReadyStatus(bool newReadyStatus) {
            ReadyToPlay = newReadyStatus;
        }

        private void OnReadyStatusChanged(bool oldValue, bool newValue) {
            PlayerReadyStatusChanged.SafeInvoke();
        }

        private void OnDisplayNameSet(string oldName, string newName) {
            PlayerSteamInfoDetermined.SafeInvoke();
        }

        public void Kick() {
            netIdentity.connectionToClient.Disconnect();
        }

        public override void OnStartClient()
        {
            Debug.Log($"OnStartClient {gameObject}");
            if (isLocalPlayer) {
                CmdSetSteamIDs(SteamUser.GetSteamID(), SteamFriends.GetPersonaName());
            }
        }

        public override void OnClientEnterRoom()
        {
            Debug.Log($"OnClientEnterRoom {SceneManager.GetActiveScene().path}");
        }

        public static event Action PlayerExitedRoom;
        public override void OnClientExitRoom()
        {
            Debug.Log($"OnClientExitRoom {SceneManager.GetActiveScene().path}");
            PlayerExitedRoom.SafeInvoke();
        }

        public override void IndexChanged(int oldIndex, int newIndex)
        {
            Debug.Log($"IndexChanged {newIndex}");
        }

        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
        {
            Debug.Log($"ReadyStateChanged {newReadyState}");
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }
    }
}
