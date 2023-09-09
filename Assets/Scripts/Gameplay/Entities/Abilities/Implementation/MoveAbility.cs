using System;
using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Gameplay.Pathfinding;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for moving a <see cref="GridEntity"/>
    /// </summary>
    public class MoveAbility : AbilityBase<MoveAbilityData, MoveAbilityParameters> {
        private MoveAbilityParameters AbilityParameters => (MoveAbilityParameters) BaseParameters;
        private int _moveCost;
        
        public MoveAbility(MoveAbilityData data, MoveAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        protected override bool CompleteCooldownImpl() {
            // TODO on a somewhat related note, I should really have these ability and CommandManager methods be more clear about which are run on the server and which are run on clients. Can't do Cmd everywhere because that would break SP. 
            Performer.CurrentMoves = Mathf.Min(Performer.CurrentMoves + _moveCost, Performer.MaxMove);
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }
        
        public override void DoAbilityEffect() {
            // Perform a single move towards the destination
            List<GridNode> path = GameManager.Instance.PathfinderService.FindPath(Performer, AbilityParameters.Destination);
            if (path == null || path.Count < 2) {
                throw new Exception("Could not find path for move ability when attempting to perform its effect");
            }
            
            GameManager.Instance.CommandManager.MoveEntityToCell(Performer, path[1].Location);
            if (path.Count > 2) {
                // There is more distance to travel, so put a new movement at the front of the queue
                Performer.QueueAbility(Data, AbilityParameters, WaitUntilLegal, false, true);
            }
        }

        public override void SerializeParameters(NetworkWriter writer) {
            base.SerializeParameters(writer);
            writer.Write(_moveCost);
        }

        public override void DeserializeImpl(NetworkReader reader) {
            base.DeserializeImpl(reader);
            _moveCost = reader.ReadInt();
        }
    }

    public class MoveAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public GridEntity.Team SelectorTeam;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
            writer.Write(SelectorTeam);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
            SelectorTeam = reader.Read<GridEntity.Team>();
        }
    }
}