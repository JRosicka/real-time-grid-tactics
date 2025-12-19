using UnityEngine;

namespace Audio {
    public class LobbyAudio : MonoBehaviour {
        [SerializeField] private AudioFile _buttonClick;
        
        private AudioPlayer _audioPlayer;
        private AudioPlayer AudioPlayer {
            get {
                if (_audioPlayer == null) {
                    _audioPlayer = FindFirstObjectByType<AudioPlayer>();
                }
                return _audioPlayer;
            }
        }
        
        public void ButtonClickSound() {
            AudioPlayer.TryPlaySFX(_buttonClick);
        }
    }
}