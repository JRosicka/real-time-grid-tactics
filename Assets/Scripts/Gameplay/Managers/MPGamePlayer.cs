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
    [field:SyncVar]
    public List<PurchasableData> OwnedPurchasables { get; } = new List<PurchasableData>();

    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }
}
