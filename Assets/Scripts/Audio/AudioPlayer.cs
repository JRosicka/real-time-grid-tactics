using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles interacting with the <see cref="AudioManager"/> to play audio and change volume.
    /// Also handles keeping track of an interruptible SFX. 
    /// </summary>
    public class AudioPlayer : MonoBehaviour {
        public bool ActivePlayer { get; private set; }

        private bool AudioEnabled => GameManager.Instance.Configuration.AudioConfiguration.AudioEnabled;

        [SerializeField] private AudioManager _audioManager;
        private OneShotAudio _interruptibleSFX;
        private OneShotAudio _activeMusic;

        private void Awake() {
            ActivePlayer = _audioManager.Initialize();
        }

        /// <summary>
        /// Try to play a sound effect. Do not call this for game music.
        ///
        /// If the audio file is interruptible, play and set as the interruptible instance, but only if its priority
        /// is higher than the currently playing SFX instance (otherwise no-op). Otherwise just play the SFX.
        /// </summary>
        /// <param name="audioFile">The audio to play</param>
        public void TryPlaySFX(AudioFile audioFile) {
            if (!AudioEnabled) return;
            
            if (audioFile.Interruptible) {
                int priorityOfCurrentSFX = _interruptibleSFX?.Priority ?? int.MinValue;
                int priorityOfNewSFX = AudioManager.GetLayerPriority(audioFile.AudioLayer);
                if (priorityOfNewSFX < priorityOfCurrentSFX) return;

                if (_interruptibleSFX != null) {
                    _audioManager.CancelAudio(_interruptibleSFX);
                }

                _interruptibleSFX = _audioManager.PlaySound(audioFile, false);
            } else {
                _audioManager.PlaySound(audioFile, false);
            }
        }

        public void PlayMusic(AudioFile audioFile) {
            if (!AudioEnabled) return;
            
            if (_activeMusic != null) {
                _audioManager.CancelAudio(_activeMusic);
            }
            _activeMusic = _audioManager.PlaySound(audioFile, true);
        }
        
        public void SetSoundEffectVolume(float newVolume) {
            _audioManager.SetSoundEffectVolume(newVolume);
        }
        
        public void SetMusicVolume(float newVolume) {
            _audioManager.SetMusicVolume(newVolume);
        }
    }
}