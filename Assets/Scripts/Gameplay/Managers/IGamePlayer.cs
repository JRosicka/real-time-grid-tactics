using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Upgrades;

/// <summary>
/// Represents a player and its associated data and overall state
/// </summary>
public interface IGamePlayer {
    PlayerData Data { get; }
    string DisplayName { get; set; }
    public int Index { get; set; }
    PlayerResourcesController ResourcesController { get; }
    /// <summary>
    /// Complete set of all <see cref="PurchasableData"/> (upgrades, units, structures) that are owned by the player and
    /// currently active in the game (not destroyed).
    /// </summary>
    PlayerOwnedPurchasablesController OwnedPurchasablesController { get; }    // TODO actually handle adding/removing things from this
    /// <summary>
    /// Called on the client
    /// </summary>
    void Initialize(List<UpgradeData> upgradesToRegister, GameConfiguration gameConfiguration);
}
