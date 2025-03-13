using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for making a <see cref="GridEntity"/> hold position, not reacting to being attacked. 
    /// </summary>
    public class HoldPositionAbility : AbilityBase<HoldPositionAbilityData, NullAbilityParameters> {
        public HoldPositionAbility(HoldPositionAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) { }

        public override bool ShouldShowCooldownTimer => false;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }
        
        public override bool DoAbilityEffect() {
            Performer.ToggleHoldPosition(true);
            return true;
        }
    }
}