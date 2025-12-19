using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes {
    /// <summary>
    /// Handles all the scenes and scene transitions
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        [SerializeField] private LoadingScreen _loadingScreen;
        
        private static SceneLoader _instance;
        
        /// <summary>
        /// Performs initialization. If initialization was already performed this app session, then destroys the GameObject. 
        /// </summary>
        /// <returns>True if this is the active player, otherwise false if this is getting destroyed</returns>
        private void Awake() {
            if (_instance != null) {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            _instance = this;
            
            // Can happen if entering play mode on a non-loading scene from the editor
            if (!_loadingScreen) {
                SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
                _loadingScreen = FindFirstObjectByType<LoadingScreen>();
            }
        }
        
        
    }
}