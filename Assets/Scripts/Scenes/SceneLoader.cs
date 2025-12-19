using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes {
    /// <summary>
    /// Handles all the scenes and scene transitions
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        [SerializeField] private LoadingScreen _loadingScreen;
        
        public void Initialize() {
            // Can happen if entering play mode on a non-loading scene from the editor
            if (!_loadingScreen) {
                SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
                _loadingScreen = FindFirstObjectByType<LoadingScreen>();
            }
        }
    }
}