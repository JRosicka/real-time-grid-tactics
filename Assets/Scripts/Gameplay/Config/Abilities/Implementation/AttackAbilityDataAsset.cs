using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/AttackAbilityData")]
    public class AttackAbilityDataAsset : BaseAbilityDataAsset<AttackAbilityData, AttackAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for attacking a specific <see cref="GridEntity"/> and moving
    /// towards it if out of range, or for doing a general attack-move towards a target cell
    /// </summary>
    [Serializable]
    public class AttackAbilityData : AbilityDataBase<AttackAbilityParameters>, ITargetableAbilityData {
        public AttackAbilityLogicType AttackType;

        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity) {
            return CanAttackTarget(parameters.Target, entity);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer) {
            return new AttackAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);
            return CanAttackTarget(target, selectedEntity);
        }

        private bool CanAttackTarget(GridEntity target, GridEntity selector) {
            if (selector == null) return false;
            if (target == null) return true;    // This is just an a-move, so can always do that
            if (target.Team == GameTeam.Neutral) return true;  // Can attack (or at least a-move to) neutral entities
            if (target == selector) return true;  // We can issue an attack move to the selected entity's cell
            if (target.InteractBehavior is { IsLocalTeam: true } && target.EntityData.FriendlyUnitsCanShareCell) return true;   // We can a-move onto a friendly entity that friendly units can enter
            
            return target.Team != selector.Team;    // Can not attack friendly entities
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);    // Only able to target the top entity!
            if (target != null && target.Team == selectedEntity.Team) {
                target = null;
            }
            GameManager.Instance.AbilityAssignmentManager.QueueAbility(selectedEntity, this, new AttackAbilityParameters {
                    TargetFire = target != null && target.Team != GameTeam.Neutral, 
                    Target = target, 
                    Destination = cellPosition
                }, true, true, false, true);
            selectedEntity.SetTargetLocation(cellPosition, target, true);
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

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }
    }
}