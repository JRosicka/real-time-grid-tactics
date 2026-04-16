using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private const int PlayerSlotCount = 2;
        private const float TooCloseMapSwitchProximityTime = .5f;

        private bool _mapLoadingLocked;
        
        public static LobbyNetworkBehaviour Instance { get; private set; }
        private void Start() {
            Instance = this;
            if (netIdentity && isServer) {
                MusicSeed = new System.Random().Next();
                GameAudio.Instance.SetMusicSeed(MusicSeed);
            } else {
                SetMusicSeed(-1, MusicSeed);
            }
        }
        
        // Client events
        public event Action<CSteamID, int, string> PlayerColorAssigned;
        public event Action PlayerSlotsAssigned;
        public event Action<string> MapChanged;
        
        [SyncVar(hook = nameof(SetMusicSeed))]
        public int MusicSeed;

        private void SetMusicSeed(int _, int newSeed) {
            GameAudio.Instance.SetMusicSeed(newSeed);
        }

        [SyncVar(hook = nameof(OnMapChanged))]
        public string MapID;

        [SerializeField] private List<PlayerColorData> _availableColors;
        private readonly Dictionary<GameNetworkPlayer, PlayerColorData> _assignedColors = new Dictionary<GameNetworkPlayer, PlayerColorData>();

        private PlayerColorData neutralColor => GameConfigurationLocator.GameConfiguration.NeutralColors;

        public RoomMenu RoomMenu { get; private set; }
        
        public void Initialize(RoomMenu roomMenu) {
            RoomMenu = roomMenu;

            SceneLoader.Instance.SceneLoaded += SceneLoaded;
            if (netIdentity && isServer) {
                // Set the map tracked in the lobby
                string mapToLoad = string.IsNullOrEmpty(SceneLoader.Instance.LastLobbyMap) 
                    ? SceneLoader.DefaultMap 
                    : SceneLoader.Instance.LastLobbyMap;
                GameTypeTracker.Instance.SetMap(mapToLoad);
                TrySwitchMap(mapToLoad);

                GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
                if (gameNetworkManager != null) {
                    gameNetworkManager.RoomServerDidDisconnectAction += UnassignColorsForDisconnectedPlayers;
                }
            } else {
                GameTypeTracker.Instance.SetMap(MapID);
            }
        }

        private void OnDestroy() {
            SceneLoader.Instance.SceneLoaded -= SceneLoaded;
            GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
            if (gameNetworkManager != null) {
                gameNetworkManager.RoomServerDidDisconnectAction -= UnassignColorsForDisconnectedPlayers;
            }
        }

        /// <summary>
        /// Set the map and update the clients 
        /// </summary>
        [Server]
        public void TrySwitchMap(string mapID) {
            if (_mapLoadingLocked) return;
            if (!string.IsNullOrEmpty(MapID) && (!NetworkManager.singleton || !NetworkManager.singleton.CanChangeScene)) {
                return;
            }
            _mapLoadingLocked = true;
            
            string oldMapID = MapID;
            MapID = mapID;
            OnMapChanged(oldMapID, mapID);
        }
        
        private void OnMapChanged(string oldMapID, string newMapID) {
            // If we are the host and don't have a current map ID, then don't bother switching the map since we already 
            // loaded it when loading into the lobby scene.
            if ((NetworkServer.active && string.IsNullOrEmpty(oldMapID)) || oldMapID == newMapID) {
                _mapLoadingLocked = false;
                MapChanged?.Invoke(newMapID);
                return;
            }
            SceneLoader.Instance.SwitchLoadedMap(newMapID, null, true);
            MapChanged?.Invoke(newMapID);
        }

        private async void SceneLoaded(string newScene) {
            if (SceneLoader.StrippedSceneName(newScene) != SceneLoader.GameSceneName) return;

            await Task.Delay(TimeSpan.FromSeconds(TooCloseMapSwitchProximityTime));
            _mapLoadingLocked = false;
        }
        
        [Server]
        public bool IsColorAvailable(string colorID) {
            if (colorID == neutralColor.ID) return true;
            foreach (var kvp in _assignedColors) {
                if (kvp.Value.ID == colorID && kvp.Key.PlayerSlotIndex < PlayerSlotCount) {
                    return false;
                }
            }

            return true;
        }
        
        [Server]
        public void AssignColor(GameNetworkPlayer player, int slotIndex, string colorID) {
            _assignedColors[player] = _availableColors.FirstOrDefault(c => c.ID == colorID) ?? neutralColor;
            RpcAssignColor(player.SteamID, slotIndex, colorID);

            if (colorID != "gray") {
                // Unassign color from other players if they had it
                List<GameNetworkPlayer> playersToRemove = new List<GameNetworkPlayer>();
                foreach (var assignColor in _assignedColors.Where(c => c.Key.SteamID != player.SteamID)) {
                    GameNetworkPlayer otherPlayer = RoomMenu.PlayersInLobby.FirstOrDefault(p => p == assignColor.Key);
                    if (otherPlayer?.GetColorID == colorID) {
                        playersToRemove.Add(otherPlayer);
                    }
                }
                foreach (GameNetworkPlayer playerToRemove in playersToRemove) {
                    _assignedColors.Remove(playerToRemove);
                    playerToRemove.ColorID = "gray";
                    RpcAssignColor(playerToRemove.SteamID, -1, "gray");
                }
            }
        }
        
        [ClientRpc]
        private void RpcAssignColor(CSteamID playerID, int slotIndex, string colorID) {
            PlayerColorAssigned?.Invoke(playerID, slotIndex, colorID);
        }

        private async void UnassignColorsForDisconnectedPlayers(NetworkConnectionToClient networkConnectionToClient) {
            // Wait to make sure that the player has left the room
            await Task.Delay(100);
            
            List<CSteamID> playersInLobby = RoomMenu.PlayersInLobby.Select(p => p.SteamID).ToList();
            for (int i = _assignedColors.Count - 1; i >= 0; i--) {
                GameNetworkPlayer player = _assignedColors.Keys.ElementAt(i);
                if (!playersInLobby.Contains(player.SteamID)) {
                    _assignedColors.Remove(player);
                }
            }
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