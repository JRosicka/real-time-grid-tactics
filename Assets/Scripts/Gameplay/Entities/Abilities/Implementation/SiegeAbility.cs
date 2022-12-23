using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An ability that represents entering/exiting siege mode for an entity
    /// </summary>
    public class SiegeAbility : AbilityBase<SiegeAbilityData, NullAbilityParameters> {
        public SiegeAbility(SiegeAbilityData data, NullAbilityParameters parameters) : base(data, parameters) {
            Debug.Log("Created ability, cool");
        }

        public override void PerformAbility() {
            Debug.Log("Did siege ability, cool");
        }
    }
}