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
    /// <see cref="IAbility"/> for attacking. Either attacks a specific <see cref="GridEntity"/> and moves towards it
    /// if out of range, or does a general attack-move towards a target cell.
    /// </summary>
    public class AttackAbility : AbilityBase<AttackAbilityData, AttackAbilityParameters> {
        public AttackAbilityParameters AbilityParameters => (AttackAbilityParameters) BaseParameters;
        private GridData GridData => GameManager.Instance.GridController.GridData;
        
        public AttackAbility(AttackAbilityData data, AttackAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) { }

        public override bool ShouldShowCooldownTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
            // TODO some of these are abstract and have empty overrides, and other methods are virtual with the option of overriding. Would be nice to pick one and use that consistently, otherwise it seems like it would be easy to forget some of these when implementing new abilities. 
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override bool DoAbilityEffect() {
            if (AbilityParameters.TargetFire) {
                return DoTargetFireEffect();
            } else {
                return DoAttackMoveEffect();
            }
        }

        private bool DoTargetFireEffect() {
            if (!GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(Performer.Team)
                .Contains(Performer)) {
                // The entity must be in the process of being killed since it is not present in the entities collection
                return false;
            }

            // Check to make sure that the performer still exists
            Vector2Int? attackerLocation = Performer == null ? null : Performer.Location;
            if (attackerLocation == null) {
                return false;
            }
            
            // If the target no longer exists, then it must have been killed or turned into a structure or something. 
            // Do a normal attack move in this case.
            Vector2Int? targetLocation = AbilityParameters.Target == null || AbilityParameters.Target.DeadOrDying 
                ? null 
                : AbilityParameters.Target.Location;
            if (targetLocation == null) {
                AbilityParameters.TargetFire = false;
                return DoAttackMoveEffect();
            } 
            
            // Attack the target if it is in range
            if (CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, targetLocation.Value) <= Performer.Range) {
                DoAttack(targetLocation.Value);
                ReQueue();
                return true;
            }
            
            // Otherwise move closer to the target and try again
            ReQueue();
            StepTowardsDestination(Performer, targetLocation.Value, false);
            return false;
        }
        
        private bool DoAttackMoveEffect() {
            Vector2Int? attackerLocation = Performer.Location;
            if (attackerLocation == null) {
                // The entity must be in the process of being killed since it is not present in the entities collection
                return false;
            }
            
            // First check to see if there is anything in range to attack
            if (AttackInRange(Performer, attackerLocation.Value)) {
                ReQueue();
                return true;
            }

            if (Performer.LastAttackedEntityValue is not null) {
                // We are not immediately doing an attack when it is available, so clear the last attack target
                Performer.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }

            // If we are at the destination, then the attack-move has completed
            if (attackerLocation == AbilityParameters.Destination) {
                return false;
            }
            
            // If the attacker is holding position, then don't try to move closer. Just stop the attack. 
            if (Performer.HoldingPosition) {
                return false;
            }
            
            // If no move available, re-queue this ability for later so that the above check for seeing if anything is 
            // in range is re-performed on the next ability queue update. 
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                ReQueue();
                return false;
            }

            if (AbilityParameters.Reaction) {
                if (AbilityParameters.ReactionTarget != null
                        && !AbilityParameters.ReactionTarget.DeadOrDying
                        && AbilityParameters.ReactionTarget.Location != null) {
                    // No one in range to attack, so move a cell closer to our destination and re-queue
                    StepTowardsDestination(Performer, AbilityParameters.ReactionTarget.Location.Value, true);
                } // Otherwise don't step towards the destination since the reaction target is dead. Just return so we can keep going with the next queued ability
                else if (Performer.QueuedAbilities.All(a => a == this || (a is not MoveAbility && a is not AttackAbility))) {
                    // But first, reset the target location if since don't have any other queued moves or attacks
                    Vector2Int? currentLocation = Performer.Location;
                    // The location might be null if the entity is being destroyed 
                    if (currentLocation != null) {
                        Performer.SetTargetLocation(currentLocation.Value, null, false);
                    }
                }
                return false;
            }
            
            // No one in range to attack, so move a cell closer to our destination and re-queue
            StepTowardsDestination(Performer, AbilityParameters.Destination, true);
            return false;
        }

        /// <summary>
        /// Try to attack something in range
        /// </summary>
        /// <returns>True if there was something to attack, otherwise false</returns>
        private bool AttackInRange(GridEntity attacker, Vector2Int location) {
            Vector2Int? attackerLocation = attacker.Location;
            if (attackerLocation == null) return false;
            
            List<Vector2Int> cellsInRange = GridData.GetCellsInRange(location, attacker.Range)
                .Select(c => c.Location)
                .ToList();
            // Only get the top entities - can't attack an entity behind another entity
            List<GridEntity> enemiesInRange = GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(
                        attacker.Team.OpponentTeam(), true)
                    .Where(e => e.Location != null && cellsInRange.Contains(e.Location.Value))
                    .ToList();
            
            if (enemiesInRange.Count == 0) return false;

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
            if (enemiesInRange.Count == 0) return false;
            
            // If there are multiple viable targets, then disregard the farther-away enemies
            // ReSharper disable PossibleInvalidOperationException      We already confirmed these values are not null
            int closestDistance = enemiesInRange.Min(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value));
            enemiesInRange = enemiesInRange
                .Where(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value) == closestDistance)
                .ToList();

            // If the attacker's last target is a viable target, then pick that one
            Vector2Int? lastAttackedEntityLocation = attacker.LastAttackedEntityValue == null ? null : attacker.LastAttackedEntityValue.Location;
            if (lastAttackedEntityLocation != null && enemiesInRange.Contains(attacker.LastAttackedEntityValue)) {
                AbilityParameters.Target = attacker.LastAttackedEntityValue;
                DoAttack(lastAttackedEntityLocation.Value);
            } else {
                // Otherwise arbitrarily pick one to attack.
                GridEntity target = enemiesInRange[Random.Range(0, enemiesInRange.Count)];
                AbilityParameters.Target = target;
                DoAttack(target.Location.Value);
            }
            // ReSharper restore PossibleInvalidOperationException

            return true;
        }

        /// <summary>
        /// Move a single cell towards the destination
        /// </summary>
        private void StepTowardsDestination(GridEntity attacker, Vector2Int destination, bool reQueueIfPossible) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, destination);
            if (path.Nodes.Count < 2) {
                return;
            }

            if (reQueueIfPossible) {
                // We want the attack move to happen again after the move command, so queue it to the front first
                ReQueue();
            }
            
            Vector2Int nextMoveCell = path.Nodes[1].Location;
            MoveAbilityData moveAbilityData = attacker.GetAbilityData<MoveAbilityData>();
            AbilityAssignmentManager.QueueAbility(attacker, moveAbilityData, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                SelectorTeam = attacker.Team,
                BlockedByOccupation = false
            }, true, false, true, false);
        }

        private void ReQueue() {
            AbilityAssignmentManager.QueueAbility(Performer, Data, AbilityParameters, true, false, true, false);
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