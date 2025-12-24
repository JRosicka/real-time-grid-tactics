using System.Collections.Generic;
using Gameplay.Config;

namespace Scenes {
    /// <summary>
    /// Handles cycling through the game previews in game scene loaded in the background of the main menu
    /// </summary>
    public class MainMenuGamePreviewManager {
        private List<MapData> _previewMaps;
        private bool _mainMenuLoaded;
        
        public void Initialize(bool setInitialMap) {
            _previewMaps = GameConfigurationLocator.GameConfiguration.MapsConfiguration.PreviewMaps;
            if (setInitialMap) {
                GameTypeTracker.Instance.SetMap(GetNextPreviewMap());
            }
        }
        
        public void SwitchToNextMap() {
            SwitchMap(GetNextPreviewMap());
        }

        public void PickNextMap() {
            GameTypeTracker.Instance.SetMap(GetNextPreviewMap());
        }

        private string GetNextPreviewMap() {
            // TODO look at preview maps instead
            string currentLoadedMap = GameTypeTracker.Instance.MapID;
            return currentLoadedMap switch {
                "origins" => "mountainPass",
                "mountainPass" => "oakcrest",
                "oakcrest" => "origins",
                _ => "origins"
            };
        }

        private void SwitchMap(string mapID) {
            SceneLoader.Instance.SwitchLoadedMap(mapID);
        }
    }
}