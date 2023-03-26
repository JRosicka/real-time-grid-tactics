using System.Collections.Generic;
using Gameplay.Config;

/// <summary>
/// Represents a player and its associated data and overall state
/// </summary>
public interface IGamePlayer {
    PlayerData Data { get; set; }
    string DisplayName { get; set; }
    PlayerResourcesController ResourcesController { get; }
    /// <summary>
    /// Complete set of all <see cref="PurchasableData"/> (upgrades, units, structures) that are owned by the player and
    /// currently active in the game (not destroyed).
    /// </summary>
    PlayerOwnedPurchasablesController OwnedPurchasablesController { get; }    // TODO actually handle adding/removing things from this
    void Initialize(List<UpgradeData> upgradesToRegister);
}
