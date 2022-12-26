using Gameplay.Config;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for building a new <see cref="PurchasableData"/>
    /// </summary>
    public class BuildAbility : AbilityBase<BuildAbilityData, BuildAbilityParameters> {
        private BuildAbilityParameters AbilityParameters => (BuildAbilityParameters) BaseParameters;

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters) : base(data, parameters) {
            
        }
        
        public override void PerformAbility() {
            Debug.Log($"Did build ability for {AbilityParameters.Buildable.ID} at cell {AbilityParameters.BuildLocation}, cool");
        }
    }

    public class BuildAbilityParameters : IAbilityParameters {
        public PurchasableData Buildable;
        public Vector2Int BuildLocation;
        public void Serialize(NetworkWriter writer) {
            writer.WriteString(Buildable.name);
            writer.Write(BuildLocation);
        }

        public void Deserialize(NetworkReader reader) {
            Buildable = Resources.Load<PurchasableData>(reader.ReadString());
            BuildLocation = reader.Read<Vector2Int>();
        }
    }
}