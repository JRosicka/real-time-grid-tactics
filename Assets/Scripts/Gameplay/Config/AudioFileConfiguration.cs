using Audio;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for various <see cref="AudioFile"/>s
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/AudioFileConfiguration", fileName = "AudioFileConfiguration")]
    public class AudioFileConfiguration : ScriptableObject {
        public bool AudioEnabled;
        public AudioFile GameMusic;
    }
}