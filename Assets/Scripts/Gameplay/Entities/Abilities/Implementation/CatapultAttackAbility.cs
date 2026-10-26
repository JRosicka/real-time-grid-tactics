using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using JetBrains.Annotations;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for area-of-effect attacks.
    /// </summary>
    public class CatapultAttackAbility : AbilityBase<CatapultAttackAbilityData, CatapultAttackAbilityParameters> {
        public CatapultAttackAbilityParameters AbilityParameters => (CatapultAttackAbilityParameters) BaseParameters;
        private GridData GridData => GameManager.Instance.GridController.GridData;
        
        public CatapultAttackAbility(CatapultAttackAbilityData data, CatapultAttackAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam = null) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.Interaction;
        public override bool ShouldShowAbilityTimer => true;
        protected override float AddedMovementTime => Performer.MovementTimeFromAttacking;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            if (!Performer.HoldingPosition) {
                // Performer.SetTargetLocation(AbilityParameters.Destination, null, PathVisualizer.PathType.AttackMove);
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
            List<GridEntity> viableTargets = DetermineTargetsForAttackMove(Performer, attackerLocation.Value);  // TODO-abilities if this is too expensive to do every update tick when there are a lot of entities, then we might need to keep track of when the last collection update occurred, and cache that in the parameters and check it before performing this operation.  
            if (viableTargets != null && viableTargets.Any()) {
                if (Performer.ActiveTimers.Any(t => t.Ability.AbilityData.Channel == AbilityData.Channel)) {
                    // We are in range of a target, but attacking is on cooldown. Do nothing for now. 
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }

                // Pick the target
                GridEntity target;
                if (viableTargets.Count == 1) {
                    target = viableTargets[0];
                } else {
                    target = viableTargets[GameManager.Instance.SeedManager.GetRNG(Performer.UID).Next(0, viableTargets.Count)];
                }

                DoAttack(target!.Location!.Value);
                return (true, AbilityResult.IncompleteWithEffect);
            }

            if (Performer.LastAttackedEntityValue != null) {
                // We are not immediately doing an attack when it is available, so clear the last attack target
                Performer.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }

            // // If we are at the destination, then just keep performing the attack move at the current location in case 
            // // anything else comes in range 
            // if (attackerLocation == AbilityParameters.Destination) {
            //     if (AbilityParameters.Reaction) {
            //         // But not for reactions, we want those to actually end.
            //         return (false, AbilityResult.CompletedWithoutEffect);
            //     }
            //     return (false, AbilityResult.IncompleteWithoutEffect);
            // }
            
            // If the attacker is holding position, then don't try to move closer
            if (Performer.HoldingPosition) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // If no move available, then don't do anything else for now
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        /// <summary>
        /// Try to find available targets in range for this attack move
        /// </summary>
        /// <returns>A list of viable targets, or null if there are no valid targets</returns>
        [ItemCanBeNull]
        private List<GridEntity> DetermineTargetsForAttackMove(GridEntity attacker, Vector2Int location) {
            Vector2Int? attackerLocation = attacker.Location;
            if (attackerLocation == null) return null;
            
            List<Vector2Int> cellsInRange = GridData.GetCellsInRange(location, attacker.Range)
                .Select(c => c.Location)
                .ToList();
            // Only get the top entities - can't attack an entity under another entity
            List<GridEntity> enemiesInRange = GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(
                        attacker.Team.OpponentTeam(), true)
                    .Where(e => e.Location != null && cellsInRange.Contains(e.Location.Value))
                    .ToList();
            if (enemiesInRange.Count == 0) {
                // See if we can target a neutral then
                enemiesInRange = GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(
                        GameTeam.Neutral, true)
                    .Where(e => e.Location != null && cellsInRange.Contains(e.Location.Value) && e.EntityData.Attackable)
                    .ToList();
            }
            
            if (enemiesInRange.Count == 0) return null;

            // Only consider the highest-priority targets
            EntityData.TargetPriority highestPriority = enemiesInRange.Max(e => e.EntityData.AttackerTargetPriority);
            enemiesInRange = enemiesInRange
                .Where(e => e.EntityData.AttackerTargetPriority == highestPriority)
                .ToList();
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
                return new List<GridEntity> { attacker.LastAttackedEntityValue };
            }
            
            enemiesInRange = enemiesInRange.OrderBy(e => e.UID).ToList();
            return enemiesInRange;
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

    public class CatapultAttackAbilityParameters : IAbilityParameters {
        public List<Vector2Int> TargetCells;    // If empty, then auto-attack an area with an enemy
        public void Serialize(NetworkWriter writer) {
            writer.WriteInt(TargetCells.Count);
            foreach (Vector2Int targetCell in TargetCells) {
                writer.WriteVector2Int(targetCell);
            }
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"TargetCells", TargetCells.ConvertToString()}
            });
        }

        public void Deserialize(NetworkReader reader) {
            List<Vector2Int> cells = new List<Vector2Int>();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++) {
                cells.Add(reader.ReadVector2Int());
            }

            TargetCells = cells;
        }
    }
}