using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/TargetAttackAbilityData")]
    public class TargetAttackAbilityDataAsset : BaseAbilityDataAsset<TargetAttackAbilityData, TargetAttackAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for attacking a specific <see cref="GridEntity"/> and moving
    /// towards it if out of range
    /// </summary>
    [Serializable]
    public class TargetAttackAbilityData : AbilityDataBase<TargetAttackAbilityParameters>, ITargetableAbilityData {
        public AttackAbilityLogicType AttackType;

        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool CancelableManually => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(TargetAttackAbilityParameters parameters, GridEntity entity) {
            return CanAttackTarget(parameters.Target, entity);
        }

        protected override IAbility CreateAbilityImpl(TargetAttackAbilityParameters parameters, GridEntity performer) {
            return new TargetAttackAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);
            AbilityLegality legality = CanAttackTarget(target, selectedEntity);
            return legality == AbilityLegality.Legal;
        }

        private AbilityLegality CanAttackTarget(GridEntity target, GridEntity selector) {
            if (selector == null) return AbilityLegality.IndefinitelyIllegal;
            if (target == null) return AbilityLegality.IndefinitelyIllegal;    // Need a target to target-fire
            if (target.Team == selector.Team) return AbilityLegality.IndefinitelyIllegal;  // Can only target enemies
            if (target.Team == GameTeam.Neutral) return AbilityLegality.IndefinitelyIllegal;  // Can only target enemies
            return AbilityLegality.Legal;
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);    // Only able to target the top entity!
            if (target != null && (target.Team == selectedEntity.Team || target.Team == GameTeam.Neutral)) {
                Debug.LogWarning("No eligible target!");
                return;
            }
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new TargetAttackAbilityParameters {
                Target = target
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
        
        public string AbilityVerb => "target-attack";
    }
}