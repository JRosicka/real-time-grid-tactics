using System;
using Game.Network;
using Mirror;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("References")] 
    public GridController GridController;
    public MultiplayerGameSetupHandler MPSetupHandler;
    public Transform SpawnBucket; 
    public ICommandController CommandController;
    public PlayerData Player1Data;
    public PlayerData Player2Data;
    
    [Header("Prefabs")]
    public SPGamePlayer SPGamePlayerPrefab;
    public MPCommandController MPCommandControllerPrefab;
    public SPCommandController SPCommandControllerPrefab;
    
    private IGamePlayer _localPlayer;
    private IGamePlayer _opponentPlayer;
    
    // The total number of players in this game, including players who have not yet arrived in the game scene
    private int _playerCount = -1;
    // The total number of players in this game who have arrived in the game scene
    private int _readyPlayerCount;
    private bool _gameInitialized;
    
    [Header("Unit 1")] 
    public bool Unit1_LocalTeam;
    public GridUnit Unit1;
    public void SpawnUnit1() {
        IGamePlayer player = Unit1_LocalTeam ? _localPlayer : _opponentPlayer;
        CommandController.SpawnEntity(1, player.Data.SpawnLocation, player.Data.Team);
    }

    [Header("Unit 2")] 
    public bool Unit2_LocalTeam;
    public GridUnit Unit2;
    public void SpawnUnit2() {
        IGamePlayer player = Unit2_LocalTeam ? _localPlayer : _opponentPlayer;
        CommandController.SpawnEntity(2, player.Data.SpawnLocation, player.Data.Team);
    }

    [HideInInspector]
    public GridEntity SelectedEntity;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("GameManager instance is not null!!");
        }
        
        Instance = this;
    }

    private void Start() {
        // If we are not connected in a multiplayer session, then we must be playing singleplayer. Set up the game now. 
        // Otherwise, wait for the network manager to set up the multiplayer game. 
        if (!NetworkClient.active) {
            SetupSPGame();
        }
    }
    
    [Server]
    private void SetupMPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        // Make sure that the command controller only gets set up once
        SetupCommandController(true);
        MPSetupHandler.RpcDetectPlayers();
        _gameInitialized = true;
    }
    
    [Server]
    public void SetupMPPlayer(GameNetworkPlayer networkPlayer, MPGamePlayer gamePlayer, int playerCount) {
        if ((MPGamePlayer) _localPlayer == gamePlayer || (MPGamePlayer) _opponentPlayer == gamePlayer) {
            Debug.LogError($"Game scene loaded for player {networkPlayer.DisplayName}, but we already detected the game scene loading for them.");
            return;
        }
        if (_playerCount > -1 && playerCount != _playerCount) {
            Debug.LogError($"{nameof(playerCount)} ({playerCount}) is a different value than what we previously recorded ({_playerCount})!");
            return;
        }

        _playerCount = playerCount;
        _readyPlayerCount++;

        gamePlayer.Data = networkPlayer.index switch {
            0 => Player1Data,
            1 => Player2Data,
            _ => throw new IndexOutOfRangeException(
                $"Tried to set up network player with invalid index ({networkPlayer.index})")
        };

        // Set up the game once all players have connected
        if (_playerCount == _readyPlayerCount) {
            SetupMPGame();
        }
    }

    public void SetPlayers(MPGamePlayer localPlayer, MPGamePlayer opponentPlayer, ICommandController commandController) {
        _localPlayer = localPlayer;
        _opponentPlayer = opponentPlayer;
        CommandController = commandController;
    }

    private void SetupSPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        SetupCommandController(false);

        _localPlayer = Instantiate(SPGamePlayerPrefab);
        _localPlayer.Data = Player1Data;
        _opponentPlayer = Instantiate(SPGamePlayerPrefab);
        _opponentPlayer.Data = Player2Data;
        
        _gameInitialized = true;
    }
    
    private void SetupCommandController(bool multiplayer) {
        if (multiplayer) {
            if (!NetworkServer.active) {
                // The server will handle this
                return;
            } 
            MPCommandController newController = Instantiate(MPCommandControllerPrefab, transform);
            NetworkServer.Spawn(newController.gameObject);
            CommandController = newController;
        } else {
            CommandController = Instantiate(SPCommandControllerPrefab, transform);
        }

        CommandController.Initialize(Unit1, Unit2, SpawnBucket);
    }

    public GridEntity GetEntityAtLocation(Vector3Int location) {
        return CommandController?.GetEntityAtCell(location);
    }

    public IGamePlayer GetPlayer(GridEntity.Team team) {
        if (_localPlayer.Data.Team == team) {
            return _localPlayer;
        } else if (_opponentPlayer.Data.Team == team) {
            return _opponentPlayer;
        } else {
            throw new ArgumentException($"Invalid team ({team}");
        }
    }
}
