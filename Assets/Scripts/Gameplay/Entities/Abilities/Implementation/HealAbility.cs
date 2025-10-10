using System.Threading.Tasks;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for healing a friendly unit.
    /// </summary>
    public class HealAbility : AbilityBase<HealAbilityData, HealAbilityParameters> {
        public HealAbilityParameters AbilityParameters => (HealAbilityParameters) BaseParameters;

        public HealAbility(HealAbilityData data, HealAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowCooldownTimer => true;

        public override async void Cancel() {
            if (Performer == null || Performer.DeadOrDying) return;

            if (AbilityParameters.Target != null) {
                AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
            }
            if (AbilityParameters.Target != null) {
                AbilityParameters.Target.KilledEvent -= TargetEntityNoLongerValid;
            }

            // Re-perform, but wait a frame so that we get the chance to finish canceling this ability first
            await Task.Yield();
            AbilityAssignmentManager.StartPerformingAbility(Performer, Data, new HealAbilityParameters {
                Target = null
            }, false, false, false);
        }

        protected override bool CompleteCooldownImpl() {
            if (CanHeal(AbilityParameters.Target)) {
                // Actually perform the heal
                AbilityParameters.Target.HPHandler.Heal(Data.HealAmount);
            }
            
            // Cancel this ability since we are starting another one. We need to start another one instead of just
            // keeping this one because we can't update the target to null from here (in a way where it will persist on
            // the server)
            GameManager.Instance.CommandManager.CancelAbility(this);
            
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer.Registered || Performer.DeadOrDying) return (false, AbilityResult.Failed);
            Vector2Int? location = Performer.Location;
            if (location == null) return (false, AbilityResult.Failed);
            
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(location.Value);
            if (target == Performer) {
                // Nothing on this structure
                target = null;
            }
            
            if (AbilityParameters.Target == null && target != null) {
                if (CanHeal(target)) {
                    // Start the cooldown timer - we just got a target
                    AbilityParameters.Target = target;
                    AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
                    AbilityParameters.Target.KilledEvent -= TargetEntityNoLongerValid;
                    AbilityParameters.Target.EntityMovedEvent += TargetEntityNoLongerValid;
                    AbilityParameters.Target.KilledEvent += TargetEntityNoLongerValid;
                
                    return (true, AbilityResult.IncompleteWithoutEffect);
                } else {
                    // There is a new target, but it can't be healed. Nothing to do
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
            }

            if (target == null && AbilityParameters.Target == null) {
                // Still no target. Nothing to do. 
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            if (target != AbilityParameters.Target || !CanHeal(AbilityParameters.Target)) {
                // Either the entity moved, got killed, or has full HP. Cancel so that it gets re-performed with no target.
                GameManager.Instance.CommandManager.CancelAbility(this);
                return (false, AbilityResult.Failed);
            }
            
            // Otherwise nothing to do - need to wait until cooldown completes
            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        private bool CanHeal(GridEntity target) {
            return target != null && !target.DeadOrDying && target.HPHandler.CurrentHP < target.MaxHP;
        }

        private void TargetEntityNoLongerValid() {
            AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
            AbilityParameters.Target.KilledEvent -= TargetEntityNoLongerValid;
         
            // Cancel the ability timer since the target is no longer heal-able
            GameManager.Instance.CommandManager.CancelAbility(this);
        }
    }

    public class HealAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
        }
    }
}