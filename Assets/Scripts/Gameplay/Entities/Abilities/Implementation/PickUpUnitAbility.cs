using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Gameplay.UI;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for picking up a unit.
    /// </summary>
    public class PickUpUnitAbility : AbilityBase<PickUpUnitAbilityData, PickUpUnitAbilityParameters> {
        public PickUpUnitAbilityParameters AbilityParameters => (PickUpUnitAbilityParameters) BaseParameters;

        public PickUpUnitAbility(PickUpUnitAbilityData data, PickUpUnitAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;

        public override void Cancel() { }

        protected override bool CompleteCooldownImpl() {
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer.IsActive() || !AbilityParameters.Target.IsActive()) {
                if (Performer.IsActive()) {
                    Performer.SetTargetLocation(Performer.Location!.Value, null, PathVisualizer.PathType.Move);
                }
                return (false, AbilityResult.CompletedWithoutEffect);
            }
            
            UpdateTargetLocation();
            
            // Try to pick up the target if we are adjacent
            if (CellDistanceLogic.Neighbors(Performer.Location!.Value).Contains(AbilityParameters.Target.Location!.Value)) {
                if (EntityPickUpLogic.TryPickUp(Performer, AbilityParameters.Target)) {
                    // Success
                    Performer.SetTargetLocation(Performer.Location.Value, null, PathVisualizer.PathType.Move);
                    return (true, AbilityResult.CompletedWithEffect);
                }
                
                // Otherwise do nothing, since we don't need to move closer
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // If already moving, then don't do anything else for now
            if (Performer.InProgressAbilities.Any(t => t is MoveAbility) || Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // Otherwise move closer to the target if not holding position 
            if (!Performer.HoldingPosition) {
                MoveToDestination(AbilityParameters.Target.Location.Value);
            }
            
            return (false, AbilityResult.IncompleteWithoutEffect);

        }
        
        private void UpdateTargetLocation() {
            if (!Performer || Performer.Location == null) return;
            if (!AbilityParameters.Target || AbilityParameters.Target.Location == null) {
                if (Performer.TargetLocationLogicValue.CurrentTarget != Performer.Location.Value ||
                    Performer.TargetLocationLogicValue.TargetEntity != null) {
                    Performer.SetTargetLocation(Performer.Location.Value, null, PathVisualizer.PathType.Move);
                }
            } else if (Performer.TargetLocationLogicValue.CurrentTarget != AbilityParameters.Target.Location.Value || 
                       Performer.TargetLocationLogicValue.TargetEntity != AbilityParameters.Target) {
                Performer.SetTargetLocation(AbilityParameters.Target.Location.Value, AbilityParameters.Target, PathVisualizer.PathType.Move, true);
            }
        }
        
        /// <summary>
        /// Move a single cell towards the destination
        /// </summary>
        private void MoveToDestination(Vector2Int destination) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, destination, 0, GameManager.Instance.FogOfWarManager!.GetTracker(PerformerTeam));
            if (path.Nodes.Count < 2) {
                return;
            }
            
            Vector2Int nextMoveCell = path.Nodes[1].Location;
            MoveAbilityData moveAbilityData = Performer.GetAbilityData<MoveAbilityData>();
            AbilityAssignmentManager.StartPerformingAbility(Performer, moveAbilityData, new MoveAbilityParameters {
                Destination = nextMoveCell,
                NextMoveCell = nextMoveCell,
                BlockedByOccupation = false,
                PerformAfterAttacks = true
            }, false, false, false, false, Performer.Team);
        }
    }
    
    public class PickUpUnitAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"Target", Target?.UID ?? 0}
            });
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
        }
    }
}