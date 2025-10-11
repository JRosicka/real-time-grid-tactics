using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Mirror;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Runtime representation of an upgrade. Uses <see cref="UpgradeData"/> for configuration. 
    /// </summary>
    public interface IUpgrade {
        UpgradeData Data { get; }
        UpgradeStatus Status { get; set; }
        
        /// <summary>
        /// Triggered when the upgrade finishes being researched
        /// </summary>
        void UpgradeFinished();

        /// <summary>
        /// Triggered when the upgrade gets removed (due to its effect expiring)
        /// </summary>
        void RemoveUpgrade();

        /// <summary>
        /// Applies an upgrade effect to the given GridEntity if relevant
        /// </summary>
        void ApplyUpgrade(GridEntity entity);
    }
    
    public static class UpgradeSerializer {
        public static void WriteUpgrade(this NetworkWriter writer, IUpgrade upgrade) {
            writer.WriteString(upgrade.Data.ID);
            writer.WriteInt((int)upgrade.Status);
            // upgrade.SerializeParameters(writer); // TODO add this if any implementations record additional state
        }

        public static IUpgrade ReadUpgrade(this NetworkReader reader) {
            UpgradeData upgradeData = (UpgradeData)GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
            UpgradeStatus status = (UpgradeStatus)reader.ReadInt();
            
            // Re-create the ability instance using the data asset we loaded
            IUpgrade upgrade = upgradeData.CreateUpgrade();
            upgrade.Status = status;
            return upgrade;
            // IAbility abilityInstance = dataAsset.Content.DeserializeAbility(reader);
        }
    }
}