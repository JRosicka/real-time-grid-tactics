using System;
using System.Linq;
using System.Threading.Tasks;
using Menu;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Network
{
    // [AddComponentMenu("")]
    // TODO LobbyNetworkBehaviour, RoomMenu, and GameNetworkPlayer are all spaghettified, reaching into each other and each doing networking and view logic. Gross. 
    public class GameNetworkPlayer : NetworkRoomPlayer
    {
        // Assume that we are using Steam for lobbying
        private SteamLobbyService steamLobbyService => SteamLobbyService.Instance;

        // TODO: Neither of these are true for the hosting player on other client machines...
        public bool IsHostPlayer => isLocalPlayer && isServer;
        
        [SyncVar(hook = nameof(OnDisplayNameSet))]
        public string DisplayName;
        [SyncVar]
        public CSteamID SteamID;
        [SyncVar]
        public int PlayerSlotIndex = -1;
        [SyncVar]
        public string ColorID;
        public string GetColorID => string.IsNullOrEmpty(ColorID) ? "gray" : ColorID;

        public static event Action PlayerReadyStatusChanged;
        public static event Action PlayerSteamInfoDetermined;
        public event Action<ulong, int> PlayerSwappedToSlot;

        [Command(requiresAuthority = false)]
        private void CmdSetSteamIDs(CSteamID newSteamID, string newDisplayName) {
            SteamID = newSteamID;
            DisplayName = newDisplayName;

            // TODO this is a little yucky. We need to wait to set this so that the SyncVar for setting the SteamID gets sent out first, that way on the client the SteamID will be set when trying to find the player to assign to the player slot. It might be better to have the LobbyNetworkBehaviour.RpcPlayerSlotsAssigned call send the info for the joining player? 
            AssignPlayerSlotsAfterDelay();
        }

        private async void AssignPlayerSlotsAfterDelay() {
            await Task.Yield();
            LobbyNetworkBehaviour.Instance.RoomMenu.AssignPlayerSlots();
        }
        
        private void OnDisplayNameSet(string oldName, string newName) {
            PlayerSteamInfoDetermined?.Invoke();
        }
        
        [Command(requiresAuthority = false)]
        public void CmdSetColor(string newColorID, int newSlotIndexOverride, bool unassignFromOthersIfTaken) {
            if (!LobbyNetworkBehaviour.Instance.IsColorAvailable(newColorID) && !unassignFromOthersIfTaken) return;
            ColorID = newColorID;
            LobbyNetworkBehaviour.Instance.AssignColor(this, newSlotIndexOverride, newColorID);
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

        [Command(requiresAuthority = false)]
        private void CmdDoKick() {
            netIdentity.connectionToClient.Disconnect();
        }

        [Command(requiresAuthority = false)]
        public void CmdSwapToSlot(int slotIndex) {
            index = slotIndex;
            
            // Swapping un-readies the player 
            CmdChangeReadyState(false);
            
            // Assign the color if the player has the neutral color
            if (ColorID == "gray") {
                CmdSetColor(LobbyNetworkBehaviour.Instance.RoomMenu.GetColorToAssign(slotIndex).ID, slotIndex, true);
            }
            
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
