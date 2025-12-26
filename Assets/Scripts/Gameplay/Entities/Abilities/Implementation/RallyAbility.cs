using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for setting the rally point for a production structure
    /// </summary>
    public class RallyAbility : AbilityBase<RallyAbilityData, RallyAbilityParameters> {
        public RallyAbilityParameters AbilityParameters => (RallyAbilityParameters) BaseParameters;
        
        public RallyAbility(RallyAbilityData data, RallyAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;

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
            GridEntity entityAtDestination = Data.RallyingUnitsAreAttackers
                ? GameManager.Instance.GetTopEntityAtLocation(AbilityParameters.Destination)
                : null;
            if (entityAtDestination != null && entityAtDestination.Team != PerformerTeam.OpponentTeam()) {
                entityAtDestination = null;
            }
            Performer.SetTargetLocation(AbilityParameters.Destination, entityAtDestination, Data.RallyingUnitsAreAttackers);
            return (true, AbilityResult.CompletedWithEffect);
        }
    }

    public class RallyAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
        }

        public string SerializeToJson() {
            return JsonUtility.ToJson(new Dictionary<string, object> {
                {"Destination", Destination}
            });
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
        }
    }
}