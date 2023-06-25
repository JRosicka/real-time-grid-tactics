using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
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

        [Serializable]
        public struct PurchasableDataWithSelectionKey {
            public PurchasableData data;
            public string selectionKey;
        }

        public override bool Targeted => Targetable;

        public override void SelectAbility(GridEntity selector) {
            Debug.Log(nameof(SelectAbility)); 
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
            return GameManager.Instance.GridController.CanEntityEnterCell(cellPosition, (EntityData)targetData, selectorTeam, new List<GridEntity>{selectedEntity});
        }

        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            BuildAbilityParameters buildParameters = new BuildAbilityParameters {Buildable = (PurchasableData) targetData, BuildLocation = cellPosition};
            
            if (selectedEntity.Location == cellPosition) {
                selectedEntity.PerformAbility(this, buildParameters, true);
            } else {
                selectedEntity.MoveToCell(cellPosition);
                selectedEntity.QueueAbility(this, buildParameters, true);
            }
        }
    }
}