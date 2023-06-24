using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/MoveAbilityData")]
    public class MoveAbilityDataAsset : BaseAbilityDataAsset<MoveAbilityData, MoveAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to move an entity
    /// </summary>
    [Serializable]
    public class MoveAbilityData : AbilityDataBase<MoveAbilityParameters>, ITargetableAbilityData {

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.GridController.SelectTargetableAbility(this, null);
        }

        protected override bool AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity) {
            return CanTargetCell(parameters.Destination, entity, parameters.SelectorTeam, null);
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            // TODO pathfinding stuff
            if (selectedEntity == null || selectedEntity.MyTeam != selectorTeam) return false;

            if (selectedEntity.Location == cellPosition) {
                // Bro you're already here
                return false;
            }

            return GameManager.Instance.GridController.CanEntityEnterCell(cellPosition, selectedEntity.EntityData, selectedEntity.MyTeam);
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GridEntity.Team selectorTeam, System.Object targetData) {
            selectedEntity.PerformAbility(this, new MoveAbilityParameters {Destination = cellPosition, SelectorTeam = selectorTeam});
        }
    }
}