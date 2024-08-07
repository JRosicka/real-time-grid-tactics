using System.Collections.Generic;
using Gameplay.Config;
using Mirror;
using UnityEngine;

public class MPGamePlayer : NetworkBehaviour, IGamePlayer {
    [field: SyncVar]
    public PlayerData Data { get; set; }
    public string DisplayName { get; set; }
    [SerializeField]
    private PlayerResourcesController _resourcesController;
    public PlayerResourcesController ResourcesController => _resourcesController;
    [SerializeField]
    private PlayerOwnedPurchasablesController _ownedPurchasablesController;
    public PlayerOwnedPurchasablesController OwnedPurchasablesController => _ownedPurchasablesController;
    public void Initialize(List<UpgradeData> upgradesToRegister, GameConfiguration gameConfiguration) {
        _ownedPurchasablesController.Initialize(Data.Team, upgradesToRegister);
        _resourcesController.Initialize(gameConfiguration);
    }

    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }
}
