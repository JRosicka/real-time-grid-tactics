using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Pathfinding;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/MoveAbilityData")]
    public class MoveAbilityDataAsset : BaseAbilityDataAsset<MoveAbilityData, MoveAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to move an entity
    /// </summary>
    [Serializable]
    public class MoveAbilityData : AbilityDataBase<MoveAbilityParameters>, ITargetableAbilityData {
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool Cancelable => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity, GameTeam team) {
            if (!CanTargetCell(parameters.Destination, entity, team, null)) {
                return AbilityLegality.IndefinitelyIllegal;
            }

            if (parameters.BlockedByOccupation && !PathfinderService.CanEntityEnterCell(parameters.Destination, 
                        entity.EntityData, team, new List<GridEntity> { entity })) {
                // Only consider this legal if we can take a step towards the destination
                PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(entity, parameters.Destination);
                List<GridNode> pathNodes = path.Nodes;
                if (pathNodes.Count < 2) {
                    return AbilityLegality.NotCurrentlyLegal;
                }
            }

            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new MoveAbility(this, parameters, performer, overrideTeam);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            if (selectedEntity == null) return false;
            if (selectedEntity.Team != selectorTeam) return false;
            return true;
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            // No actual moving to do here - the caller is responsible for moving the entity to the destination first
            // anyway, which is the only thing we're trying to do with this ability. 
            selectedEntity.SetTargetLocation(cellPosition, null, false);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            // Nothing to do
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            GameManager.Instance.GridIconDisplayer.DisplayOverHoveredCell(this, cell);
        }

        public void OwnedPurchasablesChanged(GridEntity selector) {
            // Nothing to do
        }

        public void Deselect() {
            // Nothing to do
        }

        public bool MoveToTargetCellFirst => true;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }
        
        public string AbilityVerb => "move";
        public bool ShowIconOnGridWhenSelected => true;
    }
}