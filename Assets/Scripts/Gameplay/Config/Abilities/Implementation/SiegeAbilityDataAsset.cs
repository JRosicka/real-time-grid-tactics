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
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => true;
        public override bool CancelableManually => false;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.AbilityAssignmentManager.PerformAbility(selector, this, new NullAbilityParameters(), true, false, true);
        }
        
        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new SiegeAbility(this, parameters, performer);
        }
    }
}