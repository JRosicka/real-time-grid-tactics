using System.Threading.Tasks;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles interacting with the <see cref="AudioManager"/> to play audio and change volume.
    /// Also handles keeping track of an interruptible SFX. 
    /// </summary>
    public class AudioPlayer : MonoBehaviour {
        [SerializeField] private AudioManager _audioManager;
        private OneShotAudio _interruptibleSFX;
        private long _sfxPerformerID;
        private OneShotAudio _activeMusic;

        public void Initialize() {
            _audioManager.Initialize();
            SetStartingVolume();
        }
        
        private async void SetStartingVolume() {
            // Delay a frame since we need to wait for the audio mixer to finish loading
            await Task.Yield();
            
            SetSoundEffectVolume(PlayerPrefs.GetFloat(PlayerPrefsKeys.SoundEffectVolumeKey, PlayerPrefsKeys.DefaultVolume));
            SetVoiceLineVolume(PlayerPrefs.GetFloat(PlayerPrefsKeys.VoiceLineVolumeKey, PlayerPrefsKeys.DefaultVolume));
            SetMusicVolume(PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolumeKey, PlayerPrefsKeys.DefaultVolume));
        }

        /// <summary>
        /// Try to play a sound effect. Do not call this for game music.
        /// 
        /// If the audio file is interruptible, play and set as the interruptible instance, but only if its priority
        /// is higher than the currently playing SFX instance (otherwise no-op). Otherwise just play the SFX.
        /// </summary>
        /// <param name="audioFile">The audio to play</param>
        /// <param name="sfxPerformerID">UID of whatever is performing the SFX. Ignored if -1, otherwise an interruptible
        /// SFX by the same performer will not be played if there is already one playing.</param>
        public void TryPlaySFX(AudioFile audioFile, long sfxPerformerID) {
            if (audioFile.Interruptible) {
                if (sfxPerformerID != -1) {
                    if (_interruptibleSFX is { IsPlaying: true }) {
                        // Don't play the sound if we are playing another interruptible one by the same performer
                        if (_sfxPerformerID == sfxPerformerID) return;
                    }
                    _sfxPerformerID = sfxPerformerID;
                }
                
                int priorityOfCurrentSFX = _interruptibleSFX?.Priority ?? int.MinValue;
                int priorityOfNewSFX = AudioManager.GetLayerPriority(audioFile.AudioLayer);
                if (priorityOfNewSFX < priorityOfCurrentSFX) return;

                if (_interruptibleSFX != null) {
                    _audioManager.CancelAudio(_interruptibleSFX, false);
                }

                _interruptibleSFX = _audioManager.PlaySound(audioFile, false, true);
            } else {
                _audioManager.PlaySound(audioFile, false, false);
            }
        }

        public void PlayMusic(AudioFile audioFile) {
            if (_activeMusic != null) {
                _audioManager.CancelAudio(_activeMusic, false);
            }
            _activeMusic = _audioManager.PlaySound(audioFile, true, true);
        }

        public void EndMusic(bool fadeOut) {
            if (_activeMusic == null) return;
            
            _audioManager.CancelAudio(_activeMusic, fadeOut);
        }
        
        public void SetSoundEffectVolume(float newVolume) {
            _audioManager.SetSoundEffectVolume(newVolume);
        }
        
        public void SetVoiceLineVolume(float newVolume) {
            _audioManager.SetVoiceLineVolume(newVolume);
        }

        public void SetMusicVolume(float newVolume) {
            _audioManager.SetMusicVolume(newVolume);
        }
    }
}