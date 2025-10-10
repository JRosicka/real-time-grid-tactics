using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/PickUpResourceAbilityData")]
    public class PickUpResourceAbilityDataAsset : BaseAbilityDataAsset<PickUpResourceAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for a resource pickup being collected
    /// </summary>
    [Serializable]
    public class PickUpResourceAbilityData : AbilityDataBase<NullAbilityParameters> {
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool CancelableManually => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }
        
        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new PickUpResourceAbility(this, parameters, performer, overrideTeam);
        }
    }
}