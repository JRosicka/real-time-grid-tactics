using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on a friendly entity under the local player's control
    /// </summary>
    public class OwnerInteractBehavior : IInteractBehavior {
        public bool IsLocalTeam => true;
        public bool AllowedToSeeTargetLocation => true;
        public bool AllowedToSeeQueuedStructures => true;
        public SelectionReticle.ReticleSelection ReticleSelection => SelectionReticle.ReticleSelection.Ally;

        public void Select(GridEntity entity) {
            GameManager.Instance.EntitySelectionManager.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            if (thisEntity == null) {
                return;
            }
            
            GameManager.Instance.SelectionInterface.DeselectActiveAbility();
            
            // If this entity can rally (i.e. it is a production structure), do that
            if (thisEntity.TargetLocationLogicValue.CanRally) {
                RallyAbilityData data = thisEntity.GetAbilityData<RallyAbilityData>();
                GameManager.Instance.AbilityAssignmentManager.PerformAbility(thisEntity, data, new RallyAbilityParameters {
                    Destination = targetCell
                }, false, false);
                return;
            }
            
            // Target the top entity
            GridEntity targetEntity = GameManager.Instance.GetEntitiesAtLocation(targetCell)?.GetTopEntity()?.Entity;

            // See if we should target this entity
            if (targetEntity != null && targetEntity.Team == GameTeam.Neutral) {
                thisEntity.TryMoveToCell(targetCell, false);
            } else if (targetEntity != null && thisEntity.Team != targetEntity.Team) {
                TryTargetEntity(thisEntity, targetEntity, targetCell);
            } else if (targetEntity == null || targetEntity.EntityData.FriendlyUnitsCanShareCell) {
                thisEntity.TryMoveToCell(targetCell, false);
            }
        }
        
        private void TryTargetEntity(GridEntity thisEntity, GridEntity targetEntity, Vector2Int targetCell) {
            if (!thisEntity.CanTargetThings)
                return;

            thisEntity.TryTargetEntity(targetEntity, targetCell);
        }
    }
}