using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/IncomeAbilityData")]
    public class IncomeAbilityDataAsset : BaseAbilityDataAsset<IncomeAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to earn resources
    /// </summary>
    [Serializable]
    public class IncomeAbilityData : AbilityDataBase<NullAbilityParameters> {
        public ResourceAmount ResourceAmountIncome;
        public override bool CancelWhenNewCommandGivenToPerformer => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }

        protected override bool AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new IncomeAbility(this, parameters, performer);
        }
    }
}