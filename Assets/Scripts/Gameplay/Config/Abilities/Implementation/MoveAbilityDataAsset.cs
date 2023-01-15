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
            GameManager.Instance.SelectionInterface.SelectTargetableAbility(this);
        }

        public override bool AbilityLegalImpl(MoveAbilityParameters parameters, GridEntity entity) {
            // TODO Pathfinding stuff
            return base.AbilityLegalImpl(parameters, entity);
        }

        protected override IAbility CreateAbilityImpl(MoveAbilityParameters parameters, GridEntity performer) {
            return new MoveAbility(this, parameters, performer);
        }

        public bool CanTargetCell(Vector2Int cellPosition, GridEntity selector, GridEntity target) {
            // TODO pathfinding stuff
            return target == null && selector != null;
        }

        public void CreateAbility(Vector2Int cellPosition, GridEntity selector, GridEntity target) {
            selector.DoAbility(this, new MoveAbilityParameters {Destination = cellPosition});
        }
    }
}