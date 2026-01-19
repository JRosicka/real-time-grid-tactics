using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using JetBrains.Annotations;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles playing specific audio. Keeps track of state of last played sounds.
    /// All audio playing should go through this. 
    /// </summary>
    public class GameAudio {
        private const float MinAmountOfTimeBetweenSameSounds = 1f;
        
        public static GameAudio Instance;
        
        private readonly AudioPlayer _audioPlayer;
        private readonly AudioFileConfiguration _audioConfiguration;
        private readonly Dictionary<string, AudioFile> _lastPlayedSelectionSounds = new Dictionary<string, AudioFile>();
        private readonly Dictionary<string, AudioFile> _lastPlayedOrderSounds = new Dictionary<string, AudioFile>();
        private readonly Dictionary<string, AudioFile> _lastPlayedAttackSounds = new Dictionary<string, AudioFile>();
        private readonly Dictionary<string, float> _sfxCooldownTimes = new Dictionary<string, float>();
        
        public GameAudio(AudioPlayer audioPlayer, AudioFileConfiguration audioConfiguration) {
            _audioPlayer = audioPlayer;
            _audioConfiguration = audioConfiguration;
            Instance = this;
        }

        public void Update(float deltaTime) {
            foreach (string audioFile in _sfxCooldownTimes.Keys.ToList()) {
                _sfxCooldownTimes[audioFile] -= deltaTime;
                if (_sfxCooldownTimes[audioFile] <= 0) {
                    _sfxCooldownTimes.Remove(audioFile);
                }
            }
        }
        
        public void StartMusic() {
            if (_audioConfiguration.GameMusic.Clip == null) return;
            _audioPlayer.PlayMusic(_audioConfiguration.GameMusic);
        }
        
        public void EndMusic(bool fadeOut) {
            _audioPlayer.EndMusic(fadeOut);
        }

        public void ButtonClickDownSound() {
            TryPlaySFX(_audioConfiguration.ButtonClickDownSound, AudioClipName(_audioConfiguration.ButtonClickDownSound));
        }

        public void ButtonClickUpSound() {
            TryPlaySFX(_audioConfiguration.ButtonClickUpSound, AudioClipName(_audioConfiguration.ButtonClickUpSound));
        }

        public void InvalidSound() {
            TryPlaySFX(_audioConfiguration.InvalidSound, AudioClipName(_audioConfiguration.InvalidSound));
        }

        public void EntitySelectionSound(EntityData entityData) {
            ChooseAndPlayEntitySound(entityData, entityData.SelectionSounds, _lastPlayedSelectionSounds, "selection");
        }

        public void EntityOrderSound(EntityData entityData) {
            ChooseAndPlayEntitySound(entityData, entityData.OrderSounds, _lastPlayedOrderSounds, "order");
        }

        public void EntityAttackSound(EntityData entityData) {
            ChooseAndPlayEntitySound(entityData, entityData.AttackSounds, _lastPlayedAttackSounds, "attack");
        }
        
        public void AbilitySelectSound(IAbilityData abilityData) {
            TryPlaySFX(abilityData.SelectionSound, AudioClipName(abilityData.SelectionSound));
        }
        
        public void AbilityTargetedSound(ITargetableAbilityData abilityData) {
            TryPlaySFX(abilityData.TargetedSound, AudioClipName(abilityData.TargetedSound));
        }

        public void AbilityPerformedSound(IAbilityData abilityData) {
            TryPlaySFX(abilityData.PerformedSound, AudioClipName(abilityData.PerformedSound)); 
        }

        private void ChooseAndPlayEntitySound(EntityData entityData, List<AudioFile> audioFiles, Dictionary<string, AudioFile> lastPlayedSounds, string audioPlacement) {
            if (audioFiles.Count == 0) return;

            AudioFile soundToPlay;
            List<AudioFile> soundsToPickFrom  = new List<AudioFile>(audioFiles);
            if (soundsToPickFrom.Count == 1) {
                soundToPlay = soundsToPickFrom[0];
            } else {
                if (lastPlayedSounds.TryGetValue(entityData.ID, out AudioFile soundToExclude)) {
                    // Don't pick the last played sound for this entity type
                    soundsToPickFrom = audioFiles.Where(a => a != soundToExclude).ToList();
                }
                soundToPlay = soundsToPickFrom[Random.Range(0, soundsToPickFrom.Count)];
            }
            
            lastPlayedSounds[entityData.ID] = soundToPlay;
            TryPlaySFX(soundToPlay, $"{entityData.AudioPlacementPrefix}_{audioPlacement}");
        }

        public void ArrowLandSound() {
            TryPlaySFX(_audioConfiguration.ArrowLandSound, AudioClipName(_audioConfiguration.ArrowLandSound));
        }

        public void ConstructionSound() {
            TryPlaySFX(_audioConfiguration.ConstructionSound, AudioClipName(_audioConfiguration.ConstructionSound));
        }

        public void EntityFinishedBuildingSound(EntityData entityData) {
            if (entityData.EntityFinishedBuildingSound.Clip != null) {
                TryPlaySFX(entityData.EntityFinishedBuildingSound, AudioClipName(entityData.EntityFinishedBuildingSound));
            }
        }

        public void GameStartSound() {
            TryPlaySFX(_audioConfiguration.GameStartSound, AudioClipName(_audioConfiguration.GameStartSound));
        }
        
        public void GameWinSound() {
            TryPlaySFX(_audioConfiguration.GameWinSound, AudioClipName(_audioConfiguration.GameWinSound));
        }
        
        public void GameLossSound() {
            TryPlaySFX(_audioConfiguration.GameLossSound, AudioClipName(_audioConfiguration.GameLossSound));
        }

        public void UpgradeCompleteSound() {
            TryPlaySFX(_audioConfiguration.UpgradeCompleteSound, AudioClipName(_audioConfiguration.UpgradeCompleteSound));
        }

        private void TryPlaySFX(AudioFile audioFile, string audioPlacement) {
            if (audioFile == null || audioFile.Clip == null) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.ReplayManager.PlayingReplay && !audioFile.PlayDuringReplay) return;
            if (!audioFile.AllowInQuickSuccession) {
                if (_sfxCooldownTimes.ContainsKey(audioPlacement)) return;
                _sfxCooldownTimes[audioPlacement] = MinAmountOfTimeBetweenSameSounds;
            }
            _audioPlayer.TryPlaySFX(audioFile);
        }

        [CanBeNull] private string AudioClipName(AudioFile audioFile) {
            if (audioFile == null || audioFile.Clip == null) return null;
            return audioFile.Clip.name;
        }
    }
}