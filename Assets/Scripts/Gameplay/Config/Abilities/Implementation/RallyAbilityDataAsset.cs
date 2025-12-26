using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.Serialization;

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
        public override bool Cancelable => false;
        
        [FormerlySerializedAs("UseAttackIconOnPath")] 
        public bool RallyingUnitsAreAttackers;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }
        
        protected override AbilityLegality AbilityLegalImpl(RallyAbilityParameters parameters, GridEntity entity, GameTeam team) {
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(RallyAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new RallyAbility(this, parameters, performer, overrideTeam);
        }

        public override IAbilityParameters DeserializeParametersFromJson(Dictionary<string, object> json) {
            return new RallyAbilityParameters {
                Destination = (Vector2Int)json["Destination"]
            };
        }
    }
}