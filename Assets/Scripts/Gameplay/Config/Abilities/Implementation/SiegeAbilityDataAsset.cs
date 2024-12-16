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
        public override bool CanBeCanceled => true;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => true;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.AbilityAssignmentManager.PerformAbility(selector, this, new NullAbilityParameters(), false);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new SiegeAbility(this, parameters, performer);
        }
    }
}