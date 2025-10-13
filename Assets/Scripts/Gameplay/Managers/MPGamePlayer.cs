using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Mirror;
using UnityEngine;

/// <summary>
/// An <see cref="IGamePlayer"/> in a MP game that represents one of the actual players (not a spectator)
/// </summary>
public class MPGamePlayer : NetworkBehaviour, IGamePlayer {
    [field: SyncVar]
    public PlayerData Data { get; set; }
    [field: SyncVar]
    public string DisplayName { get; set; }
    [field: SyncVar]
    public int Index { get; set; }
    // Server only
    public bool Connected { get; set; }
    [SerializeField]
    private PlayerResourcesController _resourcesController;
    public PlayerResourcesController ResourcesController => _resourcesController;
    [SerializeField]
    private PlayerOwnedPurchasablesController _ownedPurchasablesController;
    public PlayerOwnedPurchasablesController OwnedPurchasablesController => _ownedPurchasablesController;
    public void Initialize(GameConfiguration gameConfiguration) {
        _ownedPurchasablesController.Initialize(this, gameConfiguration.GetUpgrades());
        _resourcesController.Initialize(gameConfiguration.CurrencyConfiguration);
    }
}
