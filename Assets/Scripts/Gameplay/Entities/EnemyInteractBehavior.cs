using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on an enemy entity
    /// </summary>
    public class EnemyInteractBehavior : IInteractBehavior {
        public bool IsLocalTeam => false;

        public void Select(GridEntity entity) {
            GameManager.Instance.EntitySelectionManager.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            // Do nothing - can't use enemy units to do stuff
        }
    }
}