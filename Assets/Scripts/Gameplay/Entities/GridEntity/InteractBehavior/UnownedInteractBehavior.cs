using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on an entity that is not under the local player's control. Either
    /// this entity is neutral or the local player is a spectator.  
    /// </summary>
    public class UnownedInteractBehavior : IInteractBehavior {
        public bool IsLocalTeam => false;
        
        /// <summary>
        /// Only show paths for spectators
        /// </summary>
        public bool AllowedToSeeTargetLocation => GameManager.Instance.LocalTeam == GameTeam.Spectator;

        public void Select(GridEntity entity) {
            GameManager.Instance.EntitySelectionManager.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            // Do nothing
        }
    }
}