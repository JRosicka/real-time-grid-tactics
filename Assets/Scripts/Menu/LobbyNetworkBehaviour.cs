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

        [SyncVar(hook = nameof(SetMusicSeed))]
        public int MusicSeed;

        private void SetMusicSeed(int _, int newSeed) {
            GameAudio.Instance.SetMusicSeed(newSeed);
        }

        [SyncVar(hook = nameof(OnMapChanged))]
        public string MapID = SceneLoader.Instance.LastLobbyMap;

        [SerializeField] private List<PlayerColorData> _availableColors;
        private readonly Dictionary<CSteamID, PlayerColorData> _assignedColors = new Dictionary<CSteamID, PlayerColorData>();
        
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
        public void AssignColor(GameNetworkPlayer player, string colorID) {
            _assignedColors[player.SteamID] = _availableColors.First(c => c.ID == colorID);
            RpcAssignColor(player.SteamID, colorID);
        }
        
        [ClientRpc]
        private void RpcAssignColor(CSteamID playerID, string colorID) {
            // TODO: trigger event that the color views listen to so they can update the color visual
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