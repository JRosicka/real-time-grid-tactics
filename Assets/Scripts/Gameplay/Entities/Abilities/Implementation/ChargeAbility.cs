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
        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override bool DoAbilityEffect() {
            // Determine if there is an enemy entity at the destination
            GridEntityCollection.PositionedGridEntityCollection destinationEntities = CommandManager.EntitiesOnGrid.EntitiesAtLocation(AbilityParameters.Destination);
            GridEntity targetEntity = destinationEntities?.GetTopEntity()?.Entity;
            if (targetEntity == null) {
                // No entity there, so move there. Assume that AbilityLegal checked for movement legality. 
                CommandManager.MoveEntityToCell(Performer, AbilityParameters.Destination);
                return true;
            }
            
            // Entity exists, so move to one cell away from it in a straight line (Assume that AbilityLegal checked for movement legality)...
            // ReSharper disable once PossibleInvalidOperationException     No way for performer to be unregistered
            Vector2Int performerLocation = Performer.Location.Value;
            List<Vector2Int> cellsInLine = CellDistanceLogic.GetCellsInStraightLine(performerLocation, AbilityParameters.Destination);
            if (cellsInLine == null || cellsInLine.Count == 0) {
                Debug.LogError("Uhhh something about the charge ability movement got screwed up");
                return false;
            }
            if (cellsInLine.Count > 1) {
                Vector2Int oneAwayFromDestination = cellsInLine[^2];
                AbilityParameters.MoveDestination = oneAwayFromDestination;
                CommandManager.MoveEntityToCell(Performer, oneAwayFromDestination);
            }   // Otherwise if the count is one, then we are already one cell away from the target, so don't move

            // ...then attack it
            AttackAbilityData attackAbilityData = (AttackAbilityData) Performer.EntityData.Abilities.First(a => a.Content.GetType() == typeof(AttackAbilityData)).Content;
            Performer.QueueAbility(attackAbilityData, new AttackAbilityParameters {
                TargetFire = true,
                Target = targetEntity,
                Destination = AbilityParameters.Destination
            }, true, false, true);
            return true;
        }
    }

    public class ChargeAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        // Same as Destination if not attacking a target there, otherwise this is the cell right before Destination in line
        public Vector2Int MoveDestination;
        public GridEntity.Team SelectorTeam;

        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
            writer.Write(MoveDestination);
            writer.Write(SelectorTeam);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
            MoveDestination = reader.Read<Vector2Int>();
            SelectorTeam = reader.Read<GridEntity.Team>();
        }
    }
}