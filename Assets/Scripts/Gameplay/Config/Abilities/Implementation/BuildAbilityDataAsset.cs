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
        public List<PurchasableDataWithSelectionKey> Buildables;

        private GridController GridController => GameManager.Instance.GridController;
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;
        private QueuedStructureBuildsManager QueuedStructureBuildsManager => GameManager.Instance.QueuedStructureBuildsManager;

        [Serializable]
        public struct PurchasableDataWithSelectionKey {
            public PurchasableData data;
            public string selectionKey;
        }

        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => true;
        public override bool CancelableWhileQueued => true;
        public override bool Targeted => Targetable;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.SelectionInterface.SelectBuildAbility(this);
        }

        // TODO: You know, it seems like the only ability that has this cost consideration is the build ability. So maybe we just get rid of the PayCostUpFront field and consider that the default logic for... the one ability that has a cost. 
        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            BuildAbilityParameters buildParameters = (BuildAbilityParameters) parameters;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.Team);
            if (!player.ResourcesController.CanAfford(buildParameters.Buildable.Cost)) {
                Debug.Log($"Not building ({buildParameters.Buildable.ID}) because we can't pay the cost");
                return false;
            }

            return true;
        }

        protected override bool AbilityLegalImpl(BuildAbilityParameters parameters, GridEntity entity) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.Team);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;
            if (parameters.Buildable.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                Debug.Log($"Not building ({parameters.Buildable.ID}) because we don't have the proper requirements");
                return false;
            }

            if (parameters.Buildable is EntityData { IsStructure: true } buildable) {
                // We need the space to be empty (except for the builder) in order to build a new structure there
                List<GridEntity> performer = new List<GridEntity> { entity };
                bool buildableCanEnterCell = PathfinderService.CanEntityEnterCell(parameters.BuildLocation, buildable, entity.Team, performer);
                bool performerCanEnterCell = PathfinderService.CanEntityEnterCell(parameters.BuildLocation, entity.EntityData, entity.Team, performer);
                if (!buildableCanEnterCell || !performerCanEnterCell) {
                    return false;
                }
            }

            if (entity.Location == null || entity.Location.Value != parameters.BuildLocation) {
                return false;
            }

            return true;
        }

        protected override IAbility CreateAbilityImpl(BuildAbilityParameters parameters, GridEntity performer) {
            return new BuildAbility(this, parameters, performer);
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
            GameManager.Instance.AbilityAssignmentManager.QueueAbility(selectedEntity, this, buildParameters, true, false, false);
            selectedEntity.SetTargetLocation(cellPosition, null);

            if (purchasableData is EntityData { IsStructure: true }) {
                // Track and display the queued structure build
                GameManager.Instance.QueuedStructureBuildsManager.RegisterQueuedStructure(buildParameters, selectedEntity);
            }
            
            if (GameManager.Instance.SelectionInterface.BuildMenuOpenFromSelection) {
                // Leave the build menu
                GameManager.Instance.SelectionInterface.DeselectBuildAbility();
            }
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData) {
            List<Vector2Int> viableTargets = GetViableTargets(selector, (PurchasableData)targetData);
            GridController.UpdateSelectableCells(viableTargets, selector);
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
    }
}