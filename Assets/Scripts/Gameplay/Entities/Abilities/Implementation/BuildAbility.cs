using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Commands;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Upgrades;
using Gameplay.Grid;
using Gameplay.Managers;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Util;

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
        public BuildAbilityData BuildAbilityData { get; private set; }

        public BuildAbility(BuildAbilityData data, BuildAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) {
            BuildAbilityData = data;
        }
        
        private AbilityEventRouter AbilityEventRouter => GameManager.Instance.AbilityEventRouter;
        private FogOfWarManager FogOfWarManager => GameManager.Instance.FogOfWarManager;


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

        public override bool ManuallyCancelable => !AbilityParameters.Buildable.BuildsImmediately && base.ManuallyCancelable;

        public override void Cancel() {
            // Refund the amount spent on the build
            foreach (ResourceAmount resources in AbilityParameters.Buildable.Cost) {
                GameManager.Instance.GetPlayerForTeam(PerformerTeam).ResourcesController.Earn(resources);
            }

            if (AbilityParameters.Buildable is UpgradeData upgradeData) {
                // Cancel the upgrade
                GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, Performer, PerformerTeam, UpgradeStatus.NeitherOwnedNorInProgress);
            }
            
            UnregisterListeners();
        }

        protected override bool CompleteCooldownImpl() {
            if (!AbilityParameters.Buildable.BuildsImmediately) {
                return AwardPurchasable();
            }

            UnregisterListeners();
            return true;
        }

        private bool AwardPurchasable() {
            switch (AbilityParameters.Buildable) {
                case EntityData entityData:
                    if (PathfinderService.CanEntityEnterCell(AbilityParameters.BuildLocation, entityData, Performer.Team, new List<GridEntity>{Performer})) {
                        // The location is open to put this entity, so go ahead and spawn it.
                        // Note that we mark the performer entity as being ignorable since it will probably not be unregistered via
                        // the below command before we check if it's legal to spawn this new one. 
                        SpawnEntity(entityData, AbilityParameters.BuildLocation, AbilityParameters.BuildLocation);
                        UnregisterListeners();
                        return true;
                    }
                    
                    if (!Data.Targetable) {
                        // We can potentially still complete the ability. See if we can send the unit to an adjacent cell.
                        Vector2Int? adjacentCell = GetBestAdjacentCellToSpawn(entityData);
                        if (adjacentCell != null) {
                            SpawnEntity(entityData, adjacentCell.Value, AbilityParameters.BuildLocation);

                            UnregisterListeners();
                            return true;
                        }
                        return false;
                    }
                    
                    // The build location(s) is/are occupied, so we can not yet complete the ability
                    return false;
                case UpgradeData upgradeData:
                    GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, Performer, PerformerTeam, UpgradeStatus.Owned);
                    UnregisterListeners();
                    return true;
                default:
                    throw new Exception("Unexpected purchasable data type: " + AbilityParameters.Buildable.GetType());
            }
        }

        // Server method
        private void SpawnEntity(EntityData entityData, Vector2Int buildLocation, Vector2Int spawnerLocation) {
            if (GameManager.Instance == null) return;
            
            GameManager.Instance.CommandManager.SpawnEntity(entityData, buildLocation, Performer.Team, Performer, spawnerLocation, true, true, true);
            if (entityData.IsStructure) {
                // Destroy the builder
                GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(Performer, false, true);
            }
        }

        /// <summary>
        /// Search all adjacent (to the performer) cells and return the cell closest to the first point along the rally
        /// point, but only if the buildable can enter the cell. 
        /// </summary>
        /// <returns>The location of the best viable cell, or null if no cells are viable.</returns>
        private Vector2Int? GetBestAdjacentCellToSpawn(EntityData entityData) {
            PathfinderService.Path path = GameManager.Instance.PathfinderService.FindPath(Performer, Performer.TargetLocationLogicValue.CurrentTarget, 0, FogOfWarManager!.GetTracker(PerformerTeam));
            if (path.Nodes.Count >= 2) {
                // Spawn at the first node along the path to the rally point if we can.
                Vector2Int firstCellAlongRallyPoint = path.Nodes[1].Location;
                GameplayTile tile = GameManager.Instance.GridController.GridData.GetCell(firstCellAlongRallyPoint).Tile;
                if (!GameManager.Instance.TileAccessibilityManager.InaccessibleTiles(entityData).Contains(tile)
                    && PathfinderService.CanEntityEnterCell(firstCellAlongRallyPoint, entityData, Performer.Team)) {
                    return firstCellAlongRallyPoint;
                }
            }
            
            // Otherwise check each adjacent cell to the building entity, prioritizing the ones closest to the destination
            if (Performer.Location == null) return null;
            List<GridData.CellData> orderedAdjacentCells = GameManager.Instance.GridController.GridData
                .GetAdjacentCells(Performer.Location.Value)
                .OrderBy(c => CellDistanceLogic.DistanceBetweenCells(c.Location, Performer.TargetLocationLogicValue.CurrentTarget))
                .ToList();
            foreach (GridData.CellData adjacentCell in orderedAdjacentCells) {
                if (GameManager.Instance.TileAccessibilityManager.InaccessibleTiles(entityData).Contains(adjacentCell.Tile)) continue;
                if (!PathfinderService.CanEntityEnterCell(adjacentCell.Location, entityData, Performer.Team)) continue;
                return adjacentCell.Location;
            }

            // No adjacent cells work
            return null;
        }

        public override bool TryDoAbilityStartEffect() {
            if (!CanPayCost()) {
                return false;
            }
            
            if (AbilityParameters.Buildable is EntityData { IsStructure: true }) {
                // Subscribe to FoW and entity collection updates so we can cancel this ability if we see another structure at the build site
                AbilityEventRouter.RegisterListener<Action>(Performer, UID, 
                    handler => GameManager.Instance.CommandManager.EntityCollectionChangedEvent += handler,
                    handler => GameManager.Instance.CommandManager.EntityCollectionChangedEvent -= handler,
                    EntityCollectionUpdated);
                TeamFogOfWarTracker tracker = FogOfWarManager!.GetTracker(PerformerTeam);
                if (tracker != null) {
                    AbilityEventRouter.RegisterListener<Action<List<TeamFogOfWarTracker.FoWCell>>>(Performer, UID,
                        handler => tracker.FoWUpdated += handler,
                        handler => tracker.FoWUpdated -= handler,
                        FoWUpdated);
                }
            }
            
            // Pay resource cost
            GameManager.Instance.GetPlayerForTeam(PerformerTeam).ResourcesController.Spend(AbilityParameters.Buildable.Cost);
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (AbilityParameters.Buildable is UpgradeData upgradeData) {
                // Mark the upgrade as in-progress
                GameManager.Instance.CommandManager.UpdateUpgradeStatus(upgradeData, Performer, PerformerTeam, UpgradeStatus.InProgress);
            }
            
            if (AbilityParameters.Buildable.BuildsImmediately) {
                bool success = AwardPurchasable();
                if (!success) {
                    return (false, AbilityResult.IncompleteWithoutEffect);
                }
            }

            // We are starting to build now
            UnregisterListeners();
            return (true, AbilityResult.CompletedWithEffect);
        }
        
        private bool CanPayCost() {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(PerformerTeam);
            if (!player.ResourcesController.CanAfford(AbilityParameters.Buildable.Cost)) {
                return false;
            }

            return true;
        }

        private void FoWUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            TeamFogOfWarTracker.FoWCell foWCell = foWCells.FirstOrDefault(c => c.Position == AbilityParameters.BuildLocation);
            if (foWCell is { Hidden: false }) {
                // The build location just entered vision for the performer. Assess whether there is a structure there. 
                if (IsStructureOnBuildLocation()) {
                    GameManager.Instance.CommandManager.CancelAbility(this, false);
                }
            }
        }

        private void EntityCollectionUpdated() {
            if (IsBuildLocationHidden()) return;
            if (IsStructureOnBuildLocation()) {
                GameManager.Instance.CommandManager.CancelAbility(this, false);
            }
        }

        private bool IsBuildLocationHidden() {
            TeamFogOfWarTracker tracker = FogOfWarManager!.GetTracker(PerformerTeam);
            return tracker != null && tracker.IsLocationHidden(AbilityParameters.BuildLocation);
        }

        private bool IsStructureOnBuildLocation() {
            var entities = GameManager.Instance.GetEntitiesAtLocation(AbilityParameters.BuildLocation);
            return entities != null && entities.Entities.Any(e => e.Entity.EntityData.IsStructure);
        }

        private void UnregisterListeners() {
            AbilityEventRouter.UnregisterListeners(Performer, UID);
        }
    }

    public class BuildAbilityParameters : IAbilityParameters {
        public PurchasableData Buildable;
        public Vector2Int BuildLocation;
        public void Serialize(NetworkWriter writer) {
            writer.WriteString(Buildable.ID);
            writer.Write(BuildLocation);
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"Buildable", Buildable.ID},
                {"BuildLocation", BuildLocation.ConvertToString()}
            });
        }

        public void Deserialize(NetworkReader reader) {
            Buildable = GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
            BuildLocation = reader.Read<Vector2Int>();
        }
    }
}