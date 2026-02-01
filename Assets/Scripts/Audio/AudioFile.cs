using UnityEngine;

namespace Audio {
    /// <summary>
    /// Keeps track of a single audio clip and the values associated with it (volume, reference name, etc)
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/AudioFile", fileName = "AudioFile", order = 0)]
    public class AudioFile : ScriptableObject {
        public AudioClip Clip;
        [Range(0, 1)]
        public float Volume;
        public AudioLayerName AudioLayer;
        public bool Interruptible;
        public bool PlayDuringReplay;
        public bool AllowInQuickSuccession;
    }
}