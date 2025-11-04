using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for improving a resource collection structure's income rate.
    /// </summary>
    public class ParadeAbility : AbilityBase<ParadeAbilityData, ParadeAbilityParameters> {
        public ParadeAbilityParameters AbilityParameters => (ParadeAbilityParameters) BaseParameters;

        public ParadeAbility(ParadeAbilityData data, ParadeAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;

        public override void Cancel() { }

        protected override bool CompleteCooldownImpl() {
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer.Registered || Performer.DeadOrDying || Performer.Location == null) return (false, AbilityResult.Failed);

            if (AbilityParameters.Target != null) {
                // This is a targeted version of the ability, so detect and cancel any non-targeted instances of this ability
                List<ParadeAbility> inProgressAbilities = Performer.InProgressAbilities.OfType<ParadeAbility>().ToList();
                foreach (ParadeAbility paradeAbility in inProgressAbilities.Where(paradeAbility => paradeAbility.AbilityParameters.Target == null)) {
                    GameManager.Instance.CommandManager.CancelAbility(paradeAbility);
                }
            }
            
            // Is target dead?
            if (AbilityParameters.Target == null || !AbilityParameters.Target.Registered || AbilityParameters.Target.DeadOrDying || AbilityParameters.Target.Location == null) {
                AbilityParameters.Target = null;
            }
            
            GridEntity target = AbilityParameters.Target;
            if (target == null) {
                // Look for a target at the performer's current location
                target = GameManager.Instance.ResourceEntityFinder.GetResourceCollectorAtLocation(Performer.Location.Value);
                if (target == null || target.Location == null) {
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
            }
            
            // Is target out of resources?
            GridEntity resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(target, target.EntityData);
            if (resourceEntity.CurrentResourcesValue?.Amount <= 0) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // Check to see if not currently at target
            Vector2Int targetLocation = target.Location!.Value;
            if (Performer.Location.Value != targetLocation) return (false, AbilityResult.IncompleteWithoutEffect);
            
            // Do effect
            target.SetIncomeRate(target.IncomeRate + 1);
            AbilityParameters.Target = null;
            return (true, AbilityResult.IncompleteWithEffect);
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