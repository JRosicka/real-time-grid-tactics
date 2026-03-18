using System.Collections.Generic;
using Gameplay.Config;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Menu {
    /// <summary>
    /// Handles displaying details about a given map in the lobby, and allowing the host to select it 
    /// </summary>
    public class LobbyMapEntryView : MonoBehaviour {
        [SerializeField] private TMP_Text _mapNameText;
        [SerializeField] private Image _mapPreviewImage;
        [SerializeField] private List<Image> _coloredImages;
        [SerializeField] private Color _selectedColor;
        [SerializeField] private Color _unselectedColor;
        [SerializeField] private ButtonDim _buttonDim;
        
        public MapData MapData { get; private set; }
        private LobbyNetworkBehaviour _lobbyNetworkBehaviour;
        
        public void Initialize(MapData mapData, Sprite mapPreviewImage, LobbyNetworkBehaviour lobbyNetworkBehaviour) {
            MapData = mapData;
            _lobbyNetworkBehaviour = lobbyNetworkBehaviour;
            
            _mapNameText.text = mapData.displayName;
            _mapPreviewImage.sprite = mapPreviewImage;
            
            _buttonDim.Interactable = NetworkServer.active;
        }

        public void SetSelected(bool selected) {
            _coloredImages.ForEach(i => i.color = selected ? _selectedColor : _unselectedColor);
            _buttonDim.UnDim();
        }

        public void SelectMap() {
            // Don't do anything if this is a client -- only the host can switch maps
            if (!NetworkServer.active) {
                return;
            }
            
            _lobbyNetworkBehaviour.TrySwitchMap(MapData.mapID);
        }
    }
}