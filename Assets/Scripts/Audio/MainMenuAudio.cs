using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Audio {
    public class MainMenuAudio : MonoBehaviour {
        [SerializeField] private AudioFile _buttonClick;
        
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

        public void ButtonClickSound() {
            AudioPlayer.TryPlaySFX(_buttonClick);
        }
    }
}