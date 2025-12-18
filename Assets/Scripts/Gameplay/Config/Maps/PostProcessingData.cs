using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// A mapping from PP ID to a PP prefab
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/PostProcessing", fileName = "PP")]
    public class PostProcessingData : ScriptableObject {
        // ReSharper disable once InconsistentNaming
        public string PPID;
        public GameObject Prefab;
    }
}