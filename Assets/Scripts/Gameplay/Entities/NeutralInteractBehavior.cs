using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on a neutral entity under no player's control
    /// </summary>
    public class NeutralInteractBehavior : IInteractBehavior {
        public void Select(GridEntity entity) {
            // Do nothing
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            // Do nothing
        }
    }
}