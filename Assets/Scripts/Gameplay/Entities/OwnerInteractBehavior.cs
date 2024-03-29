using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on a friendly entity under the local player's control
    /// </summary>
    public class OwnerInteractBehavior : IInteractBehavior {
        public void Select(GridEntity entity) {
            GameManager.Instance.EntitySelectionManager.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            if (thisEntity == null) {
                return;
            }
            
            GameManager.Instance.SelectionInterface.DeselectActiveAbility();
            
            // Target the top entity
            GridEntity targetEntity = GameManager.Instance.GetEntitiesAtLocation(targetCell)?.GetTopEntity()?.Entity;

            // See if we should target this entity
            if (targetEntity != null && thisEntity.MyTeam != targetEntity.MyTeam) {
                TryTargetEntity(thisEntity, targetEntity, targetCell);
            } else if (targetEntity == null || targetEntity.EntityData.FriendlyUnitsCanShareCell) {
                thisEntity.TryMoveToCell(targetCell);
            } else {
                Debug.Log("Can not attack entity or move to cell, doing nothing");
            }
        }
        
        private void TryTargetEntity(GridEntity thisEntity, GridEntity targetEntity, Vector2Int targetCell) {
            if (!thisEntity.CanTargetThings)
                return;

            thisEntity.TryTargetEntity(targetEntity, targetCell);
        }
    }
}