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
        public Dictionary<UpgradeData, bool> UpgradesDict { get; private set; }

        public UpgradesCollection() : this(new Dictionary<UpgradeData, bool>()){ }

        public UpgradesCollection(Dictionary<UpgradeData, bool> dictToCopy) {
            UpgradesDict = new Dictionary<UpgradeData, bool>(dictToCopy);
        }

        public void RegisterUpgrades(List<UpgradeData> upgrades) {
            UpgradesDict = new Dictionary<UpgradeData, bool>();
            foreach (UpgradeData upgrade in upgrades) {
                UpgradesDict.Add(upgrade, false);
            }
        }

        /// <summary>
        /// Add the upgrade. Returns true if this was newly added, otherwise returns false if no-op. 
        /// </summary>
        public bool AddUpgrade(UpgradeData upgrade) {
            if (!UpgradesDict.ContainsKey(upgrade)) {
                throw new Exception($"Tried to add upgrade that is not registered in the collection: {upgrade.ID}");
            }

            if (UpgradesDict[upgrade]) {
                Debug.Log($"Not adding upgrade ({upgrade.ID}) because we already have the upgrade");
                return false;  // We already have the upgrade, so do nothing
            }

            Debug.Log($"Adding upgrade ({upgrade.ID})");
            UpgradesDict[upgrade] = true;   // Add the upgrade
            return true;
        }

        public List<UpgradeData> GetOwnedUpgrades() {
            return UpgradesDict.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
    }
    
    public static class UpgradesCollectionSerializer {
        public static void WriteUpgradesCollection(this NetworkWriter writer, UpgradesCollection collection) {
            writer.Write(collection.UpgradesDict);
        }

        public static UpgradesCollection ReadUpgradesCollection(this NetworkReader reader) {
            return new UpgradesCollection(reader.Read<Dictionary<UpgradeData, bool>>());
        }
    }

}