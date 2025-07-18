using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/RallyAbilityData")]
    public class RallyAbilityDataAsset : BaseAbilityDataAsset<RallyAbilityData, RallyAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to set the rally point for a production structure.
    ///
    /// This is not targetable because there isn't a great way to select a targetable ability for them because they already
    /// automatically select their produce ability. So, we instead trigger these with entity <see cref="IInteractBehavior"/>. 
    /// </summary>
    [Serializable]
    public class RallyAbilityData : AbilityDataBase<RallyAbilityParameters> {
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool CancelableManually => false;
        
        public bool UseAttackIconOnPath;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }
        
        protected override AbilityLegality AbilityLegalImpl(RallyAbilityParameters parameters, GridEntity entity) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(RallyAbilityParameters parameters, GridEntity performer) {
            return new RallyAbility(this, parameters, performer);
        }
    }
}