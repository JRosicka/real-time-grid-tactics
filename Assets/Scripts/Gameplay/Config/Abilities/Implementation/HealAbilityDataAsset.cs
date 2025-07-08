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
        public int HealAmount;
        public override bool CancelableWhileOnCooldown => true;
        public override bool CancelableWhileInProgress => false;
        public override bool CancelableManually => false;
        public override IAbilityParameters OnStartParameters => new HealAbilityParameters { Target = null };

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }
        
        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override AbilityLegality AbilityLegalImpl(HealAbilityParameters parameters, GridEntity entity) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(HealAbilityParameters parameters, GridEntity performer) {
            return new HealAbility(this, parameters, performer);
        }
    }
}