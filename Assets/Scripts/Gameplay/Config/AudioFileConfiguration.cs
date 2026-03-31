using System.Collections.Generic;
using Audio;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for various <see cref="AudioFile"/>s
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/AudioFileConfiguration", fileName = "AudioFileConfiguration")]
    public class AudioFileConfiguration : ScriptableObject {
        public AudioFile ButtonClickDownSound;
        public AudioFile ButtonClickInGameSound;
        public AudioFile InvalidSound;
        public AudioFile GameLossSound;
        public AudioFile GameWinSound;
        public AudioFile GameStartSound;
        public AudioFile ArrowLandSound;
        public AudioFile ConstructionSound;
        public AudioFile MenuThumpSound;
        
        [Header("Music")]
        public List<MusicAudioFile> MenuMusic;
        public List<MusicAudioFile> InGameMusic;
    }
}