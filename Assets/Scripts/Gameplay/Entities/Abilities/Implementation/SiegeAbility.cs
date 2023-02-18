using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An ability that represents entering/exiting siege mode for an entity
    /// </summary>
    public class SiegeAbility : AbilityBase<SiegeAbilityData, NullAbilityParameters> {
        public SiegeAbility(SiegeAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            Debug.Log("Created ability, cool");
        }

        protected override void CompleteCooldownImpl() {
            // Nothing to do
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override void DoAbilityEffect() {
            Debug.Log("Did siege ability, cool");
        }
    }
}