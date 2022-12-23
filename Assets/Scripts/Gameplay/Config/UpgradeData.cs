using Gameplay.Entities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Configuration for a <see cref="GridUpgrade"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/UpgradeData", fileName = "UpgradeData", order = 0)]
    public class UpgradeData : PurchasableData {
        
    }
    
    public static class UpgradeDataSerializer {
        public static void WriteUpgradeData(this NetworkWriter writer, UpgradeData data) {
            writer.WriteString(data.name);
        }

        public static UpgradeData ReadUpgradeData(this NetworkReader reader) {
            return Resources.Load<UpgradeData>(reader.ReadString());
        }
    }
}