using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/BuildAbilityData")]
    public class BuildAbilityDataAsset : BaseAbilityDataAsset<BuildAbilityData, BuildAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to build stuff
    /// </summary>
    [Serializable]
    public class BuildAbilityData : AbilityDataBase<BuildAbilityParameters>, ITargetableAbilityData {
        public bool Targetable;
        public bool UsableEverywhereByBothTeams;
        public List<PurchasableDataWithSelectionKey> Buildables;

        private GridController GridController => GameManager.Instance.GridController;
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;
        private QueuedStructureBuildsManager QueuedStructureBuildsManager => GameManager.Instance.QueuedStructureBuildsManager;

        [Serializable]
        public struct PurchasableDataWithSelectionKey {
            public PurchasableData data;
            public string selectionKey;
        }

        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => true;
        public override bool CancelableManually => true;
        public override bool Targeted => Targetable;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.SelectionInterface.SetUpBuildSelection(this);
        }
        
        protected override AbilityLegality AbilityLegalImpl(BuildAbilityParameters parameters, GridEntity entity, GameTeam team) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(team);
            if (!player.OwnedPurchasablesController.HasRequirementsForPurchase(parameters.Buildable, entity, out string whyNot)) {
                Debug.Log($"Not building ({parameters.Buildable.ID}) because {whyNot}");
                return AbilityLegality.IndefinitelyIllegal;
            }

            if (parameters.Buildable is EntityData { IsStructure: true } buildable) {
                // We need the space to be empty (except for the builder) in order to build a new structure there
                List<GridEntity> performer = new List<GridEntity> { entity };
                bool buildableCanEnterCell = PathfinderService.CanEntityEnterCell(parameters.BuildLocation, buildable, entity.Team, performer);
                bool performerCanEnterCell = PathfinderService.CanEntityEnterCell(parameters.BuildLocation, entity.EntityData, entity.Team, performer);
                if (!buildableCanEnterCell || !performerCanEnterCell) {
                    return AbilityLegality.NotCurrentlyLegal;
                }
            }

            if (entity.Location == null) {
                return AbilityLegality.IndefinitelyIllegal;
            }
            
            if (entity.Location.Value != parameters.BuildLocation) {
                return AbilityLegality.NotCurrentlyLegal;
            }

            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(BuildAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new BuildAbility(this, parameters, performer, overrideTeam);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            EntityData entityToBuild = (EntityData)targetData;
            GameplayTile tileAtLocation = GridController.GridData.GetCell(cellPosition).Tile;
            return entityToBuild.EligibleStructureLocations.Contains(tileAtLocation)
                   && !GameManager.Instance.QueuedStructureBuildsManager.LocationsWithQueuedStructures.Contains(cellPosition)
                   && PathfinderService.CanEntityEnterCell(cellPosition, entityToBuild, selectorTeam, new List<GridEntity>{selectedEntity});
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, System.Object targetData) {
            PurchasableData purchasableData = (PurchasableData)targetData;
            BuildAbilityParameters buildParameters = new BuildAbilityParameters {Buildable = purchasableData, BuildLocation = cellPosition};
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selectedEntity, this, buildParameters, true, true, false);
            selectedEntity.SetTargetLocation(cellPosition, null, false, true);
            
            if (GameManager.Instance.SelectionInterface.BuildMenuOpenFromSelection) {
                // Leave the build menu
                GameManager.Instance.SelectionInterface.DeselectBuildAbility();
            }
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            List<Vector2Int> viableTargets = GetViableTargets(selector, (PurchasableData)targetData);
            GridController.UpdateSelectableCells(viableTargets, selector);
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

        public List<Vector2Int> GetViableTargets(GridEntity selector, PurchasableData buildable) {
            if (buildable is not EntityData buildableEntity) return null;
            
            List<Vector2Int> viableTargets = new List<Vector2Int>();
            
            // Add each cell with a tile that the structure can be built on
            foreach (GameplayTile tile in buildableEntity.EligibleStructureLocations) {
                viableTargets.AddRange(GridController.GridData.GetCells(tile).Select(c => c.Location));
            }

            // Remove any cells that have friendly or opponent entities (except for the selected entity), or local queued structures
            List<Vector2Int> ret = new List<Vector2Int>();
            List<Vector2Int> queuedStructureLocations = QueuedStructureBuildsManager.LocationsWithQueuedStructures;
            foreach (Vector2Int viableTarget in viableTargets) {
                if (queuedStructureLocations.Contains(viableTarget)) {
                    // Queued structure present there, so it is ineligible
                    continue;
                }
                
                var entities = EntitiesOnGrid.EntitiesAtLocation(viableTarget);
                if (entities?.Entities == null) {
                    // No entities there, so it is eligible
                    ret.Add(viableTarget);
                    continue;
                }
                if (entities.Entities.All(e => e.Entity.Team == GameTeam.Neutral || e.Entity == selector)) {
                    // Entities there, but they are all neutral or the selected unit, so eligible 
                    ret.Add(viableTarget);
                }
            }

            return ret;
        }

        public bool MoveToTargetCellFirst => true;
        public GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData) {
            EntityData targetEntityData = (EntityData)targetData;
            InProgressBuildingView buildingView = Object.Instantiate(GameManager.Instance.PrefabAtlas.StructureImagesView);
            buildingView.Initialize(selectorTeam, targetEntityData, true);
            return buildingView.gameObject;
        }

        public string AbilityVerb => "build";
        public bool ShowIconOnGridWhenSelected => false;
    }
}