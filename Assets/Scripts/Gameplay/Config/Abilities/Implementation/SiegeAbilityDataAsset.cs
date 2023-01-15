using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/SiegeAbilityData")]
    public class SiegeAbilityDataAsset : BaseAbilityDataAsset<SiegeAbilityData, NullAbilityParameters> { }
    
    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to enter/exit siege mode
    /// </summary>
    [Serializable]
    public class SiegeAbilityData : AbilityDataBase<NullAbilityParameters> {
        public override void SelectAbility(GridEntity selector) {
            selector.DoAbility(this, new NullAbilityParameters());
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new SiegeAbility(this, parameters, performer);
        }
    }
}