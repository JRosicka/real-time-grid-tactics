using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Upgrades;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents a set of <see cref="IUpgrade"/> objects and tracks their ownership states. Upgrade-equivalent of
    /// <see cref="GridEntityCollection"/>, but on a per-player basis and doesn't care about locations. Also only cares about
    /// data objects, not business logic (business logic is tracked here).
    ///
    /// Not directly networked - instead, each client makes its own copy and updates it when receiving associated
    /// RPCs from <see cref="ICommandManager"/>. 
    /// </summary>
    [Serializable]
    public class UpgradesCollection {
        public List<IUpgrade> Upgrades { get; private set; }
        private readonly GameTeam _team;
        
        public UpgradesCollection(GameTeam team) {
            _team = team;
            Upgrades = new List<IUpgrade>();
        }
        
        public void RegisterUpgrades(List<UpgradeData> upgradeDatas) {
            Upgrades = new List<IUpgrade>();
            foreach (UpgradeData upgradeData in upgradeDatas) {
                Upgrades.Add(upgradeData.CreateUpgrade(_team));
            }
        }
        
        public IUpgrade GetUpgrade(UpgradeData data) {
            return Upgrades.FirstOrDefault(u => u.UpgradeData == data);
        }
        
        public List<UpgradeData> GetOwnedUpgradeDatas() {
            return Upgrades.Where(u => u.Status == UpgradeStatus.Owned)
                .Select(u => u.UpgradeData)
                .ToList();
        }

        public List<IUpgrade> GetOwnedUpgrades() {
            return Upgrades.Where(u => u.Status == UpgradeStatus.Owned).ToList();
        }
        
        public List<UpgradeData> GetInProgressUpgrades() {
            return Upgrades.Where(u => u.Status == UpgradeStatus.InProgress)
                .Select(u => u.UpgradeData)
                .ToList();
        }

        public void UpdateUpgradeTimers(float deltaTime) {
            Upgrades.ForEach(u => u.UpdateTimer(deltaTime));
        }
        
        public bool ExpireUpgradeTimer(UpgradeData upgradeData) {
            return GetUpgrade(upgradeData).ExpireUpgradeTimer();
        }

        /// <summary>
        /// Apply any relevant existing upgrades to the given entity. Good to call after the entity spawns.
        /// Server method.
        /// </summary>
        public void ApplyUpgrades(GridEntity entity) {
            foreach (IUpgrade upgrade in Upgrades) {
                if (upgrade.Status != UpgradeStatus.Owned) continue;
                if (!upgrade.UpgradeData.ApplyToGridEntitiesWhenTheySpawn) continue;
                upgrade.ApplyUpgrade(entity);
            }
        }

        /// <summary>
        /// Apply any client-side animations to the given entity for in-progress upgrades.
        /// Client method.
        /// </summary>
        public void ApplyUpgradeAnimations(GridEntity entity) {
            foreach (IUpgrade upgrade in Upgrades) {
                if (upgrade.Status != UpgradeStatus.Owned) continue;
                if (!upgrade.UpgradeData.ApplyToGridEntitiesWhenTheySpawn) continue;
                entity.UpgradeApplied(upgrade);
            }
        }
    }
}