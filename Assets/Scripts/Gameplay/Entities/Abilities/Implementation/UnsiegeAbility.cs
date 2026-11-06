using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for unsieging
    /// </summary>
    public class UnsiegeAbility : AbilityBase<UnsiegeAbilityData, NullAbilityParameters> {
        public UnsiegeAbility(UnsiegeAbilityData data, NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        
        public override bool ShouldShowAbilityTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            Performer.SetSiegeState(SiegeState.Unsieged);
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            Performer.SetSiegeState(SiegeState.Unsieging);
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            // Real unsiege happens at the end of cooldown
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}