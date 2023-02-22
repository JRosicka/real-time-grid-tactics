using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on a friendly entity under the local player's control
    /// </summary>
    public class OwnerInteractBehavior : IInteractBehavior {
        public void Select(GridEntity entity) {
            GameManager.Instance.SelectionInterface.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            if (thisEntity == null) {
                return;
            }

            // Target the top entity
            GridEntity targetEntity = GameManager.Instance.GetEntitiesAtLocation(targetCell)?.GetTopEntity()?.Entity;

            // See if we should move this entity
            if (targetEntity == null || targetEntity.Data.FriendlyUnitsCanShareCell) {
                TryMoveEntity(thisEntity, targetCell);
            } else {
                TryTargetEntity(thisEntity, targetEntity, targetCell);
            }
        }

        private void TryMoveEntity(GridEntity thisEntity, Vector2Int targetCell) {
            if (thisEntity.CanMove) {
                thisEntity.MoveToCell(targetCell);
            }
        }

        private void TryTargetEntity(GridEntity thisEntity, GridEntity targetEntity, Vector2Int targetCell) {
            if (!thisEntity.CanTargetThings)
                return;

            thisEntity.TryTargetEntity(targetEntity, targetCell);
        }
    }
}