using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
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
        public enum UpgradeStatus {
            NeitherOwnedNorInProgress = 0,
            InProgress = 1,
            Owned = 2
        }
        public Dictionary<UpgradeData, UpgradeStatus> UpgradesDict { get; private set; }

        public UpgradesCollection() : this(new Dictionary<UpgradeData, UpgradeStatus>()){ }

        public UpgradesCollection(Dictionary<UpgradeData, UpgradeStatus> dictToCopy) {
            UpgradesDict = new Dictionary<UpgradeData, UpgradeStatus>(dictToCopy);
        }

        public void RegisterUpgrades(List<UpgradeData> upgrades) {
            UpgradesDict = new Dictionary<UpgradeData, UpgradeStatus>();
            foreach (UpgradeData upgrade in upgrades) {
                UpgradesDict.Add(upgrade, UpgradeStatus.NeitherOwnedNorInProgress);
            }
        }

        /// <summary>
        /// Add the upgrade. Returns true if this was newly added, otherwise returns false if no-op. 
        /// </summary>
        public bool AddUpgrade(UpgradeData upgrade) {
            if (!UpgradesDict.ContainsKey(upgrade)) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgrade.ID}");
            }

            if (UpgradesDict[upgrade] == UpgradeStatus.Owned) {
                return false;  // We already have the upgrade, so do nothing
            }

            Debug.Log($"Adding upgrade ({upgrade.ID})");
            UpgradesDict[upgrade] = UpgradeStatus.Owned;   // Add the upgrade
            return true;
        }

        /// <summary>
        /// Mark the upgrade as in-progress. Returns true if this was newly added, otherwise returns false if no-op. 
        /// </summary>
        public bool AddInProgressUpgrade(UpgradeData upgrade) {
            if (!UpgradesDict.ContainsKey(upgrade)) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgrade.ID}");
            }

            if (UpgradesDict[upgrade] == UpgradeStatus.InProgress) {
                return false;  // We already have the upgrade marked as such, so do nothing
            }

            Debug.Log($"Marking upgrade as in-progress ({upgrade.ID})");
            UpgradesDict[upgrade] = UpgradeStatus.InProgress;   // Add the upgrade
            return true;
        }

        /// <summary>
        /// Mark the in-progress upgrade as not in progress. Returns true if this was changed, otherwise returns false if no-op. 
        /// </summary>
        public bool CancelInProgressUpgrade(UpgradeData upgrade) {
            if (!UpgradesDict.ContainsKey(upgrade)) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgrade.ID}");
            }

            if (UpgradesDict[upgrade] != UpgradeStatus.InProgress) {
                return false;  // Unexpected state, so do nothing
            }

            Debug.Log($"Marking upgrade as not-in-progress ({upgrade.ID})");
            UpgradesDict[upgrade] = UpgradeStatus.NeitherOwnedNorInProgress;   // Add the upgrade
            return true;
        }
        
        public List<UpgradeData> GetOwnedUpgrades() {
            return UpgradesDict.Where(kvp => kvp.Value == UpgradeStatus.Owned)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public List<UpgradeData> GetInProgressUpgrades() {
            return UpgradesDict.Where(kvp => kvp.Value == UpgradeStatus.InProgress)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
    
    public static class UpgradesCollectionSerializer {
        public static void WriteUpgradesCollection(this NetworkWriter writer, UpgradesCollection collection) {
            writer.Write(collection.UpgradesDict);
        }

        public static UpgradesCollection ReadUpgradesCollection(this NetworkReader reader) {
            return new UpgradesCollection(reader.Read<Dictionary<UpgradeData, UpgradesCollection.UpgradeStatus>>());
        }
    }
    
    public static class UpgradeDictSerializer {
        public static void WriteUpgradeDict(this NetworkWriter writer, Dictionary<UpgradeData, UpgradesCollection.UpgradeStatus> dict) {
            writer.Write(dict.Count);
            foreach ((UpgradeData key, UpgradesCollection.UpgradeStatus value) in dict) {
                writer.Write(key);
                writer.Write(value);
            }
        }

        public static Dictionary<UpgradeData, UpgradesCollection.UpgradeStatus> ReadUpgradeDict(this NetworkReader reader) {
            Dictionary<UpgradeData, UpgradesCollection.UpgradeStatus> ret = new Dictionary<UpgradeData, UpgradesCollection.UpgradeStatus>();
            int collectionSize = reader.ReadInt();
            for (int i = 0; i < collectionSize; i++) {
                ret[reader.Read<UpgradeData>()] = reader.Read<UpgradesCollection.UpgradeStatus>();
            }

            return ret;
        }
    }


}