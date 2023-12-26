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

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override void DoAbilityEffect() {
            Debug.Log("Did siege ability, cool");
        }
    }
}