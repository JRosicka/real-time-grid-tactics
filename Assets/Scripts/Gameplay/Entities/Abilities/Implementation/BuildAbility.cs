using System;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for building a new <see cref="PurchasableData"/>
    ///
    /// TODO rename these build ability classes to specify that these are for an entity plopping out a new entity. Like a
    /// structure, not like a worker creating a new structure. Also specify this in comments. We will need a new set of
    /// build classes for workers making stuff (targetable builds). 
    /// </summary>
    public class BuildAbility : AbilityBase<BuildAbilityData, BuildAbilityParameters> {
        private BuildAbilityParameters AbilityParameters => (BuildAbilityParameters) BaseParameters;

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override void CompleteCooldown() {
            switch (AbilityParameters.Buildable) {
                case EntityData entityData:
                    // TODO: What if the build location is occupied? Currently does a no-op. Would be better to block building and queue the spawn to happen when the location opens up. 
                    GameManager.Instance.CommandManager.SpawnEntity(entityData, AbilityParameters.BuildLocation,
                        Performer.MyTeam);
                    break;
                case UpgradeData upgradeData:
                    // TODO
                    break;
                default:
                    throw new Exception("Unexpected purchasable data type: " + AbilityParameters.Buildable.GetType());
            }
        }

        protected override void PayCost() {
            // TODO pay AbilityParameters.Buildable.Cost
            base.PayCost();
        }
        
        public override void DoAbilityEffect() {
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
            Buildable = GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
            BuildLocation = reader.Read<Vector2Int>();
        }
    }
}