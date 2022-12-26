using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Network;
using Mirror;
using UnityEngine;

/// <summary>
/// Handles setup for a game - figuring out if it's SP or MP, waiting for all players to connect and be ready, etc
/// </summary>
public class GameSetupManager : MonoBehaviour {
    public MultiplayerGameSetupHandler MPSetupHandler;

    [Header("Prefabs")]
    public SPGamePlayer SPGamePlayerPrefab;
    public MPCommandController MPCommandControllerPrefab;
    public SPCommandController SPCommandControllerPrefab;

    [Header("Data")]
    public PlayerData Player1Data;
    public PlayerData Player2Data;
    
    private static GameManager GameManager => GameManager.Instance;
    
    // The total number of players in this game who have arrived in the game scene
    private int _readyPlayerCount;
    // Whether the game was initialized on the server
    private bool _gameInitialized;
    
    public void Initialize() {
        // If we are not connected in a multiplayer session, then we must be playing singleplayer. Set up the game now. 
        // Otherwise, wait for the network manager to set up the multiplayer game. 
        if (!NetworkClient.active) {
            SetupSPGame();
        } else if (!NetworkServer.active) {
            // This is a client and not the host. Listen for all player objects getting created so that we can tell the server that we are ready.
            StartCoroutine(ListenForPlayersConnected());
        }
    }
    
    #region Singleplayer

    private void SetupSPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        ICommandController commandController = Instantiate(SPCommandControllerPrefab, transform);
        GameManager.SetupCommandController(commandController);

        SPGamePlayer localPlayer = Instantiate(SPGamePlayerPrefab);
        localPlayer.Data = Player1Data;
        SPGamePlayer opponentPlayer = Instantiate(SPGamePlayerPrefab);
        opponentPlayer.Data = Player2Data;
        GameManager.SetPlayers(localPlayer, opponentPlayer);
        
        _gameInitialized = true;
    }

    #endregion
    
    #region Multiplayer
    
    /// <summary>
    /// Check to see if all players have connected for this client. If they have, notify the server that we are ready.
    /// If not, then check again later. 
    /// </summary>
    [Client]
    private IEnumerator ListenForPlayersConnected() {
        if (MPSetupHandler.PlayerCount < 0) {
            // The server does not yet know the player count. Try again in a bit
            yield return new WaitForSeconds(.1f);
            StartCoroutine(ListenForPlayersConnected());
            yield break;
        }
        
        List<MPGamePlayer> players = FindObjectsOfType<MPGamePlayer>().ToList();
        if (players.Count == MPSetupHandler.PlayerCount) {
            MPSetupHandler.CmdNotifyPlayerReady(players.First(p => p.isLocalPlayer).DisplayName);
        } else if (players.Count > MPSetupHandler.PlayerCount) {
            throw new Exception($"Detected more player objects than the recorded player count! Expected: {MPSetupHandler.PlayerCount}. Actual: {players.Count}");
        } else {
            // Try again in a bit
            yield return new WaitForSeconds(.1f);
            StartCoroutine(ListenForPlayersConnected());
        }
    }

    /// <summary>
    /// We just detected that a player has arrived in the game scene! Perform setup. 
    /// </summary>
    [Server]
    public void SetupMPPlayer(GameNetworkPlayer networkPlayer, MPGamePlayer gamePlayer, int playerCount) {
        if ((MPGamePlayer) GameManager.LocalPlayer == gamePlayer || (MPGamePlayer) GameManager.OpponentPlayer == gamePlayer) {
            Debug.LogError($"Game scene loaded for player {networkPlayer.DisplayName}, but we already detected the game scene loading for them.");
            return;
        }
        if (MPSetupHandler.PlayerCount > -1 && playerCount != MPSetupHandler.PlayerCount) {
            Debug.LogError($"{nameof(playerCount)} ({playerCount}) is a different value than what we previously recorded ({MPSetupHandler.PlayerCount})!");
            return;
        }

        MPSetupHandler.PlayerCount = playerCount;

        gamePlayer.Data = networkPlayer.index switch {
            0 => Player1Data,
            1 => Player2Data,
            _ => throw new IndexOutOfRangeException(
                $"Tried to set up network player with invalid index ({networkPlayer.index})")
        };

        gamePlayer.DisplayName = networkPlayer.DisplayName;

        if (networkPlayer.isLocalPlayer) {
            // This is the server's local player. Since no other clients will notify us that this player joined, we should do it here
            MarkPlayerReady(networkPlayer.DisplayName + " (host)");
        }
    }

    /// <summary>
    /// A player has finished loading into the game scene
    /// </summary>
    [Server]
    public void MarkPlayerReady(string displayName) {
        Debug.Log($"Player ({displayName}) connected");
        _readyPlayerCount++;

        // Set up the game once all players have arrived
        if (MPSetupHandler.PlayerCount == _readyPlayerCount) {
            SetupMPGame();
        }
    }
    
    /// <summary>
    /// To be called when all players are ready. Perform command controller setup and send that info to the players.
    /// </summary>
    [Server]
    private void SetupMPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up MP game, the game was already set up");
            return;
        }
        
        // Set up the command controller - only done on the server
        MPCommandController newController = Instantiate(MPCommandControllerPrefab, transform);
        NetworkServer.Spawn(newController.gameObject);
        GameManager.SetupCommandController(newController);

        // Tell clients to look for the command controller and player GameObjects
        MPSetupHandler.RpcAssignPlayers();
        _gameInitialized = true;
    }

    #endregion
    
}