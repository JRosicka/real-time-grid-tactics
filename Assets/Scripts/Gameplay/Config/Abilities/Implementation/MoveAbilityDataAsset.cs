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
        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity) {
            if (!CanTargetCell(parameters.Destination, entity, parameters.SelectorTeam, null)) {
                return false;
            }

            if (parameters.BlockedByOccupation && !PathfinderService.CanEntityEnterCell(parameters.Destination, 
                        entity.EntityData, parameters.SelectorTeam, new List<GridEntity> { entity })) {
                // Only consider this legal if we can take a step towards the destination
                PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(entity, parameters.Destination);
                List<GridNode> pathNodes = path.Nodes;
                return pathNodes.Count >= 2;
            }

            return true;
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
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
            // Nothing to do
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
    }
}