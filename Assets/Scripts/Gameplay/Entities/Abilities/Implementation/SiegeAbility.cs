using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.UI;

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
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (Performer.SiegeState != SiegeState.Unsieged) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // We are actually beginning the siege now. Cancel any in-progress catapult move.
            MoveAbility inProgressMove = Performer.InProgressAbilities.OfType<MoveAbility>().FirstOrDefault();
            if (inProgressMove != null) {
                GameManager.Instance.CommandManager.CancelAbility(inProgressMove, false);
                Performer.SetTargetLocation(Performer.Location!.Value, null, PathVisualizer.PathType.Move);
            }
            
            // Real siege happens at the end of cooldown
            Performer.SetSiegeState(SiegeState.Sieging);
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}