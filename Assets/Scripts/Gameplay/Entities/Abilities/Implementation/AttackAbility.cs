using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for attacking. For doing an attack move (moving towards a target cell and
    /// attacking anything on the way)
    /// </summary>
    public class AttackAbility : AbilityBase<AttackAbilityData, AttackAbilityParameters> {
        public AttackAbilityParameters AbilityParameters => (AttackAbilityParameters) BaseParameters;
        private GridData GridData => GameManager.Instance.GridController.GridData;
        
        public AttackAbility(AttackAbilityData data, AttackAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.Interaction;
        public override bool ShouldShowCooldownTimer => true;
        protected override float AddedMovementTime => Performer.EntityData.AddedMovementTimeFromAttacking;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
            // TODO some of these are abstract and have empty overrides, and other methods are virtual with the option of overriding. Would be nice to pick one and use that consistently, otherwise it seems like it would be easy to forget some of these when implementing new abilities. 
        }

        public override bool TryDoAbilityStartEffect() {
            if (!AbilityParameters.TargetFire && !AbilityParameters.Reaction && !Performer.HoldingPosition) {
                Performer.SetTargetLocation(AbilityParameters.Destination, null, true);
            }
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            Vector2Int? attackerLocation = Performer.Location;
            if (attackerLocation == null) {
                // The entity must be in the process of being killed since it is not present in the entities collection
                return (false, AbilityResult.Failed);
            }
            
            // First check to see if there is anything in range to attack
            GridEntity target = DetermineTargetForAttackMove(Performer, attackerLocation.Value);  // TODO-abilities if this is too expensive to do every update tick when there are a lot of entities, then we might need to keep track of when the last collection update occurred, and cache that in the parameters and check it before performing this operation.  
            if (target != null) {
                if (Performer.ActiveTimers.Any(t => t.Ability is AttackAbility)) {
                    // We are in range of a target, but attacking is on cooldown. Do nothing for now. 
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }

                // Attack the target
                AbilityParameters.Target = target;
                DoAttack(target.Location!.Value);
                return (true, AbilityResult.IncompleteWithEffect);
            }

            if (Performer.LastAttackedEntityValue != null) {
                // We are not immediately doing an attack when it is available, so clear the last attack target
                Performer.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }

            // If we are at the destination, then just keep performing the attack move at the current location in case 
            // anything else comes in range 
            if (attackerLocation == AbilityParameters.Destination) {
                if (AbilityParameters.Reaction) {
                    // But not for reactions, we want those to actually end.
                    return (false, AbilityResult.CompletedWithoutEffect);
                }
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // If the attacker is holding position, then don't try to move closer
            if (Performer.HoldingPosition) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // If no move available, then don't do anything else for now
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }

            if (AbilityParameters.Reaction) {
                if (AbilityParameters.ReactionTarget != null
                        && !AbilityParameters.ReactionTarget.DeadOrDying
                        && AbilityParameters.ReactionTarget.Location != null) {
                    // No one in range to attack, so move a cell closer to our destination
                    Vector2Int newTargetLocation = AbilityParameters.ReactionTarget.Location.Value;
                    StepTowardsDestination(Performer, newTargetLocation);
                    // Update the destination to the new target location
                    AbilityParameters.Destination = newTargetLocation;
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
                
                // Otherwise don't step towards the destination since the reaction target is dead. Just complete the ability.
                return (false, AbilityResult.CompletedWithoutEffect);
            }
            
            // No one in range to attack, so move a cell closer to our destination
            StepTowardsDestination(Performer, AbilityParameters.Destination);
            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        /// <summary>
        /// Try to find the best target in range for this attack move
        /// </summary>
        /// <returns>The best target, otherwise null if there is no viable target</returns>
        private GridEntity DetermineTargetForAttackMove(GridEntity attacker, Vector2Int location) {
            Vector2Int? attackerLocation = attacker.Location;
            if (attackerLocation == null) return null;
            
            List<Vector2Int> cellsInRange = GridData.GetCellsInRange(location, attacker.Range)
                .Select(c => c.Location)
                .ToList();
            // Only get the top entities - can't attack an entity behind another entity
            List<GridEntity> enemiesInRange = GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(
                        attacker.Team.OpponentTeam(), true)
                    .Where(e => e.Location != null && cellsInRange.Contains(e.Location.Value))
                    .ToList();
            
            if (enemiesInRange.Count == 0) return null;

            // Only consider the highest-priority targets
            EntityData.TargetPriority highestPriority = enemiesInRange.Max(e => e.EntityData.AttackerTargetPriority);
            enemiesInRange = enemiesInRange
                .Where(e => e.EntityData.AttackerTargetPriority == highestPriority)
                .ToList();
            if (AbilityParameters.Reaction && AbilityParameters.ReactionTarget != null && !AbilityParameters.ReactionTarget.DeadOrDying) {
                // Potential targets must have a priority at least as high as the attacker we are reacting to
                enemiesInRange.RemoveAll(e => e.EntityData.AttackerTargetPriority <
                                              AbilityParameters.ReactionTarget.EntityData.AttackerTargetPriority);
            }
            if (enemiesInRange.Count == 0) return null;
            
            // If there are multiple viable targets, then disregard the farther-away enemies
            // ReSharper disable PossibleInvalidOperationException      We already confirmed these values are not null
            int closestDistance = enemiesInRange.Min(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value));
            enemiesInRange = enemiesInRange
                .Where(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value) == closestDistance)
                .ToList();

            // If the attacker's last target is a viable target, then pick that one
            Vector2Int? lastAttackedEntityLocation = attacker.LastAttackedEntityValue == null ? null : attacker.LastAttackedEntityValue.Location;
            if (lastAttackedEntityLocation != null && enemiesInRange.Contains(attacker.LastAttackedEntityValue)) {
                return attacker.LastAttackedEntityValue;
            }
            // Otherwise arbitrarily pick one to attack.
            GridEntity target = enemiesInRange[Random.Range(0, enemiesInRange.Count)];
            return target;
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

    public class AttackAbilityParameters : IAbilityParameters {
        public bool TargetFire;
        public GridEntity Target;    // only used for targeting a specific unit
        public Vector2Int Destination; // only used for attack-moves
        public bool Reaction;   // Whether this attack was made in reaction to some other entity
        public GridEntity ReactionTarget;   // Only used for reaction attacks
        public void Serialize(NetworkWriter writer) {
            writer.WriteBool(TargetFire);
            writer.Write(Target);
            writer.Write(Destination);
            writer.Write(Reaction);
            writer.Write(ReactionTarget);
        }

        public void Deserialize(NetworkReader reader) {
            TargetFire = reader.ReadBool();
            Target = reader.Read<GridEntity>();
            Destination = reader.Read<Vector2Int>();
            Reaction = reader.ReadBool();
            ReactionTarget = reader.Read<GridEntity>();
        }
    }
}