using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/BoostStructureAbilityData")]
    public class BoostStructureAbilityDataAsset : BaseAbilityDataAsset<BoostStructureAbilityData, BoostStructureAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for boosting the production speed and income of an adjacent structure
    /// </summary>
    [Serializable]
    public class BoostStructureAbilityData : AbilityDataBase<BoostStructureAbilityParameters>, ITargetableAbilityData {
        public float BoostAmount;
        
        private GridController GridController => GameManager.Instance.GridController;
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;

        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => true;
        public override bool ManuallyCancelable => false;
        
        public override IAbilityParameters OnStartParameters => new BoostStructureAbilityParameters { Target = null };

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(this, selector.Team, null);
        }
        
        protected override AbilityLegality AbilityLegalImpl(BoostStructureAbilityParameters parameters, GridEntity entity, GameTeam team, out string failureReason) {
            failureReason = null;
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(BoostStructureAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new BoostStructureAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new BoostStructureAbilityParameters {
                Target = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID((long)json["Target"])
            };
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            return GetViableTargets(selectedEntity).Contains(cellPosition);
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData) {
            GridEntity entity = GetBoostableEntity(cellPosition, selectorTeam);
            if (entity == null) {
                Debug.LogWarning($"No viable entity found at boost location {cellPosition}, that's unexpected.");
                return;
            }
            
            BoostStructureAbilityParameters boostParameters = new BoostStructureAbilityParameters { Target = entity };
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, boostParameters, 
                true, true, true, true);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            // Nothing to do
        }

        public void UpdateHoveredCell(GridEntity selector, Vector2Int? cell) {
            GameManager.Instance.GridIconDisplayer.DisplayOverHoveredCell(this, cell);
        }

        public void OwnedPurchasablesChanged(GridEntity selector) {
            List<Vector2Int> viableTargets = GetViableTargets(selector);
            GridController.UpdateSelectableCells(viableTargets, true, selector);
        }

        public void Deselect() {
            // Nothing to do
        }

        public List<Vector2Int> GetViableTargets(GridEntity selector) {
            if (selector == null || selector.DeadOrDying || selector.Location == null) return new List<Vector2Int>();
            
            // Add each cell adjacent to the selector
            List<Vector2Int> viableTargets = GridController.GridData.GetAdjacentCells(selector.Location.Value).Select(c => c.Location).ToList();
            
            // Remove any that don't contain friendly boostable structures
            for (int i = viableTargets.Count - 1; i >= 0; i--) {
                if (!GetBoostableEntity(viableTargets[i], selector.Team)) {
                    viableTargets.RemoveAt(i);
                }
            }

            return viableTargets;
        }

        [CanBeNull]
        public GridEntity GetBoostableEntity(Vector2Int location, GameTeam team) {
            List<GridEntity> entitiesAtLocation = EntitiesOnGrid.EntitiesAtLocation(location)?.Entities?.Select(e => e.Entity).ToList() ?? new List<GridEntity>();
            return entitiesAtLocation.FirstOrDefault(e => e.EntityData.IsStructure && e.EntityData.Boostable && e.Team == team && e.Location != null && !e.DeadOrDying);
        }

        public bool MoveToTargetCellFirst => false;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            return null;
        }

        public string AbilityVerb => "boost";
        public bool ShowIconOnGridWhenSelected => true;
    }
}