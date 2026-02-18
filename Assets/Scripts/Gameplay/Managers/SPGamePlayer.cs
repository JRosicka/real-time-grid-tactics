using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

public class SPGamePlayer : MonoBehaviour, IGamePlayer {
    public GameTeam Team { get; set; }
    public PlayerColorData ColorData { get; set; }
    public string DisplayName { get; set; }
    public int Index { get; set; }
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