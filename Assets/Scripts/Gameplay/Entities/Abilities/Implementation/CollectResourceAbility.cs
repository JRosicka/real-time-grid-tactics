using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for collecting a resource. Like <see cref="PickUpResourceAbility"/> but for a collector
    /// entity rather than for the collected entity.
    /// </summary>
    public class CollectResourceAbility : AbilityBase<CollectResourceAbilityData, CollectResourceAbilityParameters> {
        public CollectResourceAbilityParameters AbilityParameters => (CollectResourceAbilityParameters) BaseParameters;

        public CollectResourceAbility(CollectResourceAbilityData data, CollectResourceAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => true;

        public override void Cancel() {
            // Nothing to do
        }
        
        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer
                || Performer.Location == null
                || !AbilityParameters.Target 
                || AbilityParameters.Target.Location == null 
                || AbilityParameters.Target.DeadOrDying) {
                return (false, AbilityResult.CompletedWithoutEffect);
            }
            
            UpdateTargetLocation();

            // Try to collect the target if it is in range
            if (CellDistanceLogic.DistanceBetweenCells(Performer.Location.Value, AbilityParameters.Target.Location.Value) == 1) {
                if (Performer.ActiveTimers.Any(t => t.Ability.AbilityData.Channel == AbilityData.Channel)) {
                    // We are in range of the target, but collecting is in-progress. Do nothing for now. 
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
                
                // Otherwise start collection
                AbilityParameters.Target.UnregisteredEvent -= CancelCollection;
                AbilityParameters.Target.UnregisteredEvent += CancelCollection;
                Performer.HPHandler.AttackedEvent -= PerformerAttacked;
                Performer.HPHandler.AttackedEvent += PerformerAttacked;
                return (true, AbilityResult.IncompleteWithEffect);
            }

            
            // If no move available, then don't do anything else for now
            if (Performer.ActiveTimers.Any(t => t.Ability is MoveAbility)) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }
            
            // Otherwise move closer to the target if not holding position 
            if (!Performer.HoldingPosition) {
                StepTowardsDestination(Performer, AbilityParameters.Target.Location.Value);
            }
            
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
                BlockedByOccupation = false,
                PerformAfterAttacks = true
            }, false, true, false, false, attacker.Team);
        }

        private void UpdateTargetLocation() {
            if (!Performer || Performer.Location == null) return;
            if (!AbilityParameters.Target || AbilityParameters.Target.Location == null) {
                if (Performer.TargetLocationLogicValue.CurrentTarget != Performer.Location.Value ||
                    Performer.TargetLocationLogicValue.TargetEntity != null) {
                    Performer.SetTargetLocation(Performer.Location.Value, null, false);
                }
            } else if (Performer.TargetLocationLogicValue.CurrentTarget != AbilityParameters.Target.Location.Value || 
                       Performer.TargetLocationLogicValue.TargetEntity != AbilityParameters.Target) {
                Performer.SetTargetLocation(AbilityParameters.Target.Location.Value, AbilityParameters.Target, false, true);
            }
        }

        private void CancelCollection() {
            AbilityParameters.Target.UnregisteredEvent -= CancelCollection;
            Performer.HPHandler.AttackedEvent -= PerformerAttacked;
         
            GameManager.Instance.CommandManager.CancelAbility(this, false);
            Performer.SetTargetLocation(Performer.Location!.Value, null, false);
        }

        private void PerformerAttacked(bool lethal) {
            CancelCollection();
        }
        
        protected override bool CompleteCooldownImpl() {
            // Award the resources to the local team
            ResourceAmount resourceAmount = new ResourceAmount(AbilityParameters.Target.EntityData.StartingResourceSet);
            PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(PerformerTeam).ResourcesController;
            resourcesController.Earn(resourceAmount);
            
            // Now destroy the resource pickup
            GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(AbilityParameters.Target, false);
            
            // Now cancel the ability since we are done with it 
            CancelCollection();
            
            return true;
        }
    }
    
    public class CollectResourceAbilityParameters : IAbilityParameters {
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