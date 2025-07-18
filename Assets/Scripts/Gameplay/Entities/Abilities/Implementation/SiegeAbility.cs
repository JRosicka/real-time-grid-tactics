using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// An ability that represents entering/exiting siege mode for an entity
    /// </summary>
    public class SiegeAbility : AbilityBase<SiegeAbilityData, NullAbilityParameters> {
        public SiegeAbility(SiegeAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowCooldownTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}