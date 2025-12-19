using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes {
    /// <summary>
    /// Handles all the scenes and scene transitions
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        [SerializeField] private LoadingScreen _loadingScreen;
        [SerializeField] private string _loadingSceneName = "Loading";
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private string _lobbySceneName = "Room";
        [SerializeField] private string _gameSceneName = "GamePlay";
        
        [SerializeField] private float _minimumLoadTimeSeconds = .5f;
        
        public void Initialize() {
            // Can happen if entering play mode on a non-loading scene from the editor
            if (!_loadingScreen) {
                LoadDirectlyFromEnteringPlayMode();
                return;
            }
            
            if (SceneManager.loadedSceneCount == 1) {
                // We must be starting the game from the loading scene. Load the main menu.
                LoadScene(_mainMenuSceneName, true, true);
            }
        }
        
        private async void LoadScene(string sceneName, bool fade, bool inFrontOfMenus) {
            _loadingScreen.ShowLoadingScreen(fade, inFrontOfMenus);
            
            float startTime = Time.time;
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            float loadTime = Time.time - startTime;
            
            // Always have the gameplay scene be the active one since that's where we want GameObjects to be instantiated by default
            if (sceneName == _gameSceneName) {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(_gameSceneName));
            }

            // If the scene loaded too quickly, wait a bit before hiding the loading screen
            if (loadTime < _minimumLoadTimeSeconds) {
                await Task.Delay(TimeSpan.FromSeconds(_minimumLoadTimeSeconds - loadTime));
            }
            
            _loadingScreen.HideLoadingScreen(fade);
        }

        /// <summary>
        /// Handles entering play mode for a non-loading scene. 
        /// </summary>
        private async void LoadDirectlyFromEnteringPlayMode() {
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // Load the loading scene
            SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Single);

            // Need to wait a frame for the loading screen to spawn
            await Task.Yield();
            _loadingScreen = FindFirstObjectByType<LoadingScreen>();
            
            // Load directly into whatever scene we were looking at in the editor
            LoadScene(currentSceneName, false, false);
        }
    }
}