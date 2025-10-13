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
        private List<IUpgrade> _upgrades;
        private readonly GameTeam _team;
        
        public UpgradesCollection(GameTeam team) {
            _team = team;
            _upgrades = new List<IUpgrade>();
        }
        
        public void RegisterUpgrades(List<UpgradeData> upgradeDatas) {
            _upgrades = new List<IUpgrade>();
            foreach (UpgradeData upgradeData in upgradeDatas) {
                _upgrades.Add(upgradeData.CreateUpgrade(_team));
            }
        }
        
        public IUpgrade GetUpgrade(UpgradeData data) {
            return _upgrades.FirstOrDefault(u => u.Data == data);
        }
        
        public List<UpgradeData> GetOwnedUpgrades() {
            return _upgrades.Where(u => u.Status == UpgradeStatus.Owned)
                .Select(u => u.Data)
                .ToList();
        }
        
        public List<UpgradeData> GetInProgressUpgrades() {
            return _upgrades.Where(u => u.Status == UpgradeStatus.InProgress)
                .Select(u => u.Data)
                .ToList();
        }

        public void UpdateUpgradeTimers(float deltaTime) {
            _upgrades.ForEach(u => u.UpdateTimer(deltaTime));
        }
        
        public bool ExpireUpgradeTimer(UpgradeData upgradeData) {
            return GetUpgrade(upgradeData).ExpireUpgradeTimer();
        }

        /// <summary>
        /// Apply any relevant existing upgrades to the given entity. Good to call after the entity spawns.
        /// Server method.
        /// </summary>
        public void ApplyUpgrades(GridEntity entity) {
            foreach (IUpgrade upgrade in _upgrades) {
                if (upgrade.Status != UpgradeStatus.Owned) continue;
                if (!upgrade.Data.ApplyToGridEntities) continue;
                upgrade.ApplyUpgrade(entity);
            }
        }

        /// <summary>
        /// Apply any client-side animations to the given entity for in-progress upgrades.
        /// Client method.
        /// </summary>
        public void ApplyUpgradeAnimations(GridEntity entity) {
            foreach (IUpgrade upgrade in _upgrades) {
                if (upgrade.Status != UpgradeStatus.Owned) continue;
                if (!upgrade.Data.ApplyToGridEntities) continue;
                entity.UpgradeApplied(upgrade);
            }
        }
    }
}