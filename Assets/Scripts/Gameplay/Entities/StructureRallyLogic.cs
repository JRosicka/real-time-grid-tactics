using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IRallyLogic"/> for a structure
    /// </summary>
    public class StructureRallyLogic : IRallyLogic {
        public bool CanRally => true;
        public Vector2Int RallyPoint { get; set; }

        public StructureRallyLogic(Vector2Int spawnLocation) {
            RallyPoint = spawnLocation;
        }
    }
}