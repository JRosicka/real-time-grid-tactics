using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Gameplay.UI;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for dropping off a unit.
    /// </summary>
    public class DropOffUnitAbility : AbilityBase<DropOffUnitAbilityData, DropOffUnitAbilityParameters> {
        public DropOffUnitAbilityParameters AbilityParameters => (DropOffUnitAbilityParameters) BaseParameters;

        public DropOffUnitAbility(DropOffUnitAbilityData data, DropOffUnitAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;

        public override void Cancel() { }

        protected override bool CompleteCooldownImpl() {
            return true;
        }
        
        public override bool TryDoAbilityStartEffect() {
            Performer.SetTargetLocation(AbilityParameters.DropOffLocation, null, PathVisualizer.PathType.Move);
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer.IsActive() || !AbilityParameters.Target || AbilityParameters.Target.Location != null || AbilityParameters.Target.DeadOrDying) {
                if (Performer.IsActive()) {
                    Performer.SetTargetLocation(Performer.Location!.Value, null, PathVisualizer.PathType.Move);
                }
                return (false, AbilityResult.CompletedWithoutEffect);
            }
            
            // Try to drop off the target if we are adjacent
            if (CellDistanceLogic.Neighbors(Performer.Location!.Value).Contains(AbilityParameters.DropOffLocation)) {
                if (EntityPickUpLogic.TryDropOff(Performer, AbilityParameters.Target, AbilityParameters.DropOffLocation, false)) {
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
                MoveToDestination(AbilityParameters.DropOffLocation);
            }
            
            return (false, AbilityResult.IncompleteWithoutEffect);

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
    
    public class DropOffUnitAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public Vector2Int DropOffLocation;
        
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
            writer.WriteVector2Int(DropOffLocation);
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"Target", Target?.UID ?? 0},
                {"DropOffLocation", DropOffLocation.ConvertToString()}
            });
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
            DropOffLocation = reader.ReadVector2Int();
        }
    }
}