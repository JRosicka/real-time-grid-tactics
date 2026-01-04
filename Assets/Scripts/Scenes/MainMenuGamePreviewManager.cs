using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;

namespace Scenes {
    /// <summary>
    /// Handles cycling through the game previews in game scene loaded in the background of the main menu
    /// </summary>
    public class MainMenuGamePreviewManager {
        private List<ReplayData> _replays;
        private bool _mainMenuLoaded;
        private Random _random;
        
        public ReplayData LastReplay { get; private set; }
        
        public void Initialize(bool setInitialMap) {
            _replays = GameConfigurationLocator.GameConfiguration.MapsConfiguration.PreviewReplays;
            if (setInitialMap) {
                ReplayData replay = GetNextReplay();
                GameTypeTracker.Instance.SetMap(replay.mapID, replay.replayID);
            }
            _random = new Random();
        }
        
        public void SwitchToNextMap() {
            SwitchMap(GetNextReplay());
        }

        public void PickNextMap() {
            ReplayData nextReplay = GetNextReplay();
            GameTypeTracker.Instance.SetMap(nextReplay.mapID, nextReplay.replayID);
        }

        private ReplayData GetNextReplay() {
            List<ReplayData> eligibleReplays = _replays.Where(r => r != LastReplay).ToList();
            LastReplay = eligibleReplays.ElementAt(_random.Next(eligibleReplays.Count));
            return LastReplay;
        }

        private void SwitchMap(ReplayData replayData) {
            SceneLoader.Instance.SwitchLoadedMap(replayData.mapID, replayData.replayID, false);
        }
    }
}