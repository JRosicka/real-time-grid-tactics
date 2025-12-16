using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// A Vector2Int representing a cell. Can be assigned when in play mode by clicking on the associated button and then
    /// clicking on a cell in the game window. 
    /// </summary>
    [Serializable]
    public class AssignableLocation {
        public event Action LocationUpdated;
        
        public Vector2Int Location;

        public AssignableLocation(Vector2Int location) {
            Location = location;
        }

        [Button("Set Location")]
        public void SetLocation() {
            ThrowExceptionIfNotInGame();
            GameManager.Instance.GridInputController.ToggleSetSpawnData(this);
        }

        public void UpdateLocation(Vector2Int location) {
            Location = location;
            LocationUpdated?.Invoke();
        }
        
        private void ThrowExceptionIfNotInGame() {
#if !UNITY_EDITOR
            throw new Exception("Must be in UnityEditor to use this button. Wait how did you even get here?");
#else
            if (!EditorApplication.isPlaying) {
                throw new Exception("Game must be in play mode to set location");
            }
#endif
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null) {
                throw new Exception("Must be in-game to set location");
            }
        }

    }
}