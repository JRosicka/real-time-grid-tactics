using System;
using Gameplay.Entities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Represents an entity, its team, and its spawn location
    /// </summary>
    [Serializable]
    public class EntitySpawnData {
        public EntityData Data;
        public GameTeam Team;
        public Vector2Int SpawnLocation;

        public EntitySpawnData(EntitySpawnData other) {
            Data = other.Data;
            Team = other.Team;
            SpawnLocation = other.SpawnLocation;
        }
        
        [Button("Set Spawn")]
        public void SetSpawn() {
            // TODO
        }
    }
}