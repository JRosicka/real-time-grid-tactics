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
            return CanTargetCell(parameters.Destination, entity);
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selector) {
            // TODO pathfinding stuff
            if (selector == null || selector.MyTeam != GameManager.Instance.LocalPlayer.Data.Team) return false;

            if (selector.Location == cellPosition) {
                // Bro you're already here
                return false;
            }

            return GameManager.Instance.GridController.CanEntityEnterCell(cellPosition, selector.Data, selector.MyTeam);
        }
        
        public void DoTargetableAbility(Vector2Int cellPosition, GridEntity selector) {
            selector.DoAbility(this, new MoveAbilityParameters {Destination = cellPosition});
        }
    }
}