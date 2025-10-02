using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for improving a resource collection structure's income rate.
    /// </summary>
    public class ParadeAbility : AbilityBase<ParadeAbilityData, ParadeAbilityParameters> {
        public ParadeAbilityParameters AbilityParameters => (ParadeAbilityParameters) BaseParameters;

        public ParadeAbility(ParadeAbilityData data, ParadeAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowCooldownTimer => false;

        public override void Cancel() {
            if (Performer == null || Performer.DeadOrDying) return;
            
            AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
            AbilityParameters.Target.KilledEvent -= TargetEntityNoLongerValid;

            // Re-perform
            AbilityAssignmentManager.StartPerformingAbility(Performer, Data, new ParadeAbilityParameters {
                Target = null
            }, false, false, false);
        }

        protected override bool CompleteCooldownImpl() {
            if (CanHeal(AbilityParameters.Target)) {
                // Actually perform the heal
                AbilityParameters.Target.HPHandler.Heal(Data.HealAmount);
                
                // Reset the target so that we try to perform the heal again next execution
                AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
                AbilityParameters.Target.KilledEvent -= TargetEntityNoLongerValid;
                AbilityParameters.Target = null;
            }
            
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
            
            // Is target dead or out of resources? 
            if (AbilityParameters.Target == null || AbilityParameters.Target.CurrentResourcesValue?.Amount <= 0) return (false, AbilityResult.Failed);
            
            // Check to see if not currently at target
            GridEntity resourceCollector = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(location.Value);
            if (resourceCollector == null || resourceCollector != AbilityParameters.Target) return (false, AbilityResult.IncompleteWithoutEffect);
            
            // Do effect
            
            
            
            
            
            
            
            
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

    public class ParadeAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
        }
    }
}