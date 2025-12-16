using System;
using Sirenix.OdinInspector;

namespace Gameplay.Config {
    /// <summary>
    /// Represents an entity, and its spawn location
    /// </summary>
    [Serializable]
    public class EntitySpawnData {
        public EntityData Data;
        [InlineProperty]
        public AssignableLocation SpawnLocation;
        
        public EntitySpawnData(EntitySpawnData other) {
            Data = other.Data;
            SpawnLocation = new AssignableLocation(other.SpawnLocation.Location);
        }
    }
}