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
        
        [SyncVar]
        public string DisplayName;
        [SyncVar]
        private CSteamID _steamID;

        public static event Action PlayerSteamInfoDetermined;

        [Command]
        private void CmdSetSteamIDs(CSteamID newSteamID, string newDisplayName) {
            _steamID = newSteamID;
            DisplayName = newDisplayName;
            PlayerSteamInfoDetermined.SafeInvoke();
        }

        public override void OnStartClient()
        {
            if (isLocalPlayer) {
                CmdSetSteamIDs(SteamUser.GetSteamID(), SteamFriends.GetPersonaName());
            }
            Debug.Log($"OnStartClient {gameObject}");
        }

        public override void OnClientEnterRoom()
        {
            Debug.Log($"OnClientEnterRoom {SceneManager.GetActiveScene().path}");
        }

        public override void OnClientExitRoom()
        {
            Debug.Log($"OnClientExitRoom {SceneManager.GetActiveScene().path}");
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
