using Gameplay.Config;
using Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace Menu {
    /// <summary>
    /// Handles displaying the map in the lobby (zooming, panning, window setup)
    /// </summary>
    public class LobbyMapDisplayer : MonoBehaviour {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _contentRect;
        [SerializeField] private Image _mapImage;
        [SerializeField] private RectTransform _mapRect;
        [SerializeField] private Image _zoomButtonFill;
        
        [SerializeField] private float _nonZoomedScale = 1f;
        [SerializeField] private float _zoomedScale = 1.5f;

        private MapImagesConfiguration _mapImagesConfiguration;
        private bool _zoomed;

        public void Initialize(GameConfiguration gameConfiguration, LobbyNetworkBehaviour lobbyNetworkBehaviour) {
            _mapImagesConfiguration = gameConfiguration.MapImagesConfiguration;
            
            lobbyNetworkBehaviour.MapChanged += UpdateSelectedMap;
            string initialMapID = string.IsNullOrEmpty(lobbyNetworkBehaviour.MapID) 
                ? SceneLoader.DefaultMap 
                : lobbyNetworkBehaviour.MapID;
            UpdateSelectedMap(initialMapID); 
        }

        private void UpdateSelectedMap(string mapID) {
            SetMapSprite(_mapImagesConfiguration.GetFullMapImage(mapID));
        }
        
        private void SetMapSprite(Sprite mapSprite) {
            _mapImage.sprite = mapSprite;

            // Set the content size to match the map size
            float heightMultiplier = _mapRect.rect.height / mapSprite.rect.height;
            _contentRect.sizeDelta = new Vector2(mapSprite.rect.width * heightMultiplier - 1, _mapRect.rect.height - 1);
            
            // Set the scroll rect to the center of the map
            _scrollRect.content.anchoredPosition = Vector2.zero;

            SetZoom(false);
        }

        public void ToggleZoom() {
            SetZoom(!_zoomed);
        }

        private void SetZoom(bool zoomed) {
            _zoomed = zoomed;
            _contentRect.localScale = Vector3.one * (zoomed ? _zoomedScale : _nonZoomedScale);
            
            // Update the zoom button sprite
            _zoomButtonFill.gameObject.SetActive(!zoomed);
        }
    }
}