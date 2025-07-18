using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Grid;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for moving a <see cref="GridEntity"/>
    /// </summary>
    public class ChargeAbility : AbilityBase<ChargeAbilityData, ChargeAbilityParameters> {
        public ChargeAbilityParameters AbilityParameters => (ChargeAbilityParameters) BaseParameters;

        private static ICommandManager CommandManager => GameManager.Instance.CommandManager;
        
        public ChargeAbility(ChargeAbilityData data, IAbilityParameters abilityParameters, GridEntity performer) : base(data, abilityParameters, performer) { }
        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowCooldownTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            // Determine if there is an enemy entity at the destination
            GridEntity targetEntity = GameManager.Instance.GetTopEntityAtLocation(AbilityParameters.Destination);
            if (targetEntity == null || (Performer.GetTargetType(targetEntity) != GridEntity.TargetType.Enemy && targetEntity.EntityData.FriendlyUnitsCanShareCell)) {
                // No entity there (or it's a friendly entity that we can share a cell with), so move there.
                // Assume that AbilityLegal checked for movement legality. 
                CommandManager.MoveEntityToCell(Performer, AbilityParameters.Destination);
                PerformFollowUpAttackMove(null);
                return (true, AbilityResult.CompletedWithEffect);
            }
            
            // Entity exists, so move to one cell away from it in a straight line (Assume that AbilityLegal checked for movement legality)...
            // ReSharper disable once PossibleInvalidOperationException     No way for performer to be unregistered
            Vector2Int performerLocation = Performer.Location.Value;
            List<Vector2Int> cellsInLine = CellDistanceLogic.GetCellsInStraightLine(performerLocation, AbilityParameters.Destination);
            if (cellsInLine == null || cellsInLine.Count == 0) {
                Debug.LogError("Uhhh something about the charge ability movement got screwed up");
                return (false, AbilityResult.Failed);
            }
            if (cellsInLine.Count > 1) {
                Vector2Int oneAwayFromDestination = cellsInLine[^2];
                AbilityParameters.MoveDestination = oneAwayFromDestination;
                CommandManager.MoveEntityToCell(Performer, oneAwayFromDestination);
            }   // Otherwise if the count is one, then we are already one cell away from the target, so don't move

            // ...then attack it
            AttackTargetAtEndOfCharge(targetEntity);
            return (true, AbilityResult.CompletedWithEffect);
        }

        private void AttackTargetAtEndOfCharge(GridEntity targetEntity) {
            int bonusDamage = Data.GetBonusDamage(Performer.Team);
            bool attacked = GameManager.Instance.AttackManager.PerformAttack(Performer, targetEntity, bonusDamage, true);
            if (attacked) {
                GameManager.Instance.AbilityAssignmentManager.AddAttackTime(Performer, Data.AddedAttackTime);
                AbilityParameters.Attacking = true; 
            }
            PerformFollowUpAttackMove(targetEntity);
        }

        private void PerformFollowUpAttackMove(GridEntity targetEntity) {
            if (targetEntity != null && targetEntity.DeadOrDying) {
                // Could happen if we just did a charge-attack on this entity and that attack killed it
                targetEntity = null;
            }
            
            if (AbilityParameters.Destination != AbilityParameters.ClickLocation) {
                // Don't target-fire the charge-attacked entity unless we specifically clicked on it with the charge
                targetEntity = null;
            }
            
            AttackAbilityData attackData = Performer.GetAbilityData<AttackAbilityData>();
            AbilityAssignmentManager.StartPerformingAbility(Performer, attackData, new AttackAbilityParameters {
                TargetFire = targetEntity != null,
                Target = targetEntity,
                Destination = AbilityParameters.ClickLocation,
                Reaction = false,
                ReactionTarget = null
            }, false, true, true);
            Performer.SetTargetLocation(AbilityParameters.ClickLocation, targetEntity, true);
        }
    }

    public class ChargeAbilityParameters : IAbilityParameters {
        // The location where we are attacking at the end of a charge, or the move destination if we are not performing an attack with this charge
        public Vector2Int Destination;
        // Same as Destination if not attacking a target there, otherwise this is the cell right before Destination in line
        public Vector2Int MoveDestination;
        public Vector2Int ClickLocation;
        public GameTeam SelectorTeam;
        // Whether this charge results in an attack. Gets set when resolving the charge. 
        public bool Attacking;

        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
            writer.Write(MoveDestination);
            writer.Write(ClickLocation);
            writer.Write(SelectorTeam);
            writer.WriteBool(Attacking);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
            MoveDestination = reader.Read<Vector2Int>();
            ClickLocation = reader.Read<Vector2Int>();
            SelectorTeam = reader.Read<GameTeam>();
            Attacking = reader.ReadBool();
        }
    }
}