using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Upgrades;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents a set of <see cref="UpgradeData"/> objects and tracks their ownership states. Upgrade-equivalent of
    /// <see cref="GridEntityCollection"/>, but on a per-player basis and doesn't care about locations. Also only cares about
    /// data objects, not business logic (business logic is tracked here). 
    /// </summary>
    [Serializable]
    public class UpgradesCollection {
        public List<IUpgrade> Upgrades { get; private set; }
        private IUpgrade GetUpgrade(UpgradeData data) {
            return Upgrades.FirstOrDefault(u => u.Data == data);
        }

        public UpgradesCollection() : this(new List<IUpgrade>()){ }

        public UpgradesCollection(List<IUpgrade> upgradesToCopy) {
            Upgrades = new List<IUpgrade>(upgradesToCopy);
        }

        public void RegisterUpgrades(List<UpgradeData> upgradeDatas) {
            Upgrades = new List<IUpgrade>();
            foreach (UpgradeData upgradeData in upgradeDatas) {
                Upgrades.Add(upgradeData.CreateUpgrade());
            }
        }

        /// <summary>
        /// Add the upgrade. Returns true if this was newly added, otherwise returns false if no-op. 
        /// </summary>
        public bool AddUpgrade(UpgradeData upgradeData) {
            IUpgrade upgrade = GetUpgrade(upgradeData);
            if (upgrade == null) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgradeData.ID}");
            }

            if (upgrade.Status == UpgradeStatus.Owned) {
                return false;  // We already have the upgrade, so do nothing
            }

            Debug.Log($"Adding upgrade ({upgradeData.ID})");
            upgrade.Status = UpgradeStatus.Owned;   // Add the upgrade
            return true;
        }

        /// <summary>
        /// Mark the upgrade as in-progress. Returns true if this was newly added, otherwise returns false if no-op. 
        /// </summary>
        public bool AddInProgressUpgrade(UpgradeData upgradeData) {
            IUpgrade upgrade = GetUpgrade(upgradeData);
            if (upgrade == null) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgradeData.ID}");
            }

            if (upgrade.Status == UpgradeStatus.InProgress) {
                return false;  // We already have the upgrade marked as such, so do nothing
            }

            Debug.Log($"Marking upgrade as in-progress ({upgradeData.ID})");
            upgrade.Status = UpgradeStatus.InProgress;   // Add the upgrade
            return true;
        }

        /// <summary>
        /// Mark the in-progress upgrade as not in progress. Returns true if this was changed, otherwise returns false if no-op. 
        /// </summary>
        public bool CancelInProgressUpgrade(UpgradeData upgradeData) {
            IUpgrade upgrade = GetUpgrade(upgradeData);
            if (upgrade == null) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgradeData.ID}");
            }

            if (upgrade.Status != UpgradeStatus.InProgress) {
                return false;  // Unexpected state, so do nothing
            }

            Debug.Log($"Marking upgrade as not-in-progress ({upgradeData.ID})");
            upgrade.Status = UpgradeStatus.NeitherOwnedNorInProgress;   // Add the upgrade
            return true;
        }
        
        public List<UpgradeData> GetOwnedUpgrades() {
            return Upgrades.Where(u => u.Status == UpgradeStatus.Owned)
                .Select(u => u.Data)
                .ToList();
        }

        public List<UpgradeData> GetInProgressUpgrades() {
            return Upgrades.Where(u => u.Status == UpgradeStatus.InProgress)
                .Select(u => u.Data)
                .ToList();
        }
    }
    
    public static class UpgradesCollectionSerializer {
        public static void WriteUpgradesCollection(this NetworkWriter writer, UpgradesCollection collection) {
            writer.Write(collection.Upgrades);
        }

        public static UpgradesCollection ReadUpgradesCollection(this NetworkReader reader) {
            return new UpgradesCollection(reader.Read<List<IUpgrade>>());
        }
    }
    
    public static class UpgradeListSerializer {
        public static void WriteUpgradeList(this NetworkWriter writer, List<IUpgrade> list) {
            writer.Write(list.Count);
            foreach (IUpgrade upgrade in list) {
                writer.Write(upgrade);
            }
        }

        public static List<IUpgrade> ReadUpgradeDict(this NetworkReader reader) {
            List<IUpgrade> ret = new List<IUpgrade>();
            int collectionSize = reader.ReadInt();
            for (int i = 0; i < collectionSize; i++) {
                ret.Add(reader.Read<IUpgrade>());
            }

            return ret;
        }
    }
}