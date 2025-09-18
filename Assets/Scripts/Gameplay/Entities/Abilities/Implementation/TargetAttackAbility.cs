using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for attacking. Attacks a specific <see cref="GridEntity"/> and moves towards it
    /// if out of range.
    /// </summary>
    public class TargetAttackAbility : AbilityBase<TargetAttackAbilityData, TargetAttackAbilityParameters> {
        public TargetAttackAbilityParameters AbilityParameters => (TargetAttackAbilityParameters) BaseParameters;
        private GridData GridData => GameManager.Instance.GridController.GridData;

        public TargetAttackAbility(TargetAttackAbilityData data, TargetAttackAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {}

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.Interaction;
        public override bool ShouldShowCooldownTimer => true;
        protected override float AddedMovementTime => Performer.EntityData.AddedMovementTimeFromAttacking;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(Performer.Team)
                .Contains(Performer)) {
                // The entity must be in the process of being killed since it is not present in the entities collection
                return (false, AbilityResult.Failed);
            }

            // Check to make sure that the performer still exists
            Vector2Int? attackerLocation = Performer == null ? null : Performer.Location;
            if (attackerLocation == null) {
                return (false, AbilityResult.Failed);
            }
            
            Vector2Int? targetLocation = AbilityParameters.Target == null || AbilityParameters.Target.DeadOrDying 
                ? null 
                : AbilityParameters.Target.Location;
            if (targetLocation == null) {
                // If the target no longer exists, then it must have been killed or turned into a structure or something. 
                // STOP in that case.
                return (false, AbilityResult.CompletedWithoutEffect);
            }

            // Try to attack the target if it is in range
            if (CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, targetLocation.Value) <= Performer.Range) {
                if (Performer.ActiveTimers.Any(t => t.Ability.AbilityData.Channel == AbilityData.Channel)) {
                    // We are in range of the target, but attacking is on cooldown. Do nothing for now. 
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
                
                // Otherwise actually attack
                DoAttack(targetLocation.Value);
                return (true, AbilityResult.IncompleteWithEffect);
            }
            
            // If no move available, then don't do anything else for now
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // Otherwise move closer to the target and try again
            StepTowardsDestination(Performer, targetLocation.Value);
            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        /// <summary>
        /// Move a single cell towards the destination
        /// </summary>
        private void StepTowardsDestination(GridEntity attacker, Vector2Int destination) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, destination);
            if (path.Nodes.Count < 2) {
                return;
            }
            
            Vector2Int nextMoveCell = path.Nodes[1].Location;
            MoveAbilityData moveAbilityData = attacker.GetAbilityData<MoveAbilityData>();
            AbilityAssignmentManager.StartPerformingAbility(attacker, moveAbilityData, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                SelectorTeam = attacker.Team,
                BlockedByOccupation = false,
                PerformAfterAttacks = true
            }, false, true, false);
        }

        private void DoAttack(Vector2Int location) {
            // Even though we have our target, we need to check if there is any viable target on top of the target. If so, 
            // then the attack needs to go towards whatever entity is on top of the stack. Them's the rules. 
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(location);
            if (target == null) {
                Debug.LogWarning("Unexpectedly failed to find the attack target");
                return;
            }
            
            GameManager.Instance.AttackManager.PerformAttack(Performer, target, 0, false);
        }
    }
    
    public class TargetAttackAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
        }
    }
}