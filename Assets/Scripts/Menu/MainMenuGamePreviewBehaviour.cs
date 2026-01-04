using Scenes;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// MonoBehaviour for managing the game preview in the main menu. See <see cref="MainMenuGamePreviewManager"/>
    /// </summary>
    public class MainMenuGamePreviewBehaviour : MonoBehaviour {
        private float _timeUntilNextMapSwitch;
        private MainMenuGamePreviewManager _mainMenuGamePreviewManager;
        
        private void Start() {
            _mainMenuGamePreviewManager = SceneLoader.Instance.MainMenuGamePreviewManager;
            SceneLoader.Instance.SceneLoaded += SceneLoaded;
        }
        
        private void OnDestroy() {
            SceneLoader.Instance.SceneLoaded -= SceneLoaded;
        }
        
        private void Update() {
            if (_timeUntilNextMapSwitch <= 0) return;
            
            _timeUntilNextMapSwitch -= Time.deltaTime;
            if (_timeUntilNextMapSwitch <= 0) {
                SwitchToNextMap();
            }
        }
        
        private void SceneLoaded(string sceneName) {
            if (sceneName == SceneLoader.GameSceneName) {
                _timeUntilNextMapSwitch = _mainMenuGamePreviewManager.LastReplay.duration;
            }
        }

        private void SwitchToNextMap() {
            SceneLoader.Instance.MainMenuGamePreviewManager.SwitchToNextMap();
        }
    }
}