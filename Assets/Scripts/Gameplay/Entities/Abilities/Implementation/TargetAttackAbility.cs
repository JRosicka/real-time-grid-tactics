using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Commands;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for attacking. Attacks a specific <see cref="GridEntity"/> and moves towards it
    /// if out of range.
    /// </summary>
    public class TargetAttackAbility : AbilityBase<TargetAttackAbilityData, TargetAttackAbilityParameters> {
        public TargetAttackAbilityParameters AbilityParameters => (TargetAttackAbilityParameters) BaseParameters;

        public TargetAttackAbility(TargetAttackAbilityData data, TargetAttackAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam = null) : base(data, parameters, performer, overrideTeam) {}

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.Interaction;
        public override bool ShouldShowAbilityTimer => true;
        protected override float AddedMovementTime => Performer.MovementTimeFromAttacking;
        private TeamFogOfWarTracker FowTracker => GameManager.Instance.FogOfWarManager!.GetTracker(PerformerTeam);
        private AbilityEventRouter AbilityEventRouter => GameManager.Instance.AbilityEventRouter;

        public override void Cancel() {
            UnRegisterTargetListeners();
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            RegisterTargetListeners();
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(Performer.Team)
                .Contains(Performer)) {
                // The entity must be in the process of being killed since it is not present in the entities collection
                UnRegisterTargetListeners(); 
                return (false, AbilityResult.Failed);
            }

            // Check to make sure that the performer still exists
            Vector2Int? attackerLocation = Performer == null ? null : Performer.Location;
            if (attackerLocation == null) {
                UnRegisterTargetListeners();
                return (false, AbilityResult.Failed);
            }

            if (!TrackedEntityLocationKnown) {
                if (attackerLocation == AbilityParameters.LastKnownLocation) {
                    // We have lost track of the target, and have moved to its last known location without finding it. We're done here. 
                    UnRegisterTargetListeners();
                    return (false, AbilityResult.CompletedWithoutEffect);
                }
                
                // If no move available, then don't do anything else for now
                if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
                
                // Otherwise move closer to the target if not holding position 
                if (!Performer.HoldingPosition) {
                    StepTowardsDestination(Performer, AbilityParameters.LastKnownLocation, false);
                }

                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            Vector2Int? targetLocation = AbilityParameters.Target == null || AbilityParameters.Target.DeadOrDying 
                ? null 
                : AbilityParameters.Target.Location;
            if (targetLocation == null) {
                // If the target no longer exists, then it must have been killed or turned into a structure or something. 
                UnRegisterTargetListeners();
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
            
            // Otherwise move closer to the target if not holding position 
            if (!Performer.HoldingPosition) {
                StepTowardsDestination(Performer, targetLocation.Value, true);
            }
            
            return (false, AbilityResult.IncompleteWithoutEffect);
        }

        /// <summary>
        /// Move a single cell towards the destination
        /// </summary>
        private void StepTowardsDestination(GridEntity attacker, Vector2Int destination, bool inRangeAcceptable) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, destination, inRangeAcceptable ? Performer.Range : 0, GameManager.Instance.FogOfWarManager!.GetTracker(PerformerTeam));
            if (path.Nodes.Count < 2) {
                return;
            }
            
            Vector2Int nextMoveCell = path.Nodes[1].Location;
            MoveAbilityData moveAbilityData = attacker.GetAbilityData<MoveAbilityData>();
            AbilityAssignmentManager.StartPerformingAbility(attacker, moveAbilityData, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                BlockedByOccupation = false,
                PerformAfterAttacks = true
            }, false, true, false, false, attacker.Team);
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

        private void DoFollowUpAttackMove() {
            if (!Performer || Performer.DeadOrDying) {
                UnRegisterTargetListeners();
                return;
            }
            
            if (AbilityParameters?.Target?.Location != null) {
                Performer.TryAttack(AbilityParameters.Target.Location.Value, null);
            }
        }

        private void RegisterTargetListeners() {
            if (AbilityParameters.Target == null) return;
            
            AbilityEventRouter.RegisterListener<Action>(Performer, UID, 
                handler => AbilityParameters.Target.UnregisteredEvent += handler,
                handler => AbilityParameters.Target.UnregisteredEvent -= handler,
                DoFollowUpAttackMove);
            AbilityEventRouter.RegisterListener<Action>(Performer, UID, 
                handler => AbilityParameters.Target.EntityMovedEvent += handler,
                handler => AbilityParameters.Target.EntityMovedEvent -= handler,
                TrackedEntityMoved);
            TeamFogOfWarTracker tracker = FowTracker;
            if (tracker != null) {
                AbilityEventRouter.RegisterListener<Action<bool>>(Performer, UID,
                    handler => tracker.RegisterEntityListener(AbilityParameters.Target, handler),
                    handler => tracker.UnregisterEntityListener(AbilityParameters.Target, handler),
                    TrackedEntityHiddenStateChanged);
            }
        }

        private void UnRegisterTargetListeners() {
            if (!AbilityParameters?.Target) return;
            
            AbilityEventRouter.UnregisterListeners(Performer, UID);
        }

        // Called on server
        private void TrackedEntityHiddenStateChanged(bool newHidden) {
            if (!newHidden) {
                SetLastKnownLocation(AbilityParameters.Target.Location!.Value);
            }
        }

        // Called on server
        private void TrackedEntityMoved() {
            if (FowTracker == null || !FowTracker.IsEntityHidden(AbilityParameters.Target)) {
                SetLastKnownLocation(AbilityParameters.Target.Location!.Value);
            }
        }

        private void SetLastKnownLocation(Vector2Int location) {
            AbilityParameters.LastKnownLocation = location;
        }

        private bool TrackedEntityLocationKnown => AbilityParameters?.Target?.Location != null && AbilityParameters.Target.Location.Value == AbilityParameters.LastKnownLocation;
    }
    
    public class TargetAttackAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        // For if the entity gets hidden by FoW, from the ability performer's perspective
        public Vector2Int LastKnownLocation;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
            writer.WriteVector2Int(LastKnownLocation);
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"Target", Target?.UID ?? 0},
                {"LastKnownLocation", LastKnownLocation.ConvertToString()},
            });
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
            LastKnownLocation = reader.ReadVector2Int();
        }
    }
}