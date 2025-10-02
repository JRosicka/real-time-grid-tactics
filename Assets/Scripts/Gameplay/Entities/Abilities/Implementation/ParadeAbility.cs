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
            if (!Performer.Registered || Performer.DeadOrDying) return (false, AbilityResult.Failed);

            // Is target dead?
            if (AbilityParameters.Target == null) return (false, AbilityResult.Failed);
            
            // Is target out of resources?
            GridEntity resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(AbilityParameters.Target, AbilityParameters.Target.EntityData);
            if (resourceEntity.CurrentResourcesValue?.Amount <= 0) return (false, AbilityResult.Failed);
            
            Vector2Int? targetLocation = AbilityParameters.Target.Location;
            if (targetLocation == null) return (false, AbilityResult.Failed);

            // Check to see if not currently at target
            if (Performer.Location!.Value != targetLocation.Value) return (false, AbilityResult.IncompleteWithoutEffect);
            
            // Do effect
            AbilityParameters.Target.SetIncomeRate(AbilityParameters.Target.IncomeRate + 1);
            return (true, AbilityResult.CompletedWithEffect);
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