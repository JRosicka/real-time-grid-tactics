using System.Collections.Generic;
using Gameplay.Config;
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
    public int Index { get; set; }
    public bool Connected { get; set; }
    [SerializeField]
    private PlayerResourcesController _resourcesController;
    public PlayerResourcesController ResourcesController => _resourcesController;
    [SerializeField]
    private PlayerOwnedPurchasablesController _ownedPurchasablesController;
    public PlayerOwnedPurchasablesController OwnedPurchasablesController => _ownedPurchasablesController;
    public void Initialize(List<UpgradeData> upgradesToRegister, GameConfiguration gameConfiguration) {
        _ownedPurchasablesController.Initialize(Data.Team, upgradesToRegister);
        _resourcesController.Initialize(gameConfiguration.CurrencyConfiguration);
    }
}
