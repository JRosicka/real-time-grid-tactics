using System;
using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for building a new <see cref="PurchasableData"/>.
    /// Note that this ability covers both structure builds (a structure building a unit) and worker builds (worker building a structure).
    /// 
    /// TODO It might be nice to refactor abilities to not have the "do the functionality at the end of the cooldown"
    /// setting. Seems like it would be more streamlined and better organized to have all abilities do something right
    /// at the start, and to have stuff like this build be handled by some new thing that gets instantiated and handled
    /// on the server. Maybe. 
    /// </summary>
    public class BuildAbility : AbilityBase<BuildAbilityData, BuildAbilityParameters> {
        public BuildAbilityParameters AbilityParameters => (BuildAbilityParameters) BaseParameters;

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override float CooldownDuration => AbilityParameters.Buildable.BuildTime;

        public override void Cancel() {
            // Refund the amount spent on the build
            foreach (ResourceAmount resources in AbilityParameters.Buildable.Cost) {
                GameManager.Instance.GetPlayerForTeam(Performer.MyTeam).ResourcesController.Earn(resources);
            }
        }

        protected override bool CompleteCooldownImpl() {
            switch (AbilityParameters.Buildable) {
                case EntityData entityData:
                    if (PathfinderService.CanEntityEnterCell(AbilityParameters.BuildLocation, entityData, Performer.MyTeam, new List<GridEntity>{Performer})) {
                        // The location is open to put this entity, so go ahead and spawn it.
                        // Note that we mark the performer entity as being ignorable since it will probably not be unregistered via
                        // the below command before we check if it's legal to spawn this new one. 
                        GameManager.Instance.CommandManager.SpawnEntity(entityData, AbilityParameters.BuildLocation, Performer.MyTeam, Performer);
                        if (entityData.IsStructure) {
                            // Destroy the builder.
                            GameManager.Instance.CommandManager.UnRegisterEntity(Performer, false);
                        }
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
        
        public override bool DoAbilityEffect() {
            Debug.Log($"Did build ability for {AbilityParameters.Buildable.ID} at cell {AbilityParameters.BuildLocation}, cool");
            return true;
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