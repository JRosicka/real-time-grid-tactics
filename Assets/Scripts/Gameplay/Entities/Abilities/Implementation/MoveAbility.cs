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
        public MoveAbilityParameters AbilityParameters => (MoveAbilityParameters) BaseParameters;
        
        public MoveAbility(MoveAbilityData data, MoveAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // TODO on a somewhat related note, I should really have these ability and CommandManager methods be more clear about which are run on the server and which are run on clients. Can't do Cmd everywhere because that would break SP. 
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }
        
        public override bool DoAbilityEffect() {
            // Perform a single move towards the destination
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, AbilityParameters.Destination);
            List<GridNode> pathNodes = path.Nodes;
            if (pathNodes.Count < 2) {
                return false;
            }

            AbilityParameters.NextMoveCell = pathNodes[1].Location;
            
            GameManager.Instance.CommandManager.MoveEntityToCell(Performer, pathNodes[1].Location);
            if (pathNodes.Count > 2) {
                // There is more distance to travel, so put a new movement at the front of the queue
                Performer.QueueAbility(Data, AbilityParameters, WaitUntilLegal, false, true);
            }

            return true;
        }
    }

    public class MoveAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public Vector2Int NextMoveCell;
        public GridEntity.Team SelectorTeam;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
            writer.Write(NextMoveCell);
            writer.Write(SelectorTeam);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
            NextMoveCell = reader.Read<Vector2Int>();
            SelectorTeam = reader.Read<GridEntity.Team>();
        }
    }
}