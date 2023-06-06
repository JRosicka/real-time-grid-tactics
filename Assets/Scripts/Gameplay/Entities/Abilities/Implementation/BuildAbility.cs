using System;
using System.Collections.Generic;
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
    /// build classes for workers making stuff (targetable builds, like castles). And ANOTHER set for workers capturing
    /// neutral structures (like income structures) if those end up being capturable. Though that's sort of functionally
    /// the same as building strucutres, maybe I just don't have neutral ones hmmmmmm.  
    /// </summary>
    public class BuildAbility : AbilityBase<BuildAbilityData, BuildAbilityParameters> {
        private BuildAbilityParameters AbilityParameters => (BuildAbilityParameters) BaseParameters;

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        protected override bool CompleteCooldownImpl() {
            switch (AbilityParameters.Buildable) {
                case EntityData entityData:
                    if (GameManager.Instance.GridController.CanEntityEnterCell(AbilityParameters.BuildLocation, entityData, Performer.MyTeam, new List<GridEntity>{Performer})) {
                        // The location is open to put this entity, so go ahead and spawn it
                        if (entityData.IsStructure) {
                            // Destroy the builder first. TODO Is this guaranteed to happen before the below spawn command? If not then the server recheck of CanEntityEnterCell will fail because the builder still exists at the entity location. 
                            GameManager.Instance.CommandManager.UnRegisterEntity(Performer, false);
                        }
                        GameManager.Instance.CommandManager.SpawnEntity(entityData, AbilityParameters.BuildLocation,
                            Performer.MyTeam);
                        return true;
                    } else {
                        // The build location is occupied, so we can not yet complete the ability
                        return false;
                    }
                case UpgradeData upgradeData:
                    GameManager.Instance.CommandManager.AddUpgrade(upgradeData, Performer.MyTeam);
                    return true;
                default:
                    throw new Exception("Unexpected purchasable data type: " + AbilityParameters.Buildable.GetType());
            }
        }


        protected override void PayCostImpl() {
            // Pay resource cost
            GameManager.Instance.GetPlayerForTeam(Performer.MyTeam).ResourcesController.Spend(AbilityParameters.Buildable.Cost);
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