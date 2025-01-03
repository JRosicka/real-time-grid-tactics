using Gameplay.Config;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Handles playing misc audio during gameplay
    /// </summary>
    public class GameAudioPlayer : MonoBehaviour {
        private AudioPlayer _audioPlayer;
        private GameSetupManager _gameSetupManager;
        private AudioFileConfiguration _audioConfiguration;
        
        public void Initialize(AudioPlayer audioPlayer, GameSetupManager gameSetupManager, AudioFileConfiguration audioConfiguration) {
            _audioPlayer = audioPlayer;
            _gameSetupManager = gameSetupManager;
            _audioConfiguration = audioConfiguration;
            
            if (gameSetupManager.GameInitialized) {
                PlayGameMusic();
            } else {
                gameSetupManager.GameInitializedEvent += PlayGameMusic;
            }
        }

        public void UnregisterListeners() {
            _gameSetupManager.GameInitializedEvent -= PlayGameMusic;
        }

        private void PlayGameMusic() {
            _audioPlayer.PlayMusic(_audioConfiguration.GameMusic);
        }
    }
}