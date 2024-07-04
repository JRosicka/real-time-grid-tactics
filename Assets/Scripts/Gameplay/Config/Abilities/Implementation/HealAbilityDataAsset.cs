using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/HealAbilityData")]
    public class HealAbilityDataAsset : BaseAbilityDataAsset<HealAbilityData, HealAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for healing a friendly unit.
    /// </summary>
    [Serializable]
    public class HealAbilityData : AbilityDataBase<HealAbilityParameters> {
        public override bool CancelWhenNewCommandGivenToPerformer => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }

        public bool CanHeal(HealAbilityParameters parameters, GridEntity entity) {
            return parameters.Target != null 
                   && !parameters.Target.DeadOrDying() 
                   && parameters.Target.CurrentHP < parameters.Target.MaxHP;
        }

        protected override bool AbilityLegalImpl(HealAbilityParameters parameters, GridEntity entity) {
            return CanHeal(parameters, entity);
        }

        protected override IAbility CreateAbilityImpl(HealAbilityParameters parameters, GridEntity performer) {
            return new HealAbility(this, parameters, performer);
        }
    }
}