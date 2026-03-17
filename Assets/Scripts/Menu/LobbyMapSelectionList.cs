using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Scenes;
using TMPro;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// Handles populating the list of available maps and allowing the lobby host to switch between them
    /// </summary>
    public class LobbyMapSelectionList : MonoBehaviour {
        [SerializeField] private LobbyMapEntryView _mapEntryPrefab;
        [SerializeField] private GameObject _mapListParent;
        [SerializeField] private TMP_Text _selectedMap;
        [SerializeField] private TMP_Text _description;
        
        private readonly List<LobbyMapEntryView> _mapEntries = new List<LobbyMapEntryView>();

        // Client initialization
        public void Initialize(GameConfiguration gameConfiguration, LobbyNetworkBehaviour lobbyNetworkBehaviour) {
            foreach (MapData mapData in gameConfiguration.MapsConfiguration.PlayableMaps.OrderBy(m => m.index)) {
                Sprite mapPreviewImage = gameConfiguration.MapImagesConfiguration.GetMapPreviewImage(mapData.mapID);
                AddMapEntry(mapData, mapPreviewImage, lobbyNetworkBehaviour);
            }
            
            lobbyNetworkBehaviour.MapChanged += UpdateSelectedMap;
            string initialMapID = string.IsNullOrEmpty(lobbyNetworkBehaviour.MapID) 
                ? SceneLoader.DefaultMap 
                : lobbyNetworkBehaviour.MapID;
            UpdateSelectedMap(initialMapID); 
        }
        
        private void AddMapEntry(MapData mapData, Sprite mapPreviewImage, LobbyNetworkBehaviour lobbyNetworkBehaviour) {
            LobbyMapEntryView newEntry = Instantiate(_mapEntryPrefab, _mapListParent.transform);
            newEntry.Initialize(mapData, mapPreviewImage, lobbyNetworkBehaviour);
            _mapEntries.Add(newEntry);
        }
        
        private void UpdateSelectedMap(string mapID) {
            foreach (LobbyMapEntryView entry in _mapEntries) {
                entry.SetSelected(entry.MapData.mapID == mapID);
                if (entry.MapData.mapID == mapID) {
                    _selectedMap.text = entry.MapData.displayName;
                    _description.text = entry.MapData.description;
                }
            }
        }
    }
}