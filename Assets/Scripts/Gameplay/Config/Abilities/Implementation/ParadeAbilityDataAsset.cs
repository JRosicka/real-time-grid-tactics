using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/ParadeAbilityData")]
    public class ParadeAbilityDataAsset : BaseAbilityDataAsset<ParadeAbilityData, ParadeAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for improving a resource collection structure's income rate.
    /// </summary>
    [Serializable]
    public class ParadeAbilityData : AbilityDataBase<ParadeAbilityParameters>, ITargetableAbilityData {
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool CancelableManually => false;
        public override IAbilityParameters OnStartParameters => new ParadeAbilityParameters { Target = null };

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(ParadeAbilityParameters parameters, GridEntity performer) {
            return AbilityLegalAtResourceCollector(performer, parameters.Target) ? AbilityLegality.Legal : AbilityLegality.IndefinitelyIllegal;
        }

        private bool AbilityLegalAtResourceCollector(GridEntity performer, GridEntity resourceCollector) {
            if (resourceCollector == null) return false;
            if (resourceCollector.Team != performer.Team) return false;
            if (!resourceCollector.EntityData.IsResourceExtractor) return false;
            GridEntity resourceProvider = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(resourceCollector, resourceCollector.EntityData);
            if (resourceProvider.CurrentResourcesValue.Amount <= 0) return false;
            return true;
        }

        protected override IAbility CreateAbilityImpl(ParadeAbilityParameters parameters, GridEntity performer) {
            return new ParadeAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity resourceCollector = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(cellPosition);
            return AbilityLegalAtResourceCollector(selectedEntity, resourceCollector);
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity resourceCollector = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(cellPosition);
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new ParadeAbilityParameters {
                Target = resourceCollector
            }, true, true, false);
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

        public string AbilityVerb => "perform a parade";
    }
}