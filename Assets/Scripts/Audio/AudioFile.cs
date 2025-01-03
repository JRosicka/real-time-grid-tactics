using System;
using UnityEngine;

// ReSharper disable UnassignedField.Global // fields are assigned from AudioList's inspector
namespace Audio {
    /// <summary>
    /// Keeps track of a single audio clip and the values associated with it (volume, reference name, etc)
    /// </summary>
    [Serializable]
    public class AudioFile {
        public AudioClip Clip;
        [Range(0, 1)]
        public float Volume;
        public AudioLayerName AudioLayer;
        public bool Interruptible;
    }
}