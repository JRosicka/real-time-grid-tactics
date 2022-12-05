using System.Collections.Generic;
using System.Linq;
using Mirror;

/// <summary>
/// Object for <see cref="GameManager"/> to delegate any multiplayer initialization. This way, <see cref="GameManager"/>
/// can be a MonoBehavior instead of a NetworkBehavior, so it can work for SP games too. 
/// </summary>
public class MultiplayerGameSetupHandler : NetworkBehaviour {
    [Command]
    public void CmdNotifyPlayerReady(string displayName) {
        GameManager.Instance.MarkPlayerReady(displayName);
    }
    
    [ClientRpc]
    public void RpcAssignPlayers() {
        List<MPGamePlayer> players = FindObjectsOfType<MPGamePlayer>().ToList();
        MPGamePlayer localPlayer = players.FirstOrDefault(p => p.isLocalPlayer);
        MPGamePlayer opponentPlayer = players.FirstOrDefault(p => !p.isLocalPlayer);
        ICommandController commandController = FindObjectOfType<MPCommandController>();
        
        GameManager.Instance.SetPlayers(localPlayer, opponentPlayer, commandController);
    }
}