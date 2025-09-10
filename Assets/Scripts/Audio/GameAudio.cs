using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles playing misc audio during gameplay
    /// </summary>
    public class GameAudio : MonoBehaviour {
        private AudioPlayer _audioPlayer;
        private AudioPlayer AudioPlayer {
            get {
                if (_audioPlayer == null) {
                    List<AudioPlayer> audioPlayers = FindObjectsOfType<AudioPlayer>().ToList();
                    _audioPlayer = audioPlayers.First(a => a.ActivePlayer);
                }
                return _audioPlayer;
            }
        }
        
        private GameSetupManager _gameSetupManager;
        private AudioFileConfiguration _audioConfiguration;
        
        public void Initialize(GameSetupManager gameSetupManager, AudioFileConfiguration audioConfiguration) {
            _gameSetupManager = gameSetupManager;
            _audioConfiguration = audioConfiguration;
            
            if (gameSetupManager.GameInitialized) {
                StartMusic();
            } else {
                gameSetupManager.GameInitializedEvent += StartMusic;
            }
        }

        public void UnregisterListeners() {
            _gameSetupManager.GameInitializedEvent -= StartMusic;
        }

        private void StartMusic() {
            AudioPlayer.PlayMusic(_audioConfiguration.GameMusic);
        }
        
        public void EndMusic(bool fadeOut) {
            AudioPlayer.EndMusic(fadeOut);
        }

        public void ButtonClickSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.ButtonClickSound);
        }

        public void InvalidSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.InvalidSound);
        }

        public void EntitySelectionSound(EntityData entityData) {
            AudioPlayer.TryPlaySFX(entityData.SelectionSound);
        }

        public void GameStartSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.GameStartSound);
        }
        
        public void GameWinSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.GameWinSound);
        }
        
        public void GameLossSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.GameLossSound);
        }
    }
}