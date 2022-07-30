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

        [Command]
        private void CmdSetSteamIDs(CSteamID newSteamID, string newDisplayName) {
            _steamID = newSteamID;
            DisplayName = newDisplayName;
        }
        
        public override void OnStartClient()
        {
            // TODO here or elsewhere, link the steamID of the steam player object (whatever that is) with the correct Mirror player object. 
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
