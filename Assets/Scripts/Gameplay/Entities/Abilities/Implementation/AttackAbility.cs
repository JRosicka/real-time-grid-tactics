using System.Collections.Generic;
using System.Linq;
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
                .ActiveEntitiesForTeam(Performer.MyTeam)
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
            Vector2Int? targetLocation = AbilityParameters.Target == null ? null : AbilityParameters.Target.Location;
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
            StepTowardsDestination(Performer, targetLocation.Value);
            ReQueue();
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
            
            // We are not immediately doing an attack when it is available, so clear the last attack target
            Performer.LastAttackedEntity = null;
            
            // If we are at the destination, then the attack-move has completed
            if (attackerLocation == AbilityParameters.Destination) {
                return false;
            }
            
            // If no move available, re-queue this ability for later so that the above check for seeing if anything is 
            // in range is re-performed on the next ability queue update. 
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                ReQueue();
                return false;
            }
            
            // No one in range to attack, so move a cell closer to our destination and re-queue
            if (StepTowardsDestination(Performer, AbilityParameters.Destination)) {
                ReQueue();
            }
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
                        GridEntity.OpponentTeam(attacker.MyTeam), true)
                    .Where(e => e.Location != null && cellsInRange.Contains(e.Location.Value))
                    .ToList();
            
            if (enemiesInRange.Count == 0) return false;

            // If there are multiple viable targets, then disregard the farther-away enemies
            // ReSharper disable PossibleInvalidOperationException      We already confirmed these values are not null
            int closestDistance = enemiesInRange.Min(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value));
            enemiesInRange = enemiesInRange
                .Where(e => CellDistanceLogic.DistanceBetweenCells(attackerLocation.Value, e.Location.Value) == closestDistance)
                .ToList();

            // If the attacker's last target is a viable target, then pick that one
            Vector2Int? lastAttackedEntityLocation = attacker.LastAttackedEntity == null ? null : attacker.LastAttackedEntity.Location;
            if (lastAttackedEntityLocation != null && enemiesInRange.Contains(attacker.LastAttackedEntity)) {
                AbilityParameters.Target = attacker.LastAttackedEntity;
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
        /// <returns>True if we moved a cell, otherwise false if there is no path which actually brings us closer</returns>
        private bool StepTowardsDestination(GridEntity attacker, Vector2Int destination) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, destination);
            if (path.Nodes.Count < 2) {
                return false;
            }
            Vector2Int nextMoveCell = path.Nodes[1].Location;
            IAbilityData moveAbility = attacker.Abilities.First(a => a.Content is MoveAbilityData).Content;
            attacker.PerformAbility(moveAbility, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                SelectorTeam = attacker.MyTeam
            }, true);
            return true;
        }

        private void ReQueue() {
            Performer.QueueAbility(Data, AbilityParameters, true, false, false);
        }

        private void DoAttack(Vector2Int location) {
            // Even though we have our target, we need to check if there is any viable target on top of the target. If so, 
            // then the attack needs to go towards whatever entity is on top of the stack. Them's the rules. 
            GridEntity target = GameManager.Instance.CommandManager.GetEntitiesAtCell(location).GetTopEntity().Entity;
            
            IAttackLogic attackLogic = AttackAbilityLogicFactory.CreateAttackLogic(this);
            attackLogic.DoAttack(Performer, target);
        }
    }

    public class AttackAbilityParameters : IAbilityParameters {
        public bool TargetFire;
        public GridEntity Target;    // only used for targeting a specific unit
        public Vector2Int Destination; // only used for attack-moves
        public void Serialize(NetworkWriter writer) {
            writer.WriteBool(TargetFire);
            writer.Write(Target);
            writer.Write(Destination);
        }

        public void Deserialize(NetworkReader reader) {
            TargetFire = reader.ReadBool();
            Target = reader.Read<GridEntity>();
            Destination = reader.Read<Vector2Int>();
        }
    }
}