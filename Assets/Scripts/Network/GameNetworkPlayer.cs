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
        public CSteamID SteamID;

        public static event Action PlayerReadyStatusChanged;
        public static event Action PlayerSteamInfoDetermined;
        public event Action<ulong, int> PlayerSwappedToSlot;

        [Command]
        private void CmdSetSteamIDs(CSteamID newSteamID, string newDisplayName) {
            SteamID = newSteamID;
            DisplayName = newDisplayName;
        }
        
        private void OnDisplayNameSet(string oldName, string newName) {
            PlayerSteamInfoDetermined?.Invoke();
        }

        [Server]
        public void Kick() {
            TargetPrepareForKick(netIdentity.connectionToClient);
        }

        [TargetRpc]
        private void TargetPrepareForKick(NetworkConnection target) {
            DisconnectFeedbackService.SetDisconnectReason(DisconnectFeedbackService.DisconnectReason.Kicked);
            CmdDoKick();
        }

        [Command]
        private void CmdDoKick() {
            netIdentity.connectionToClient.Disconnect();
        }

        [Command]
        public void CmdSwapToSlot(int slotIndex) {
            // Swapping un-readies the player 
            CmdChangeReadyState(false);
            index = slotIndex;
            
            RpcSwapToSlot(slotIndex);
        }

        [ClientRpc]
        private void RpcSwapToSlot(int slotIndex) {
            PlayerSwappedToSlot?.Invoke(SteamID.m_SteamID, slotIndex);
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
            PlayerExitedRoom?.Invoke();
        }

        public override void IndexChanged(int oldIndex, int newIndex)
        {
            Debug.Log($"IndexChanged {newIndex}");
        }

        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
        {
            Debug.Log($"ReadyStateChanged {newReadyState}");
            PlayerReadyStatusChanged?.Invoke();
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }
    }
}
