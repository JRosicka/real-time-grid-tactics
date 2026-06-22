using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IInteractBehavior"/> for clicking on a friendly entity under the local player's control
    /// </summary>
    public class OwnerInteractBehavior : IInteractBehavior {
        private readonly GridEntity _entity;
        
        public bool IsLocalTeam => true;
        public bool AllowedToSeeMiscInfo => true;
        public bool AllowedToSeeQueuedBuilds(GameTeam team) {
            if (_entity.EntityData.ControllableByAllPlayers) {
                // Only show for local team abilities 
                return team == GameManager.Instance.LocalTeam;
            }

            return true;
        }

        public SelectionReticle.ReticleSelection ReticleSelection => SelectionReticle.ReticleSelection.Ally;

        public OwnerInteractBehavior(GridEntity entity) {
            _entity = entity;
        }
        
        public void Select(GridEntity entity) {
            GameManager.Instance.EntitySelectionManager.SelectEntity(entity);
        }

        public void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell) {
            if (thisEntity == null) {
                return;
            }
            
            // If this entity can rally (i.e. it is a production structure), do that
            if (thisEntity.TargetLocationLogicValue.CanRally) {
                RallyAbilityData data = thisEntity.GetAbilityData<RallyAbilityData>();
                GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(thisEntity, data, new RallyAbilityParameters {
                    Destination = targetCell
                }, true, false, false, true);
                return;
            }
            
            // Don't move/target the cell if this is a worker in the middle of building or collecting
            if (thisEntity.Tags.Contains(EntityTag.Worker)) {
                if (thisEntity.ActiveTimers.Any(t => t.Ability is BuildAbility)) {
                    GameManager.Instance.EntitySelectionManager.DeselectTargetableAbility();
                    GameManager.Instance.AlertTextDisplayer.DisplayAlert("You must cancel the structure first.");
                    return;
                }
                if (thisEntity.ActiveTimers.Any(t => t.Ability is CollectResourceAbility)) {
                    GameManager.Instance.EntitySelectionManager.DeselectTargetableAbility();
                    GameManager.Instance.AlertTextDisplayer.DisplayAlert("You must cancel the the resource collection first.");
                    return;
                }
            }
            
            // Target the top entity
            List<GridEntity> entitiesAtLocation = GameManager.Instance.CommandManager.EntitiesOnGrid.EntitiesAtLocation(targetCell)
                ?.Entities?.OrderByDescending(o => o.Order).Select(e => e.Entity).ToList();
            if (entitiesAtLocation == null || entitiesAtLocation.Count == 0) {
                thisEntity.TryMoveToCell(targetCell, true, true, true);
            } else {
                bool locationContainsThisEntity = entitiesAtLocation.Contains(thisEntity);
                GridEntity targetEntity = entitiesAtLocation.FirstOrDefault(e => e != thisEntity);
                
                // See if we should target this entity
                if (targetEntity != null && targetEntity.Team == GameTeam.Neutral && !targetEntity.EntityData.Targetable) {
                    thisEntity.TryMoveToCell(targetCell, true, true, true);
                } else if (targetEntity != null && thisEntity.Team != targetEntity.Team) {
                    if (!TryTargetEntity(thisEntity, targetEntity, targetCell)) {
                        thisEntity.TryMoveToCell(targetCell, true, true, true);
                    }
                } else if (locationContainsThisEntity) {
                    // We are right-clicking the selected entity's cell? Cancel all move and attack abilities. (and collection) 
                    List<IAbility> abilitiesToCancel = thisEntity.GetAbilities(new List<Type> {typeof(MoveAbility), typeof(AttackAbility), typeof(TargetAttackAbility), typeof(CollectResourceAbility)});
                    if (abilitiesToCancel.Any()) {
                        abilitiesToCancel.ForEach(a => GameManager.Instance.CommandManager.CancelAbility(a, true));
                    
                        // Update the rally point
                        Vector2Int? currentLocation = thisEntity.Location;
                        // The location might be null if the entity is being destroyed 
                        if (currentLocation != null) {
                            thisEntity.SetTargetLocation(currentLocation.Value, null, false);
                        }
                    }
                } else {
                    thisEntity.TryMoveToCell(targetCell, true, true, true);
                }
            }
            
            GameManager.Instance.EntitySelectionManager.DeselectTargetableAbility();
        }
        
        private bool TryTargetEntity(GridEntity thisEntity, GridEntity targetEntity, Vector2Int targetCell) {
            if (!thisEntity.CanTargetThings)
                return false;

            return thisEntity.TryTargetEntity(targetEntity, targetCell);
        }
    }
}