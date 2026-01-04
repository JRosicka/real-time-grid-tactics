using System;
using System.Collections.Generic;
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
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool Cancelable => false;

        public override void SelectAbility(GridEntity selector) {
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(selector, this, new NullAbilityParameters(), 
                true, false, true, true);
        }
        
        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity, GameTeam team) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new HoldPositionAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new NullAbilityParameters();
        }
    }
}