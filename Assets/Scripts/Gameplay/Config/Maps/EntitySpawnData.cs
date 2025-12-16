using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Represents an entity, its team, and its spawn location
    /// </summary>
    [Serializable]
    public class EntitySpawnData {
        public EntityData Data;
        public Vector2Int SpawnLocation;

        public event Action SpawnLocationUpdated;

        public EntitySpawnData(EntitySpawnData other) {
            Data = other.Data;
            SpawnLocation = other.SpawnLocation;
        }
        
        [Button("Set Spawn")]
        public void SetSpawn() {
            ThrowExceptionIfNotInGame();
            GameManager.Instance.GridInputController.ToggleSetSpawnData(this);
        }

        public void UpdateSpawnLocation(Vector2Int location) {
            SpawnLocation = location;
            SpawnLocationUpdated?.Invoke();
        }
        
        private void ThrowExceptionIfNotInGame() {
#if !UNITY_EDITOR
            throw new Exception("Must be in UnityEditor to use this button. Wait how did you even get here?");
#else
            if (!EditorApplication.isPlaying) {
                throw new Exception("Game must be in play mode to set spawn");
            }
#endif
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null) {
                throw new Exception("Must be in-game to set spawn");
            }
        }
    }
    
}