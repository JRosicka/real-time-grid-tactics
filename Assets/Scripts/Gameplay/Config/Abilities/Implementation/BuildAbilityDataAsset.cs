using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using UnityEngine;

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

        [Serializable]
        public struct PurchasableDataWithSelectionKey {
            public PurchasableData data;
            public string selectionKey;
        }

        public override bool CancelWhenNewCommandGivenToPerformer => true;
        public override bool Targeted => Targetable;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.SelectionInterface.SelectBuildAbility(this);
        }

        protected override bool AbilityLegalImpl(BuildAbilityParameters parameters, GridEntity entity) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.MyTeam);
            if (!player.ResourcesController.CanAfford(parameters.Buildable.Cost)) {
                Debug.Log($"Not building ({parameters.Buildable.ID}) because we can't pay the cost");
                return false;
            }
            
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;
            if (parameters.Buildable.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                Debug.Log($"Not building ({parameters.Buildable.ID}) because we don't have the proper requirements");
                return false;
            }

            return true;
        }

        protected override IAbility CreateAbilityImpl(BuildAbilityParameters parameters, GridEntity performer) {
            return new BuildAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            EntityData entityToBuild = (EntityData)targetData;
            GameplayTile tileAtLocation = GridController.GridData.GetCell(cellPosition).Tile;
            return entityToBuild.EligibleStructureLocations.Contains(tileAtLocation) 
                   && PathfinderService.CanEntityEnterCell(cellPosition, entityToBuild, selectorTeam, new List<GridEntity>{selectedEntity});
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            BuildAbilityParameters buildParameters = new BuildAbilityParameters {Buildable = (PurchasableData) targetData, BuildLocation = cellPosition};
            selectedEntity.QueueAbility(this, buildParameters, true, false, false);
            selectedEntity.SetTargetLocation(cellPosition, null);
        }

        public void RecalculateTargetableAbilitySelection(GridEntity selector) {
            // Nothing to do
        }

        public bool MoveToTargetCellFirst => true;
    }
}