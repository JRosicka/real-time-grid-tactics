using System.Collections.Generic;
using System.Linq;
using Mirror;

/// <summary>
/// Object for <see cref="GameSetupManager"/> to delegate any NetworkBehaviour-specific multiplayer initialization. This way, 
/// <see cref="GameSetupManager"/> can be a MonoBehavior instead of a NetworkBehavior, so it can work for SP games too. 
/// </summary>
public class MultiplayerGameSetupHandler : NetworkBehaviour {
    public GameSetupManager GameSetupManager;
    
    // The total number of players in this game, including players who have not yet arrived in the game scene
    [SyncVar]
    public int PlayerCount = -1;

    [SyncVar(hook = nameof(OnGameInitialized))] 
    public bool GameInitialized;
    private void OnGameInitialized(bool previousState, bool currentState) {
        if (currentState) {
            GameSetupManager.TriggerGameInitializedEvent();
        }
    }
    
    [Command(requiresAuthority = false)]    // TODO
    public void CmdNotifyPlayerReady(string displayName) {
        GameSetupManager.MarkPlayerReady(displayName);
    }
    
    [ClientRpc]
    public void RpcAssignPlayers() {
        List<MPGamePlayer> players = FindObjectsOfType<MPGamePlayer>().ToList();
        MPGamePlayer localPlayer = players.FirstOrDefault(p => p.isLocalPlayer);
        MPGamePlayer opponentPlayer = players.FirstOrDefault(p => !p.isLocalPlayer);
        ICommandManager commandManager = FindObjectOfType<MPCommandManager>();
        
        GameManager.Instance.CommandManager = commandManager;
        GameManager.Instance.SetPlayers(localPlayer, opponentPlayer);
    }
}