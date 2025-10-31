using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles playing misc audio during gameplay. Keeps track of state of last played sounds. 
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
        private Dictionary<string, AudioFile> _lastPlayedSelectionSounds = new Dictionary<string, AudioFile>();
        private Dictionary<string, AudioFile> _lastPlayedOrderSounds = new Dictionary<string, AudioFile>();
        private Dictionary<string, AudioFile> _lastPlayedAttackSounds = new Dictionary<string, AudioFile>();
        
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
            ChooseAndPlayEntitySound(entityData.ID, entityData.SelectionSounds, _lastPlayedSelectionSounds);
        }

        public void EntityOrderSound(EntityData entityData) {
            ChooseAndPlayEntitySound(entityData.ID, entityData.OrderSounds, _lastPlayedOrderSounds);
        }

        public void EntityAttackSound(EntityData entityData) {
            ChooseAndPlayEntitySound(entityData.ID, entityData.AttackSounds, _lastPlayedAttackSounds);
        }

        private void ChooseAndPlayEntitySound(string entityType, List<AudioFile> audioFiles, Dictionary<string, AudioFile> lastPlayedSounds) {
            if (audioFiles.Count == 0) return;

            AudioFile soundToPlay;
            List<AudioFile> soundsToPickFrom  = new List<AudioFile>(audioFiles);
            if (soundsToPickFrom.Count == 1) {
                soundToPlay = soundsToPickFrom[0];
            } else {
                if (lastPlayedSounds.TryGetValue(entityType, out AudioFile soundToExclude)) {
                    // Don't pick the last played sound for this entity type
                    soundsToPickFrom = audioFiles.Where(a => a != soundToExclude).ToList();
                }
                soundToPlay = soundsToPickFrom[Random.Range(0, soundsToPickFrom.Count)];
            }
            
            lastPlayedSounds[entityType] = soundToPlay;
            AudioPlayer.TryPlaySFX(soundToPlay);
        }

        public void ArrowLandSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.ArrowLandSound);
        }

        public void ConstructionSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.ConstructionSound);
        }

        public void EntityFinishedBuildingSound(EntityData entityData) {
            if (entityData.EntityFinishedBuildingSound.Clip != null) {
                AudioPlayer.TryPlaySFX(entityData.EntityFinishedBuildingSound);
            }
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

        public void UpgradeCompleteSound() {
            AudioPlayer.TryPlaySFX(_audioConfiguration.UpgradeCompleteSound);
        }
    }
}