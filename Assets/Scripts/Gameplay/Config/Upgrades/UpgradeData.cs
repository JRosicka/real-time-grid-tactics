using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using Mirror;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    /// <summary>
    /// Configuration for an upgrade, which is sort of like a <see cref="GridEntity"/> but does not have a location on
    /// the grid. Upgrades are only tracked in this data class - see <see cref="UpgradesCollection"/> for
    /// keeping track of what upgrades are owned during a game.
    ///
    /// See <see cref="IUpgrade"/> for runtime logic backed by this configuration.
    /// </summary>
    public abstract class UpgradeData : PurchasableData {
        [Tooltip("Whether this upgrade can be purchased again after being removed")]
        public bool Repeatable;
        [Tooltip("Whether this upgrade has an effect that gets applied to certain GridEntities - at the time of completion of the upgrade")]
        public bool ApplyToGridEntitiesUponCompletion;
        [Tooltip("Whether this upgrade has an effect that gets applied to certain GridEntities - when an entity spawns while this upgrade is completed")]
        public bool ApplyToGridEntitiesWhenTheySpawn;
        [Tooltip("How long the upgrade effect lasts once researched. A value of 0 indicates that this is un-timed.")]
        [Range(0, 60)]
        public int ExpirationSeconds;
        public bool Timed => ExpirationSeconds > 0;
        public abstract IUpgrade CreateUpgrade(GameTeam team);
    }
    
    public static class UpgradeDataSerializer {
        public static void WriteUpgradeData(this NetworkWriter writer, UpgradeData data) {
            writer.WriteString(data.ID);
        }

        public static UpgradeData ReadUpgradeData(this NetworkReader reader) {
            return (UpgradeData) GameManager.Instance.Configuration.GetPurchasable(reader.ReadString());
        }
    }
}