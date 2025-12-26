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
        private string _lastReplayID;
        private Random _random;
        
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
            List<ReplayData> eligibleReplays = _replays.Where(r => r.replayID != _lastReplayID).ToList();
            return eligibleReplays.ElementAt(_random.Next(eligibleReplays.Count));
        }

        private void SwitchMap(ReplayData replayData) {
            SceneLoader.Instance.SwitchLoadedMap(replayData.mapID, replayData.replayID);
        }
    }
}