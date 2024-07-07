using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;

public class SPGamePlayer : MonoBehaviour, IGamePlayer {
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
}