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
            GameManager.Instance.GridController.SelectTargetableAbility(this);
        }

        protected override bool AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity) {
            return CanTargetCell(parameters.Destination, entity, parameters.SelectorTeam);
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam) {
            // TODO pathfinding stuff
            if (entity == null || entity.MyTeam != selectorTeam) return false;

            if (entity.Location == cellPosition) {
                // Bro you're already here
                return false;
            }

            return GameManager.Instance.GridController.CanEntityEnterCell(cellPosition, entity.EntityData, entity.MyTeam);
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam) {
            entity.DoAbility(this, new MoveAbilityParameters {Destination = cellPosition, SelectorTeam = selectorTeam});
        }
    }
}