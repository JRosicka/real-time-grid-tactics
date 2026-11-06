using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for entering siege mode
    /// </summary>
    public class SiegeAbility : AbilityBase<SiegeAbilityData, NullAbilityParameters> {
        public SiegeAbility(SiegeAbilityData data, NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        
        public override bool ShouldShowAbilityTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            Performer.SetSiegeState(SiegeState.Sieged);
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            Performer.SetSiegeState(SiegeState.Sieging);
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            // Real siege happens at the end of cooldown
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}