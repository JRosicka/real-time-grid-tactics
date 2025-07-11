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
        
        public override AbilityExecutionType ExecutionType => AbilityParameters.PerformAfterAttacks 
                                                                ? AbilityExecutionType.PostInteractionGridUpdate 
                                                                : AbilityExecutionType.PreInteractionGridUpdate;

        public override float CooldownDuration {
            get {
                GameplayTile tile = GameManager.Instance.GridController.GridData.GetCell(AbilityParameters.NextMoveCell).Tile;
                return Performer.MoveTimeToTile(tile);
            }
        }

        public override bool ShouldShowCooldownTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // TODO on a somewhat related note, I should really have these ability and CommandManager methods be more clear about which are run on the server and which are run on clients. Can't do Cmd everywhere because that would break SP. 
            return true;
        }

        public override bool TryPayUpFrontCost() {
            // Nothing to do
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            // Perform a single move towards the destination
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, AbilityParameters.Destination);
            List<GridNode> pathNodes = path.Nodes;
            if (pathNodes.Count < 2) {
                // We can not complete the move right now - don't give up skeleton, try again later
                return (false, AbilityResult.IncompleteWithoutEffect);
            }

            AbilityParameters.NextMoveCell = pathNodes[1].Location;
            
            GameManager.Instance.CommandManager.MoveEntityToCell(Performer, pathNodes[1].Location);
            if (pathNodes.Count > 2 || (AbilityParameters.BlockedByOccupation && pathNodes[1].Location != AbilityParameters.Destination)) {
                // There is more distance to travel, so keep this ability for the next execution loop
                AbilityParameters.PerformAfterAttacks = false;
                return (true, AbilityResult.IncompleteWithEffect);
            }

            // Ability complete
            return (true, AbilityResult.CompletedWithEffect);
        }
    }

    public class MoveAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public Vector2Int NextMoveCell;
        public GameTeam SelectorTeam;
        public bool BlockedByOccupation;    // Whether we consider the move illegal when the target location is occupied
        public bool PerformAfterAttacks;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
            writer.Write(NextMoveCell);
            writer.Write(SelectorTeam);
            writer.WriteBool(BlockedByOccupation);
            writer.WriteBool(PerformAfterAttacks);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
            NextMoveCell = reader.Read<Vector2Int>();
            SelectorTeam = reader.Read<GameTeam>();
            BlockedByOccupation = reader.ReadBool();
            PerformAfterAttacks = reader.ReadBool();
        }
    }
}