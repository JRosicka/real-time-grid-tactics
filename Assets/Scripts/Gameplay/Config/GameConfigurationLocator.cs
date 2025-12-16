using System;
using JetBrains.Annotations;
using UnityEditor;
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
#if UNITY_EDITOR
                    // If in the editor, then load the file directly since GameConfigurationLocator only operates in play mode
                    string[] guids = AssetDatabase.FindAssets("t:GameConfiguration");
                    switch (guids.Length) {
                        case 0:
                            throw new Exception("No game configuration found");
                        case > 1:
                            throw new Exception("More than one game configuration found");
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _gameConfiguration = AssetDatabase.LoadAssetAtPath<GameConfiguration>(assetPath);

                    if (_gameConfiguration == null) {
                        throw new Exception($"Failed to find cheat configuration: {assetPath}");
                    }
#else
                    throw new Exception("Game configuration not yet loaded!");
#endif
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