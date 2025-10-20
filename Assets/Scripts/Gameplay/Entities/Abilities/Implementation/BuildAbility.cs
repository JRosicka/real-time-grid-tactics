using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Upgrades;
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

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            
        }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;

        public override float CooldownDuration => GameManager.Instance.Cheats.RemoveBuildTime ? .1f 
            : AbilityParameters.Buildable.BuildTime;

        public override bool ShouldShowAbilityTimer {
            get {
                // Always show the build progress for builder units
                if (!Performer.EntityData.IsStructure) return true;
                // We need to be a spectator or the performer's owner to see a structure's builds
                return Performer.InteractBehavior != null && Performer.InteractBehavior.AllowedToSeeQueuedBuilds(PerformerTeam);
            }
        }

        public override void Cancel() {
            // Refund the amount spent on the build
            foreach (ResourceAmount resources in AbilityParameters.Buildable.Cost) {
                GameManager.Instance.GetPlayerForTeam(PerformerTeam).ResourcesController.Earn(resources);
            }

            if (AbilityParameters.Buildable is UpgradeData upgradeData) {
                // Cancel the upgrade
                GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, PerformerTeam, UpgradeStatus.NeitherOwnedNorInProgress);
            }
        }

        protected override bool CompleteCooldownImpl() {
            if (!AbilityParameters.Buildable.BuildsImmediately) {
                return AwardPurchasable();
            }

            return true;
        }

        private bool AwardPurchasable() {
            switch (AbilityParameters.Buildable) {
                case EntityData entityData:
                    if (PathfinderService.CanEntityEnterCell(AbilityParameters.BuildLocation, entityData, Performer.Team, new List<GridEntity>{Performer})) {
                        // The location is open to put this entity, so go ahead and spawn it.
                        // Note that we mark the performer entity as being ignorable since it will probably not be unregistered via
                        // the below command before we check if it's legal to spawn this new one. 
                        SpawnEntity(entityData, AbilityParameters.BuildLocation, true);
                        return true;
                    }
                    
                    if (!Data.Targetable) {
                        // We can potentially still complete the ability. See if we can send the unit to an adjacent cell.
                        Vector2Int? adjacentCell = GetBestAdjacentCellToSpawn(entityData);
                        if (adjacentCell != null) {
                            SpawnEntity(entityData, adjacentCell.Value, false);
                            
                            return true;
                        }
                        return false;
                    }
                    
                    // The build location(s) is/are occupied, so we can not yet complete the ability
                    return false;
                case UpgradeData upgradeData:
                    GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, PerformerTeam, UpgradeStatus.Owned);
                    return true;
                default:
                    throw new Exception("Unexpected purchasable data type: " + AbilityParameters.Buildable.GetType());
            }
        }

        // Server method
        private void SpawnEntity(EntityData entityData, Vector2Int buildLocation, bool originalBuildLocation) {
            if (GameManager.Instance == null) return;
            GameManager.Instance.CommandManager.SpawnEntity(entityData, buildLocation, Performer.Team, Performer, !originalBuildLocation, true);
            if (entityData.IsStructure) {
                // Destroy the builder.
                GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(Performer, false);
            }
        }

        /// <summary>
        /// Search all adjacent (to the performer) cells and return the cell closest to the first point along the rally
        /// point, but only if the buildable can enter the cell. 
        /// </summary>
        /// <returns>The location of the best viable cell, or null if no cells are viable.</returns>
        private Vector2Int? GetBestAdjacentCellToSpawn(EntityData entityData) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, Performer.TargetLocationLogicValue.CurrentTarget);
            if (path.Nodes.Count < 2) {
                return null;
            }
            Vector2Int firstCellAlongRallyPoint = path.Nodes[1].Location;
            
            if (GameManager.Instance.GridController.GridData.GetCell(firstCellAlongRallyPoint).Tile.InaccessibleTags.Any(t => entityData.Tags.Contains(t))
                    || !PathfinderService.CanEntityEnterCell(firstCellAlongRallyPoint, entityData, Performer.Team)) {
                return null;
            }

            return firstCellAlongRallyPoint;
        }

        public override bool TryDoAbilityStartEffect() {
            if (!CanPayCost()) {
                return false;
            }
            
            // Pay resource cost
            GameManager.Instance.GetPlayerForTeam(PerformerTeam).ResourcesController.Spend(AbilityParameters.Buildable.Cost);
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (AbilityParameters.Buildable is UpgradeData upgradeData) {
                // Mark the upgrade as in-progress
                GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, PerformerTeam, UpgradeStatus.InProgress);
            }
            
            if (AbilityParameters.Buildable.BuildsImmediately) {
                bool success = AwardPurchasable();
                if (!success) {
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
            }

            return (true, AbilityResult.CompletedWithEffect);
        }
        
        private bool CanPayCost() {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(PerformerTeam);
            if (!player.ResourcesController.CanAfford(AbilityParameters.Buildable.Cost)) {
                Debug.Log($"Not building ({AbilityParameters.Buildable.ID}) because we can't pay the cost");
                return false;
            }

            return true;
        }
    }

    public class BuildAbilityParameters : IAbilityParameters {
        public PurchasableData Buildable;
        public Vector2Int BuildLocation;
        public void Serialize(NetworkWriter writer) {
            writer.WriteString(Buildable.ID);
            writer.Write(BuildLocation);
        }

        public void Deserialize(NetworkReader reader) {
            Buildable = GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
            BuildLocation = reader.Read<Vector2Int>();
        }
    }
}