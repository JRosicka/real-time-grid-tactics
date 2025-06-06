using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/HoldPositionAbilityData")]
    public class HoldPositionAbilityDataAsset : BaseAbilityDataAsset<HoldPositionAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for making a <see cref="GridEntity"/> hold position, not reacting
    /// to being attacked. 
    /// </summary>
    [Serializable]
    public class HoldPositionAbilityData : AbilityDataBase<NullAbilityParameters> {
        public override bool CanBeCanceled => false;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => false;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.AbilityAssignmentManager.PerformAbility(selector, this, new NullAbilityParameters(), true, false);
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new HoldPositionAbility(this, parameters, performer);
        }
    }
}