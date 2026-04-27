using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/CollectResourceAbilityData")]
    public class CollectResourceAbilityDataAsset : BaseAbilityDataAsset<CollectResourceAbilityData, CollectResourceAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for a resource pickup being collected
    /// </summary>
    [Serializable]
    public class CollectResourceAbilityData : AbilityDataBase<CollectResourceAbilityParameters>, ITargetableAbilityData {
        public List<EntityData> CollectableResourceEntities;
        
        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => true;
        public override bool Cancelable => true;
        public override bool Targeted => true;
        
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(CollectResourceAbilityParameters parameters, GridEntity entity, GameTeam team) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(CollectResourceAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new CollectResourceAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new NullAbilityParameters();
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            return GetCollectibleResourceAtLocation(cellPosition) != null;
        }
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity resourceToCollect = GetCollectibleResourceAtLocation(cellPosition);
            if (!resourceToCollect) return;
            
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, new CollectResourceAbilityParameters {
                Target = resourceToCollect
            }, true, true, true, true);
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
        public string AbilityVerb => "collect";
        public bool ShowIconOnGridWhenSelected => true;

        private GridEntity GetCollectibleResourceAtLocation(Vector2Int cellPosition) {
            return EntitiesOnGrid.EntitiesAtLocation(cellPosition)?.Entities.Select(o => o.Entity)
                .FirstOrDefault(e => CollectableResourceEntities.Contains(e.EntityData));
        }
    }
}