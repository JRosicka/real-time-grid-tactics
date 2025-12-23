using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

namespace Scenes {
    /// <summary>
    /// Handles all the scenes and scene transitions
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        [SerializeField] private LoadingScreen _loadingScreen;
        [SerializeField] private GameTypeTracker _gameTypeManager;
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
            _gameTypeManager.SetGameType(false, false, false);
            return _loadingScreen.ShowLoadingScreen(true, true);
        }

        public async void LoadMainMenu() {
            _targetScene = MainMenuSceneName;
            _gameTypeManager.SetGameType(false, false, true);
            await UnloadCurrentScenesAsync(true);
            await LoadScene(MainMenuSceneName, true, true, true, false);
            await LoadScene(GameSceneName, false, true, true, true);
        }
        
        /// <summary>
        /// Load int a SP match
        /// </summary>
        public async void LoadIntoSinglePlayerGame() {
            _targetScene = GameSceneName;
            _gameTypeManager.SetGameType(false, true, true);
            await UnloadCurrentScenesAsync(true);
            await LoadScene(GameSceneName, true, true, true, true);
        }
        
        /// <summary>
        /// Switch the map in the currently loaded game scene
        /// </summary>
        public void SwitchLoadedMap(string newMapID) {
            _gameTypeManager.SetMap(newMapID);
            
            if (_switchMapTask == null || _switchMapTask.IsCompleted) {
                _switchMapTask = DoSwitchLoadedMap();
            }
        }

        private Task _switchMapTask;
        private bool _mapLoadingLocked;
        private async Task DoSwitchLoadedMap() {
            await UnloadScene(GameSceneName, false, true);
            
            if (_mapLoadingLocked) {
                Debug.Log("Not reloading the game scene while switching maps because the game scene is already loaded. Someone else must have loaded the game scene.");
                return;
            }
            
            await LoadScene(GameSceneName, false, true, false, true);
        }
        
        public void LockMapLoading() {
            _mapLoadingLocked = true;
        }

        /// <summary>
        /// Just show the loading screen without doing any actual scene loading/unloading (presumably because Mirror
        /// is handling that)
        /// </summary>
        public void ShowLoadingScreen() {
            _loadingScreen.ShowLoadingScreen(true, true).FireAndForget();
        }
        
        /// <summary>
        /// Just hide the loading screen without doing any actual scene loading/unloading (presumably because Mirror
        /// is handling that)
        /// </summary>
        public void HideLoadingScreen() {
            _loadingScreen.HideLoadingScreen(true);
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

        private async Task UnloadScene(string sceneName, bool showLoadingScreenInFrontOfMenus, bool fade) {
            Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
            if (!sceneToUnload.isLoaded || !sceneToUnload.IsValid() || !Application.isPlaying) {
                return;
            }
            
            await _loadingScreen.ShowLoadingScreen(fade, showLoadingScreenInFrontOfMenus);
            
            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            // Can happen if exiting the application or exiting play mode
            if (op == null) return;
            await op;
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
            if (_targetScene == GameSceneName) {
                GameTypeTracker.Instance.SetGameType(false, true, true);
            }
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
            
            await UnloadCurrentScenesAsync(false);
            switch (strippedSceneName) {
                case MainMenuSceneName:
                    _mapLoadingLocked = false;
                    _gameTypeManager.SetGameType(false, false, true);
                    await LoadScene(GameSceneName, false, true, true, true);
                    break;
                case LobbySceneName:
                    _mapLoadingLocked = false;
                    _gameTypeManager.SetGameType(false, false, false);
                    await LoadScene(GameSceneName, false, true, true, true);
                    break;
                case GameSceneName:
                    _mapLoadingLocked = true;
                    _gameTypeManager.SetGameType(true, true, true);
                    break;
            }
        }

        /// <summary>
        /// Mirror is ordering a scene change, so handle unloading whatever scene we currently have loaded and handle
        /// showing/hiding the loading screen if needed
        /// </summary>
        private async Task UnloadCurrentScenesAsync(bool fadeOutFirst) {
            List<Task> unloadTasks = new() {
                UnloadScene(MainMenuSceneName, true, fadeOutFirst),
                UnloadScene(LobbySceneName, true, fadeOutFirst),
                UnloadScene(GameSceneName, true, fadeOutFirst)
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