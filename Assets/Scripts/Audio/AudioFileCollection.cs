using System.Collections.Generic;
using UnityEngine;

namespace Audio {
    /// <summary>
    /// Keeps track of a group of audio clips
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/AudioFileCollection", fileName = "AudioFileCollection", order = 0)]
    public class AudioFileCollection : ScriptableObject {
        public List<AudioFile> AudioFiles;
    }
}