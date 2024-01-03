using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Gameplay.Pathfinding;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for attacking. Either attacks a specific <see cref="GridEntity"/> and moves towards it
    /// if out of range, or does a general attack-move towards a target cell.
    /// </summary>
    public class AttackAbility : AbilityBase<AttackAbilityData, AttackAbilityParameters> {
        private AttackAbilityParameters AbilityParameters => (AttackAbilityParameters) BaseParameters;
        private bool TargetFire => AbilityParameters.Target != null;
        private GridData GridData => GameManager.Instance.GridController.GridData;

        public AttackAbility(AttackAbilityData data, AttackAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

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
            if (TargetFire) {
                return DoTargetFireEffect();
            } else {
                return DoAttackMoveEffect();
            }
        }

        private bool DoTargetFireEffect() {
            GridEntity attacker = AbilityParameters.Attacker;
            Vector2Int attackerLocation = attacker.Location;
            Vector2Int targetLocation = AbilityParameters.Target.Location;

            // Check to make sure that the target still exists
            if (AbilityParameters.Target == null) {
                return false;
            }
            
            // Attack the target if it is in range
            if (CellDistanceLogic.DistanceBetweenCells(attackerLocation, targetLocation) <= attacker.Range) {
                Debug.Log($"Did attack to {AbilityParameters.Target.DisplayName}, cool");
                AbilityParameters.Target.ReceiveAttackFromEntity(AbilityParameters.Attacker);
                ReQueue();
                return true;
            }
            
            // Otherwise move closer to the target and try again
            StepTowardsDestination(attacker);
            ReQueue();
            return false;
        }
        
        private bool DoAttackMoveEffect() {
            GridEntity attacker = AbilityParameters.Attacker;
            Vector2Int attackerLocation = attacker.Location;
            
            // First check to see if there is anything in range to attack
            if (AttackInRange(attacker, attackerLocation)) {
                ReQueue();
                return true;
            }
            
            // If we are at the destination, then the attack-move has completed
            if (attackerLocation == AbilityParameters.Destination) {
                return false;
            }
            
            // If no move available, re-queue this ability for later so that the above check for seeing if anything is 
            // in range is re-performed on the next ability queue update. 
            if (attacker.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                ReQueue();
                return false;
            }
            
            // No one in range to attack, so move a cell closer to our destination and re-queue
            StepTowardsDestination(attacker);
            ReQueue();
            return false;
        }

        /// <summary>
        /// Try to attack something in range
        /// </summary>
        /// <returns>True if there was something to attack, otherwise false</returns>
        private bool AttackInRange(GridEntity attacker, Vector2Int location) {
            List<Vector2Int> cellsInRange = GridData.GetCellsInRange(location, attacker.Range)
                .Select(c => c.Location)
                .ToList();
            List<GridEntity> enemiesInRange =
                GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(
                        GridEntity.OpponentTeam(attacker.MyTeam))
                    .Where(e => cellsInRange.Contains(e.Location))
                    .ToList();
            
            if (enemiesInRange.Count == 0) return false;
            
            // Arbitrarily pick one to attack. TODO pick the closest one instead.
            GridEntity target = enemiesInRange[Random.Range(0, enemiesInRange.Count)];
            target.ReceiveAttackFromEntity(AbilityParameters.Attacker);
            
            return true;
        }

        private void StepTowardsDestination(GridEntity attacker) {
            List<GridNode> path = GameManager.Instance.PathfinderService.FindPath(Performer, AbilityParameters.Destination);
            if (path == null || path.Count < 2) {
                throw new Exception("Could not find path for move ability when attempting to perform its effect");
            }
            Vector2Int nextMoveCell = path[1].Location;
            IAbilityData moveAbility = attacker.Abilities.First(a => a.Content is MoveAbilityData).Content;
            attacker.PerformAbility(moveAbility, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                SelectorTeam = attacker.MyTeam
            }, true);
        }

        private void ReQueue() {
            Performer.QueueAbility(Data, AbilityParameters, true, false, false);
        }
    }

    public class AttackAbilityParameters : IAbilityParameters {
        public GridEntity Attacker;
        public GridEntity Target;    // only used for targeting a specific unit
        public Vector2Int Destination; // only used for attack-moves
        public void Serialize(NetworkWriter writer) {
            writer.Write(Attacker);
            writer.Write(Target);
            writer.Write(Destination);
        }

        public void Deserialize(NetworkReader reader) {
            Attacker = reader.Read<GridEntity>();
            Target = reader.Read<GridEntity>();
            Destination = reader.Read<Vector2Int>();
        }
    }
}