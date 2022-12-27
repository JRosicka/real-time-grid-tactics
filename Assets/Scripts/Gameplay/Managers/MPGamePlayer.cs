using System.Collections.Generic;
using Gameplay.Config;
using Mirror;

public class MPGamePlayer : NetworkBehaviour, IGamePlayer {
    [field: SyncVar]
    public PlayerData Data { get; set; }
    public string DisplayName { get; set; }

    [field:SyncVar]
    public List<PurchasableData> OwnedPurchasables { get; } = new List<PurchasableData>();

    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }
}
