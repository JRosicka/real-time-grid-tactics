using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/AttackAbilityData")]
    public class AttackAbilityDataAsset : BaseAbilityDataAsset<AttackAbilityData, AttackAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for doing an attack move (moving towards a target cell and
    /// attacking anything on the way)
    /// </summary>
    [Serializable]
    public class AttackAbilityData : AbilityDataBase<AttackAbilityParameters>, ITargetableAbilityData {
        public AttackAbilityLogicType AttackType;

        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool Cancelable => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(AttackAbilityParameters parameters, GridEntity entity, GameTeam team) {
            return CanAttackTarget(entity);
        }

        protected override IAbility CreateAbilityImpl(AttackAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new AttackAbility(this, parameters, performer, overrideTeam);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            AbilityLegality legality = CanAttackTarget(selectedEntity);
            return legality == AbilityLegality.Legal;
        }

        private AbilityLegality CanAttackTarget(GridEntity selector) {
            if (selector == null) return AbilityLegality.IndefinitelyIllegal;
            return AbilityLegality.Legal;    // This is just an a-move, so can always do that
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(cellPosition);    // Only able to target the top entity!
            if (target != null && target.Team == selectedEntity.Team) {
                target = null;
            }
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new AttackAbilityParameters {
                    Target = target, 
                    Destination = cellPosition
                }, true, true, true);
            selectedEntity.SetTargetLocation(cellPosition, null, true);
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

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }

        public string AbilityVerb => "attack";
        public bool ShowIconOnGridWhenSelected => true;
    }
}