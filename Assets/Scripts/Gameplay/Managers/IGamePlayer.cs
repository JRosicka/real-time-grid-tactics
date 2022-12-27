using System.Collections.Generic;
using Gameplay.Config;

/// <summary>
/// Represents a player and its associated data and overall state
/// </summary>
public interface IGamePlayer {
    PlayerData Data { get; set; }
    string DisplayName { get; set; }
    /// <summary>
    /// Complete set of all <see cref="PurchasableData"/> (upgrades, units, structures) that are owned by the player and
    /// currently active in the game (not destroyed).
    /// </summary>
    List<PurchasableData> OwnedPurchasables { get; }    // TODO actually handle adding/removing things from this
}
