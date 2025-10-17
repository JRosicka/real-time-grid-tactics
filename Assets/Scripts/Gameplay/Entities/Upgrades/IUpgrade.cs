using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Runtime representation of an upgrade. Uses <see cref="UpgradeData"/> for configuration. 
    /// </summary>
    public interface IUpgrade {
        UpgradeData UpgradeData { get; }
        UpgradeStatus Status { get; }
        
        /// <summary>
        /// Triggered when the upgrade finishes being researched.
        /// Server method.
        /// </summary>
        void UpgradeFinished();
        /// <summary>
        /// Triggered when the upgrade gets removed (due to its effect expiring).
        /// Server method.
        /// </summary>
        void RemoveUpgrade();
        /// <summary>
        /// Applies the upgrade effect to the given friendly GridEntity if relevant.
        /// Server method.
        /// </summary>
        void ApplyUpgrade(GridEntity friendlyEntity);
        
        /// <summary>
        /// Update the status of the upgrade (i.e. just got researched, canceled, etc)
        /// RPC client method.
        /// </summary>
        void UpdateStatus(UpgradeStatus status);
        
        // Not synchronized
        UpgradeDurationTimer UpgradeTimer { get; }
        bool ExpireUpgradeTimer();
        void UpdateTimer(float deltaTime);
        int GetAttackBonus(GridEntity attackingEntity);
        string GetAttackTooltipMessage(GridEntity attackingEntity);
    }
}