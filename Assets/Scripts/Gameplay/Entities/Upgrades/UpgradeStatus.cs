namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Status of an <see cref="IUpgrade"/>
    /// </summary>
    public enum UpgradeStatus {
        NeitherOwnedNorInProgress = 0,
        InProgress = 1,
        Owned = 2
    }
}