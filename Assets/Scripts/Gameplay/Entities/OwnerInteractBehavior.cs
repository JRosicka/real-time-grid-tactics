using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
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
            
            // If this entity can rally (i.e. it is a production structure), do that
            if (thisEntity.TargetLocationLogic.CanRally) {
                RallyAbilityData data = (RallyAbilityData) thisEntity.Abilities.First(a => a.Content.GetType() == typeof(RallyAbilityData)).Content;
                thisEntity.PerformAbility(data, new RallyAbilityParameters {
                    Destination = targetCell
                }, false, false);
                return;
            }
            
            // Target the top entity
            GridEntity targetEntity = GameManager.Instance.GetEntitiesAtLocation(targetCell)?.GetTopEntity()?.Entity;

            // See if we should target this entity
            if (targetEntity != null && targetEntity.MyTeam == GridEntity.Team.Neutral) {
                thisEntity.TryMoveToCell(targetCell, false);
            } else if (targetEntity != null && thisEntity.MyTeam != targetEntity.MyTeam) {
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