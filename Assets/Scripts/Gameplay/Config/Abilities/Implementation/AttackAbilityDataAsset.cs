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

        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool CancelableManually => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override AbilityLegality AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity) {
            return CanAttackTarget(parameters.Target, entity);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer) {
            return new AttackAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);
            AbilityLegality legality = CanAttackTarget(target, selectedEntity);
            return legality == AbilityLegality.Legal;
        }

        private AbilityLegality CanAttackTarget(GridEntity target, GridEntity selector) {
            if (selector == null) return AbilityLegality.IndefinitelyIllegal;
            if (target == null) return AbilityLegality.Legal;    // This is just an a-move, so can always do that
            if (target.Team == GameTeam.Neutral) return AbilityLegality.Legal;  // Can attack (or at least a-move to) neutral entities
            if (target == selector) return AbilityLegality.Legal;  // We can issue an attack move to the selected entity's cell
            if (target.InteractBehavior is { IsLocalTeam: true } && target.EntityData.FriendlyUnitsCanShareCell) return AbilityLegality.Legal;   // We can a-move onto a friendly entity that friendly units can enter
            if (target.Team == selector.Team) return AbilityLegality.IndefinitelyIllegal;     // Can not attack friendly entities
            return AbilityLegality.Legal;
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);    // Only able to target the top entity!
            if (target != null && target.Team == selectedEntity.Team) {
                target = null;
            }
            GameManager.Instance.AbilityAssignmentManager.PerformAbility(selectedEntity, this, new AttackAbilityParameters {
                    TargetFire = target != null && target.Team != GameTeam.Neutral, 
                    Target = target, 
                    Destination = cellPosition
                }, true, true, true);
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