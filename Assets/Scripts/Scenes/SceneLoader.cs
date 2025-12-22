using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Network;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

namespace Scenes {
    /// <summary>
    /// Handles all the scenes and scene transitions
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        [SerializeField] private LoadingScreen _loadingScreen;
        private const string LoadingSceneName = "Loading";
        private const string MainMenuSceneName = "MainMenu";
        private const string LobbySceneName = "Room";
        private const string GameSceneName = "GamePlay";
        
        [SerializeField] private float _minimumLoadTimeSeconds = .5f;
        
        public static SceneLoader Instance { get; private set; }
        private string _targetScene;
        
        public void Initialize() {
            Instance = this;

            SubscribeToNetworkedSceneChanges();
            
            // Can happen if entering play mode on a non-loading scene from the editor
            if (!_loadingScreen) {
                LoadDirectlyFromEnteringPlayMode();
                return;
            }
            
            if (SceneManager.loadedSceneCount == 1) {
                // We must be starting the game from the loading scene. Load the main menu.
                LoadMainMenu();
            }
        }
        
        public Task LoadLobby() {
            // The actual scene loading for the lobby is handled by Mirror, so just handle the loading screen
            return _loadingScreen.ShowLoadingScreen(true, true);
        }

        public async void LoadMainMenu() {
            _targetScene = MainMenuSceneName;
            await UnloadCurrentScenesAsync();
            await LoadScene(MainMenuSceneName, true, true, true, false);
            await LoadScene(GameSceneName, false, true, true, true);
        }
        
        /// <summary>
        /// Load a match
        /// </summary>
        public async void LoadIntoGame() {
            _targetScene = GameSceneName;
            await UnloadCurrentScenesAsync();
            await LoadScene(GameSceneName, true, true, true, true);
        }

        /// <summary>
        /// Just show the loading screen without doing any actual scene loading/unloading (presumably because Mirror
        /// is handling that)
        /// </summary>
        public void ShowLoadingScreen() {
            _loadingScreen.ShowLoadingScreen(true, true).FireAndForget();
        }
        
        private async Task LoadScene(string sceneName, bool asActive, bool fade, bool loadingScreenInFrontOfMenus, bool hideLoadingScreenWhenDone) {
            await _loadingScreen.ShowLoadingScreen(fade, loadingScreenInFrontOfMenus);
            
            float startTime = Time.time;
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            float loadTime = Time.time - startTime;
            
            if (asActive) {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            }

            // If the scene loaded too quickly, wait a bit before hiding the loading screen
            if (loadTime < _minimumLoadTimeSeconds) {
                await Task.Delay(TimeSpan.FromSeconds(_minimumLoadTimeSeconds - loadTime));
            }

            if (hideLoadingScreenWhenDone) {
                _loadingScreen.HideLoadingScreen(fade);
            }
        }

        private async Task UnloadScene(string sceneName) {
            Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
            if (!sceneToUnload.isLoaded || !sceneToUnload.IsValid() || !Application.isPlaying) {
                return;
            }
            
            await _loadingScreen.ShowLoadingScreen(false, true);
            
            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            // Can happen if exiting the application or exiting play mode
            if (op == null) return;
            await op;
            
            _loadingScreen.HideLoadingScreen(true);
        }

        /// <summary>
        /// Handles entering play mode for a non-loading scene. 
        /// </summary>
        private async void LoadDirectlyFromEnteringPlayMode() {
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // Load the loading scene
            SceneManager.LoadScene(LoadingSceneName, LoadSceneMode.Single);

            // Need to wait a frame for the loading screen to spawn
            await Task.Yield();
            _loadingScreen = FindFirstObjectByType<LoadingScreen>();
            
            // Load directly into whatever scene we were looking at in the editor
            _targetScene = StrippedSceneName(currentSceneName);
            await LoadScene(currentSceneName, true, false, false, true);
        }

        private void SubscribeToNetworkedSceneChanges() {
            GameNetworkManager.ClientChangeSceneAction += MirrorChangedScene;
            GameNetworkManager.ServerChangeSceneAction += MirrorChangedScene;
        }

        private void OnDestroy() {
            GameNetworkManager.ClientChangeSceneAction -= MirrorChangedScene;
            GameNetworkManager.ServerChangeSceneAction -= MirrorChangedScene;
        }

        /// <summary>
        /// There was a scene change invoked by Mirror, so respond accordingly
        /// </summary>
        /// <param name="newSceneName"></param>
        private async void MirrorChangedScene(string newSceneName) {
            string strippedSceneName = StrippedSceneName(newSceneName);
            if (_targetScene == strippedSceneName) return;
            _targetScene = strippedSceneName;
            
            await UnloadCurrentScenesAsync();
            switch (strippedSceneName) {
                case MainMenuSceneName:
                case LobbySceneName:
                    await LoadScene(GameSceneName, false, true, true, true);
                    break;
            }
        }

        /// <summary>
        /// Mirror is ordering a scene change, so handle unloading whatever scene we currently have loaded and handle
        /// showing/hiding the loading screen if needed
        /// </summary>
        private async Task UnloadCurrentScenesAsync() {
            List<Task> unloadTasks = new() {
                UnloadScene(MainMenuSceneName),
                UnloadScene(LobbySceneName),
                UnloadScene(GameSceneName)
            };

            await Task.WhenAll(unloadTasks);
        }

        private static string StrippedSceneName(string sceneName) {
            if (sceneName == null || !sceneName.Contains("/") || !sceneName.Contains(".")) {
                return sceneName;
            }
            
            sceneName = sceneName.Substring(sceneName.LastIndexOf('/') + 1);
            sceneName = sceneName.Substring(0, sceneName.LastIndexOf('.'));
            return sceneName;
        }
    }
}