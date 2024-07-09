using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for setting the rally point for a production structure
    /// </summary>
    public class RallyAbility : AbilityBase<RallyAbilityData, RallyAbilityParameters> {
        public RallyAbilityParameters AbilityParameters => (RallyAbilityParameters) BaseParameters;
        
        public RallyAbility(RallyAbilityData data, RallyAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }
        
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
            Performer.RallyLogic.RallyPoint = AbilityParameters.Destination;
            return true;
        }
    }

    public class RallyAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
        }
    }
}