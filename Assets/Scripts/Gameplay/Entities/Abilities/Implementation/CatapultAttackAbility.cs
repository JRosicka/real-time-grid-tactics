using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using Gameplay.UI;
using JetBrains.Annotations;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for area-of-effect attacks.
    /// </summary>
    public class CatapultAttackAbility : AbilityBase<CatapultAttackAbilityData, CatapultAttackAbilityParameters>, IAttackAbility {
        public CatapultAttackAbilityParameters AbilityParameters => (CatapultAttackAbilityParameters) BaseParameters;
        private GridData GridData => GameManager.Instance.GridController.GridData;
        
        public CatapultAttackAbility(CatapultAttackAbilityData data, CatapultAttackAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam = null) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.Interaction;
        public override bool ShouldShowAbilityTimer => true;
        protected override float AddedMovementTime => Performer.MovementTimeFromAttacking;
        private TeamFogOfWarTracker FowTracker => GameManager.Instance.FogOfWarManager!.GetTracker(PerformerTeam);

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
            Vector2Int? attackerLocation = Performer.Location;
            if (attackerLocation == null) {
                // The entity must be in the process of being killed (or is picked up) since it is not present in the entities collection
                return (false, AbilityResult.Failed);
            }

            bool notAllowedToAttack = Performer.SiegeState != SiegeState.Sieged 
                                   || Performer.ActiveTimers.Any(t => t.Ability.AbilityData.Channel == AbilityData.Channel);
            
            // If we are targeting a particular location, try to fire there
            if (AbilityParameters.TargetCells is { Count: > 0 }) {
                if (notAllowedToAttack) {
                    // We are in range of a target, but attacking is on cooldown. Do nothing for now. 
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }

                DoAttack(AbilityParameters.TargetCells);
                return (true, AbilityResult.IncompleteWithEffect);
            } 
            
            // Otherwise find a valid target if one exists
            List<GridEntity> viableTargets = DetermineTargetsForAttackMove(Performer, attackerLocation.Value, FowTracker);
            if (viableTargets != null && viableTargets.Any()) {
                if (notAllowedToAttack) {
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

            TryClearLastAttackedEntity();
            
            Performer.SetTargetLocation(attackerLocation.Value, null, PathVisualizer.PathType.AttackMove);

            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        private void TryClearLastAttackedEntity() {
            if (Performer.LastAttackedEntityValue != null) {
                // We are not immediately doing an attack when it is available, so clear the last attack target
                Performer.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }
        }

        private int Range => Data.Range(Performer);

        /// <summary>
        /// Try to find available targets in range for this attack move
        /// </summary>
        /// <returns>A list of viable targets, or null if there are no valid targets</returns>
        [ItemCanBeNull]
        private List<GridEntity> DetermineTargetsForAttackMove(GridEntity attacker, Vector2Int location, TeamFogOfWarTracker fowTracker) {
            Vector2Int? attackerLocation = attacker.Location;
            if (attackerLocation == null) return null;
            
            List<Vector2Int> cellsInRange = GridData.GetCellsInRange(location, Range)
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
            
            // Don't consider entities that are hidden by FoW
            enemiesInRange.RemoveAll(fowTracker.IsEntityHidden);
            
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
            List<Vector2Int> targetCells = new List<Vector2Int> { location };
            
            // Pick random (eligible) adjacent cells to the location to include in the attack, favoring cells closer to the performer
            var cellsInRange = Data.GetCellsInRange(Performer);
            List<Vector2Int> eligibleAdjacentCells = GridData.GetAdjacentCells(location)
                .Select(c => c.Location)
                .Where(c => cellsInRange.Contains(c))
                .OrderBy(c => CellDistanceLogic.DistanceBetweenCells(c, Performer.Location.Value))
                .ToList();
            
            // Pick the closest to be in the target group
            targetCells.Add(eligibleAdjacentCells.First());
            
            // Get the third cell. It might not be a real cell if this is on the edge of the map!
            List<Vector2Int> eligibleThirdCells = CellDistanceLogic.Neighbors(targetCells[0]).Intersect(CellDistanceLogic.Neighbors(targetCells[1])).ToList();
            targetCells.Add(eligibleThirdCells.OrderBy(c => CellDistanceLogic.DistanceBetweenCells(c, Performer.Location.Value)).First());
            
            DoAttack(targetCells.ToHashSet());
        }
        
        private void DoAttack(HashSet<Vector2Int> location) {
            GameManager.Instance.AttackManager.PerformAttack(Performer, location, 0, true);
        }
    }

    public class CatapultAttackAbilityParameters : IAbilityParameters {
        public HashSet<Vector2Int> TargetCells = new HashSet<Vector2Int>();    // If empty, then auto-attack an area with an enemy
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
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++) {
                cells.Add(reader.ReadVector2Int());
            }

            TargetCells = cells;
        }
    }
}