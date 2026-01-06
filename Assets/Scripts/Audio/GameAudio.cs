using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
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
                    _audioPlayer = FindFirstObjectByType<AudioPlayer>();
                }
                return _audioPlayer;
            }
        }
        
        private GameSetupManager _gameSetupManager;
        private AudioFileConfiguration _audioConfiguration;
        private readonly Dictionary<string, AudioFile> _lastPlayedSelectionSounds = new Dictionary<string, AudioFile>();
        private readonly Dictionary<string, AudioFile> _lastPlayedOrderSounds = new Dictionary<string, AudioFile>();
        private readonly Dictionary<string, AudioFile> _lastPlayedAttackSounds = new Dictionary<string, AudioFile>();
        
        public void Initialize(GameSetupManager gameSetupManager, AudioFileConfiguration audioConfiguration) {
            _gameSetupManager = gameSetupManager;
            _audioConfiguration = audioConfiguration;
            
            if (gameSetupManager.GameRunning) {
                StartMusic();
            } else {
                gameSetupManager.GameRunningEvent += StartMusic;
            }
        }

        public void UnregisterListeners() {
            if (_gameSetupManager) {
                _gameSetupManager.GameRunningEvent -= StartMusic;
            }
        }

        private void StartMusic() {
            if (_audioConfiguration.GameMusic.Clip == null) return;
            AudioPlayer.PlayMusic(_audioConfiguration.GameMusic);
        }
        
        public void EndMusic(bool fadeOut) {
            AudioPlayer.EndMusic(fadeOut);
        }

        public void ButtonClickSound() {
            TryPlaySFX(_audioConfiguration.ButtonClickSound);
        }

        public void InvalidSound() {
            TryPlaySFX(_audioConfiguration.InvalidSound);
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
        
        public void AbilitySelectSound(IAbilityData abilityData) {
            TryPlaySFX(abilityData.SelectionSound);
        }
        
        public void AbilityTargetedSound(ITargetableAbilityData abilityData) {
            TryPlaySFX(abilityData.TargetedSound);
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
            TryPlaySFX(soundToPlay);
        }

        public void ArrowLandSound() {
            TryPlaySFX(_audioConfiguration.ArrowLandSound);
        }

        public void ConstructionSound() {
            TryPlaySFX(_audioConfiguration.ConstructionSound);
        }

        public void EntityFinishedBuildingSound(EntityData entityData) {
            if (entityData.EntityFinishedBuildingSound.Clip != null) {
                TryPlaySFX(entityData.EntityFinishedBuildingSound);
            }
        }

        public void GameStartSound() {
            TryPlaySFX(_audioConfiguration.GameStartSound);
        }
        
        public void GameWinSound() {
            TryPlaySFX(_audioConfiguration.GameWinSound);
        }
        
        public void GameLossSound() {
            TryPlaySFX(_audioConfiguration.GameLossSound);
        }

        public void UpgradeCompleteSound() {
            TryPlaySFX(_audioConfiguration.UpgradeCompleteSound);
        }

        private void TryPlaySFX(AudioFile audioFile) {
            if (audioFile == null || audioFile.Clip == null) return;
            if (GameManager.Instance.ReplayManager.PlayingReplay && !audioFile.PlayDuringReplay) return;
            AudioPlayer.TryPlaySFX(audioFile);
        }
    }
}