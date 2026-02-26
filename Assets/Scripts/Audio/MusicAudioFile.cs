using UnityEngine;

namespace Audio {
    /// <summary>
    /// Keeps track of a single music audio clip and the values associated with it (volume, reference name, etc)
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/MusicAudioFile", fileName = "MusicAudioFile", order = 0)]
    public class MusicAudioFile : AudioFile {
        public string DisplayName;
    }
}