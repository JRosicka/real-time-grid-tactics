using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Network;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.UI;
using Mirror;
using UnityEngine;

/// <summary>
/// Handles setup for a game - figuring out if it's SP or MP, waiting for all players to connect and be ready, and
/// controlling the flow of game-start logic in the case of MP games.
/// </summary>
public class GameSetupManager : MonoBehaviour {
    public MultiplayerGameSetupHandler MPSetupHandler;
    public MapLoader MapLoader;
    public CountdownTimerView CountdownTimer;
    public GameOverView GameOverView;
    public InGamePauseMenu _pauseMenu;

    [Header("Prefabs")]
    public SPGamePlayer SPGamePlayerPrefab;
    public MPCommandManager MPCommandManagerPrefab;
    public SPCommandManager SPCommandManagerPrefab;

    [Header("Data")]
    public PlayerData Player1Data;
    public PlayerData Player2Data;
    public float CountdownTimeSeconds = 3f;
    public int GameOverDelayMillis = 5 * 1000;
    
    private static GameManager GameManager => GameManager.Instance;
    
    // The total number of players in this game who have arrived in the game scene
    private int _readyPlayerCount;
    // The total number of players in this game who have had their gameobjects assigned
    private int _assignedPlayerCount;
    // Whether the game was initialized on the server
    private bool _gameInitialized;
    public bool GameInitialized {
        get {
            if (NetworkClient.active) {
                // MP
                return MPSetupHandler.GameInitialized;
            }
            return _gameInitialized;
        }
        private set {
            _gameInitialized = value;
            if (NetworkServer.active) {
                MPSetupHandler.GameInitialized = value;
            } else if (value) {
                TriggerGameInitializedEvent();
            }
        }
    }
    
    public bool InputAllowed => !_pauseMenu.Paused && GameInitialized && !GameOver;

    public event Action GameInitializedEvent;
    public void TriggerGameInitializedEvent() {
        GameInitializedEvent?.Invoke();
    }
    
    public bool GameOver { get; private set; }

    public void Initialize() {
        // If we are not connected in a multiplayer session, then we must be playing singleplayer. Set up the game now. 
        // Otherwise, wait for the network manager to set up the multiplayer game. 
        if (!NetworkClient.active) {
            SetupSPGame();
        } else {
            CountdownTimer.ShowLoadingStatus();
            if (!NetworkServer.active) {
                // This is a client and not the host. Listen for all player objects getting created so that we can tell the server that we are ready.
                StartCoroutine(ListenForPlayersConnected());
            }
        }
        
        GameManager.GameEndManager.GameEnded += HandleGameOver;
    }
    
    // TODO it would be good to move all of this game over logic to GameEndManager, but we currently can't do server-side logic in that class
    private void HandleGameOver(IGamePlayer winner) {
        GridEntity.Team winningTeam = winner == null ? GridEntity.Team.Neutral : winner.Data.Team;
        if (!NetworkClient.active) {
            ReturnToLobbyAsync();
            NotifyGameOver(winningTeam);
        } else {
            MPSetupHandler.CmdGameOver(winningTeam);
        }
    }
    
    /// <summary>
    /// Server/SP logic for ending the game. Halts commands and returns to lobby shortly. (SP will just reload the game scene)
    /// </summary>
    public async void ReturnToLobbyAsync() {
        await Task.Delay(GameOverDelayMillis);
        GameManager.ReturnToLobby();
    }

    /// <summary>
    /// Client/SP logic for ending the game
    /// </summary>
    /// <param name="winner"></param>
    public void NotifyGameOver(GridEntity.Team winner) {
        GameOver = true;
        if (winner == GridEntity.Team.Neutral) {
            GameOverView.ShowTie();
        } else if (winner == GameManager.LocalPlayer.Data.Team) {
            GameOverView.ShowVictory();
        } else {
            GameOverView.ShowDefeat();
        }
    }

    private void SpawnStartingUnits() {
        SpawnPlayerStartingUnits(Player1Data);
        SpawnPlayerStartingUnits(Player2Data);
    }

    private void SpawnPlayerStartingUnits(PlayerData player) {
        MapLoader.StartingEntitySet entitySet = MapLoader.UnitSpawns.First(s => s.Team == player.Team);
        foreach (MapLoader.EntitySpawn entity in entitySet.Entities) {
            GameManager.CommandManager.SpawnEntity(entity.Entity, entity.SpawnLocation, player.Team, null);
        }
    }

    /// <summary>
    /// Start the UI for counting down. Does not actually start the game when finished counting down. 
    /// </summary>
    public void StartCountdownTimerView() {
        CountdownTimer.StartCountdown(CountdownTimeSeconds);
    }

    /// <summary>
    /// Actual timer (separate from <see cref="CountdownTimerView"/>) for starting the game
    /// </summary>
    private async void PerformGameStartCountdown() {
        await Task.Delay((int)(CountdownTimeSeconds * 1000));
        
        // The game is actually starting now, so perform on-start abilities for the starting set of entities
        GameManager.CommandManager.EntitiesOnGrid.AllEntities().ForEach(e => e.PerformOnStartAbilities());
        GameInitialized = true;
    }
    
    #region Singleplayer

    private void SetupSPGame() {
        if (GameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        ICommandManager commandManager = Instantiate(SPCommandManagerPrefab, transform);
        GameManager.SetupCommandManager(commandManager);
        
        SPGamePlayer localPlayer = Instantiate(SPGamePlayerPrefab);
        localPlayer.Data = Player1Data;
        SPGamePlayer opponentPlayer = Instantiate(SPGamePlayerPrefab);
        opponentPlayer.Data = Player2Data;
        GameManager.SetPlayers(localPlayer, opponentPlayer);
        
        MapLoader.LoadMap(localPlayer.Data.Team);
        SpawnStartingUnits();

        GameManager.CommandManager.EntitiesOnGrid.AllEntities().ForEach(e => e.PerformOnStartAbilities());

        GameInitialized = true;
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
            // Load the map client-side first, then notify the server that the client setup is finished
            MapLoader.LoadMap(players.First(p => p.isLocalPlayer).Data.Team);
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

        PlayerData data = networkPlayer.index switch {
            0 => Player1Data,
            1 => Player2Data,
            _ => throw new IndexOutOfRangeException(
                $"Tried to set up network player with invalid index ({networkPlayer.index})")
        };

        gamePlayer.Data = data;
        gamePlayer.DisplayName = networkPlayer.DisplayName;

        if (networkPlayer.isLocalPlayer) {
            // This is the server's local player. Since no other clients will notify us that this player joined, we should do it here
            MapLoader.LoadMap(gamePlayer.Data.Team); 
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
        if (GameInitialized) {
            Debug.LogError("Can not set up MP game, the game was already set up");
            return;
        }
        
        // Set up the command controller - only done on the server
        MPCommandManager newManager = Instantiate(MPCommandManagerPrefab, transform);
        NetworkServer.Spawn(newManager.gameObject);
        GameManager.SetupCommandManager(newManager);

        // Tell clients to look for the command controller and player GameObjects
        MPSetupHandler.RpcAssignPlayers();
    }

    /// <summary>
    /// A player has finished the assign step
    /// </summary>
    [Server]
    public void MarkPlayerAssigned() {
        _assignedPlayerCount++;

        // Proceed with game setup once all players have been assigned
        if (MPSetupHandler.PlayerCount == _assignedPlayerCount) {
            PerformFinalGameSetupSteps();
        }
    }

    [Server]
    private void PerformFinalGameSetupSteps() {
        // Now that both players are set up, spawn units for both sides
        SpawnStartingUnits();

        PerformGameStartCountdown();
        MPSetupHandler.RpcBeginCountdownView();
    }

    #endregion
    
}