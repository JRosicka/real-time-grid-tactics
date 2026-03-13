using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Game.Network;
using Gameplay.Config;
using Mirror;
using Scenes;
using Steamworks;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// Networked logic for the lobby
    /// TODO LobbyNetworkBehaviour, RoomMenu, and GameNetworkPlayer are all spaghettified, reaching into each other and each doing networking and view logic. Gross. 
    /// </summary>
    public class LobbyNetworkBehaviour : NetworkBehaviour {
        public static LobbyNetworkBehaviour Instance { get; private set; }
        private void Start() {
            Instance = this;
            if (isServer) {
                MusicSeed = new System.Random().Next();
                GameAudio.Instance.SetMusicSeed(MusicSeed);
            } else {
                SetMusicSeed(-1, MusicSeed);
            }
        }
        
        public event Action<CSteamID, int, string> PlayerColorAssigned;
        // Client event
        public event Action PlayerSlotsAssigned;
        
        [SyncVar(hook = nameof(SetMusicSeed))]
        public int MusicSeed;

        private void SetMusicSeed(int _, int newSeed) {
            GameAudio.Instance.SetMusicSeed(newSeed);
        }

        [SyncVar(hook = nameof(OnMapChanged))]
        public string MapID = SceneLoader.Instance.LastLobbyMap;

        [SerializeField] private List<PlayerColorData> _availableColors;
        private readonly Dictionary<CSteamID, PlayerColorData> _assignedColors = new Dictionary<CSteamID, PlayerColorData>();

        public RoomMenu RoomMenu { get; private set; }
        
        public void Initialize(RoomMenu roomMenu) {
            RoomMenu = roomMenu;
        }
        
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
        
        public bool IsColorAvailable(string colorID) {
            return !_assignedColors.Values.Select(c => c.ID).Contains(colorID);
        }
        
        [Server]
        public void AssignColor(GameNetworkPlayer player, int slotIndex, string colorID) {
            _assignedColors[player.SteamID] = _availableColors.First(c => c.ID == colorID);
            RpcAssignColor(player.SteamID, slotIndex, colorID);
            
            // Unassign color from other players if they had it
            List<GameNetworkPlayer> playersToRemove = new List<GameNetworkPlayer>();
            foreach (var assignColor in _assignedColors.Where(c => c.Key != player.SteamID)) {
                GameNetworkPlayer otherPlayer = RoomMenu.PlayersInLobby.FirstOrDefault(p => p.SteamID == assignColor.Key);
                if (otherPlayer?.GetColorID == colorID) {
                    playersToRemove.Add(otherPlayer);
                }
            }
            
            foreach (GameNetworkPlayer playerToRemove in playersToRemove) {
                _assignedColors.Remove(playerToRemove.SteamID);
                playerToRemove.ColorID = "gray";
                RpcAssignColor(playerToRemove.SteamID, -1, "gray");
            }
        }
        
        [ClientRpc]
        private void RpcAssignColor(CSteamID playerID, int slotIndex, string colorID) {
            PlayerColorAssigned?.Invoke(playerID, slotIndex, colorID);
        }
        
        [ClientRpc]
        public void RpcPlayerSlotsAssigned() {
            PlayerSlotsAssigned?.Invoke();
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