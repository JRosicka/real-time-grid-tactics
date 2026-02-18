using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles determining which <see cref="GameplayTile"/>s can be accessed by particular <see cref="EntityData"/>s.
    /// </summary>
    public class TileAccessibilityManager {
        private List<GameplayTile> _tiles;
        
        private readonly Dictionary<string, float> _fastestMoveTimesCache = new Dictionary<string, float>();
        
        public void Initialize(GameConfiguration gameConfiguration) {
            _tiles = gameConfiguration.Tiles;
        }

        public List<GameplayTile> InaccessibleTiles(EntityData entityData) {
            return _tiles.Where(t => t.InaccessibleTags.Any(tag => entityData.Tags.Contains(tag))
                                                || t.InaccessibleEntities.Contains(entityData)).ToList();
        }

        public List<GameplayTile> SlowTiles(EntityData entityData) {
            return _tiles.Where(tile => tile.IsSlowed(entityData.Tags)).ToList();
        }
        
        /// <summary>
        /// The fastest move time for the given entity to whatever tile it moves in the fastest
        /// </summary>
        public float GetFastestMoveTime(EntityData entityData) {
            if (_fastestMoveTimesCache.TryGetValue(entityData.ID, out float cachedValue)) {
                return cachedValue;
            }
            
            float normalMoveTime = entityData.NormalMoveTime;
            float fastestMoveTimeModifier = _tiles.Min(tile => tile.GetMoveModifier(entityData.Tags)) - 1;
            float fastestMoveTime = normalMoveTime + normalMoveTime * fastestMoveTimeModifier;
            _fastestMoveTimesCache[entityData.ID] = fastestMoveTime;

            return fastestMoveTime;
        }
    }
}