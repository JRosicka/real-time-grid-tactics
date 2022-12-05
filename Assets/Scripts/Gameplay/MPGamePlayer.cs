using Mirror;

public class MPGamePlayer : NetworkBehaviour, IGamePlayer {
    [field: SyncVar]
    public PlayerData Data { get; set; }
    public string DisplayName { get; set; }

    public override void OnStartLocalPlayer() { }
    public override void OnStopLocalPlayer() { }
}
