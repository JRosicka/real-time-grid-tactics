using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Provides a means to get the <see cref="GameConfiguration"/> at runtime from anywhere.
    /// Every scene needs an instance of this. Well, I suppose just the first loaded scene does for a build, but for
    /// convenience purposes we should include an instance in every scene so that we can directly enter any scene in
    /// the Unity editor. 
    /// </summary>
    public class GameConfigurationLocator : MonoBehaviour {
        private static GameConfiguration _gameConfiguration;
        
        [SerializeField]
        private GameConfiguration _gameConfigurationRef;

        [NotNull]
        public static GameConfiguration GameConfiguration {
            get {
                if (_gameConfiguration == null) {
                    throw new Exception("Game configuration not yet loaded!");
                }
                return _gameConfiguration;
            }
        }

        private void Awake() {
            if (!_gameConfiguration) {
                _gameConfiguration = _gameConfigurationRef;
            }
        }
    }
}