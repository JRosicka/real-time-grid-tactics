using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using Mirror;

namespace Gameplay.Config.Upgrades {
    /// <summary>
    /// Configuration for an upgrade, which is sort of like a <see cref="GridEntity"/> but does not have a location on
    /// the grid. Upgrades are only tracked in this data class - see <see cref="UpgradesCollection"/> for
    /// keeping track of what upgrades are owned during a game.
    ///
    /// See <see cref="IUpgrade"/> for runtime logic backed by this configuration.
    /// </summary>
    public abstract class UpgradeData : PurchasableData {
        public abstract IUpgrade CreateUpgrade();
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