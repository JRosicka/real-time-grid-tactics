using Gameplay.Entities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for an upgrade, which is sort of like a <see cref="GridEntity"/> but does not have a location on
    /// the grid. Upgrades are only tracked in this data class - see <see cref="PlayerOwnedPurchasablesController"/> for
    /// keeping track of what upgrades are owned during a game. 
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/UpgradeData", fileName = "UpgradeData", order = 0)]
    public class UpgradeData : PurchasableData {
        
    }
    
    public static class UpgradeDataSerializer {
        public static void WriteUpgradeData(this NetworkWriter writer, UpgradeData data) {
            writer.WriteString(data.name);
        }

        public static UpgradeData ReadUpgradeData(this NetworkReader reader) {
            return (UpgradeData) GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
        }
    }
}