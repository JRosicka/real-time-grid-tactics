using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles determining which <see cref="GameplayTile"/>s can be accessed by particular <see cref="EntityData"/>s.
    /// </summary>
    public class TileAccessibilityManager {
        private List<GameplayTile> _tiles;
        
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
    }
}