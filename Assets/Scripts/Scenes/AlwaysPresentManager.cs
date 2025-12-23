using Audio;
using Gameplay.Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scenes {
    /// <summary>
    /// Controls the group of singleton objects that should be present in every scene
    /// </summary>
    public class AlwaysPresentManager : MonoBehaviour {
        private static AlwaysPresentManager _instance;
        
        [SerializeField] private AudioPlayer _audioPlayer;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private GameConfigurationLocator _gameConfigurationLocator;
        [FormerlySerializedAs("_gameNetworkStateManager")] [SerializeField] private GameTypeTracker _gameTypeManager;

        /// <summary>
        /// Performs initialization. If initialization was already performed this app session, then destroys the GameObject. 
        /// </summary>
        private void Awake() {
            if (_instance != null) {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            _instance = this;
            
            _gameTypeManager.Initialize();
            _gameConfigurationLocator.Initialize();
            _audioPlayer.Initialize();
            _sceneLoader.Initialize();
        }
    }
}