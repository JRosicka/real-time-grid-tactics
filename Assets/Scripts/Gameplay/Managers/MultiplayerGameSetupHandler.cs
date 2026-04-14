using System.Collections.Generic;
using System.Linq;
using Audio;
using Gameplay.Entities;
using Gameplay.Managers;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Object for <see cref="GameSetupManager"/> to delegate any NetworkBehaviour-specific multiplayer initialization. This way, 
/// <see cref="GameSetupManager"/> can be a MonoBehavior instead of a NetworkBehavior, so it can work for SP games too. 
/// </summary>
public class MultiplayerGameSetupHandler : NetworkBehaviour {
    public GameSetupManager GameSetupManager;
    
    // The total number of players in this game, including players who have not yet arrived in the game scene
    [SyncVar]
    public int PlayerCount = -1;

    [SyncVar(hook = nameof(OnGameRunning))] 
    public bool GameRunning;
    private void OnGameRunning(bool previousState, bool currentState) {
        if (currentState) {
            GameSetupManager.TriggerGameRunningEvent();
        }
    }
    
    [Command(requiresAuthority = false)]    // TODO
    public void CmdNotifyPlayerReady(string displayName) {
        GameSetupManager.MarkPlayerReady(displayName);
    }
    
    [ClientRpc]
    public void RpcAssignPlayers() {
        ICommandManager commandManager = FindFirstObjectByType<MPCommandManager>();
        GameManager.Instance.CommandManager = commandManager;
        // Client initialization in case this was not initialized on the server pass
        commandManager.AbilityExecutor.Initialize(commandManager, GameManager.Instance.GameEndManager, GameManager.Instance.AbilityAssignmentManager, false);
        
        List<MPGamePlayer> players = FindObjectsByType<MPGamePlayer>(FindObjectsSortMode.InstanceID).ToList(); 
        MPGamePlayer player1 = players.FirstOrDefault(p => p.Team == GameTeam.Player1);
        MPGamePlayer player2 = players.FirstOrDefault(p => p.Team == GameTeam.Player2);
        MPGamePlayer localPlayer = players.FirstOrDefault(p => p.isLocalPlayer);

        if (localPlayer == null) { 
            Debug.LogError("Could not find local player");
            return;
        }
        GameManager.Instance.SetPlayers(player1, player2, localPlayer, localPlayer.Index);
        
        // Let the server know we are done 
        CmdNotifyPlayerAssigned();
    }
    
    [Command(requiresAuthority = false)]
    public void CmdNotifyPlayerAssigned() {
        GameSetupManager.MarkPlayerAssigned();
    }

    /// <summary>
    /// Client-side countdown call. Does not actually start the game - that is handled later from the server. 
    /// </summary>
    [ClientRpc]
    public void RpcBeginCountdownView() {
        GameSetupManager.StartCountdownTimerView();
    }

    [Command(requiresAuthority = false)]
    public void CmdGameOver(GameTeam winner) {
        GameSetupManager.LeaveGameAsync();
        RpcNotifyGameOver(winner);
    }

    [ClientRpc]
    private void RpcNotifyGameOver(GameTeam winner) {
        GameSetupManager.NotifyGameOver(winner);
    }
}