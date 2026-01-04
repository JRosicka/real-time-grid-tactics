using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for making a <see cref="GridEntity"/> hold position, not reacting to being attacked. 
    /// </summary>
    public class HoldPositionAbility : AbilityBase<HoldPositionAbilityData, NullAbilityParameters> {
        public HoldPositionAbility(HoldPositionAbilityData data, NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            Performer.ToggleHoldPosition(true, false);
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}