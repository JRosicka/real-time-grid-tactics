using System.Collections.Generic;
using System.Linq;
using Game.Network;
using Mirror;
using UnityEngine;

// TODO for lobby + basic network + basic gameplay milestone: 
// 1. Make it so that units kill other units when they click on them
// 2. Configure team colors and indices, figure out flow of information for that, have it determine spawns and team colors and which units are which
// 3. Pass a GridEntity prefab (eventually a ScriptableObject) to AbstractCommandController.SpawnEntity(...) instead of an int

public class GameManager : NetworkBehaviour {
    public static GameManager Instance;

    public SPGamePlayer SPGamePlayerPrefab;
    
    [Header("References")] 
    public GridController GridController;

    private IGamePlayer _localPlayer;
    private IGamePlayer _opponentPlayer;
    
    private bool _gameInitialized;
    public ICommandController CommandController;
    
    [Header("Unit 1")] 
    public Vector3Int SpawnLocationUnit1;
    public GridUnit Unit1;
    public void SpawnUnit1() {
        CommandController.SpawnEntity(1, SpawnLocationUnit1);
    }

    [Header("Unit 2")] 
    public Vector3Int SpawnLocationUnit2;
    public GridUnit Unit2;
    public void SpawnUnit2() {
        CommandController.SpawnEntity(2, SpawnLocationUnit2);
    }

    [Header("Config")]
    public Transform SpawnBucket;
    public MPCommandController MPCommandControllerPrefab;
    public SPCommandController SPCommandControllerPrefab;
    
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
        // TODO Also probably set a flag in some script that we can access here so that we know whether to start a SP game.
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
        DetectPlayers();
        _gameInitialized = true;
    }

    [ClientRpc]
    private void DetectPlayers() {
        // TODO Do I need to do anything to get this rpc call to run on clients? Since this isn't a NetworkBehavior object.
        List<MPGamePlayer> players = FindObjectsOfType<MPGamePlayer>().ToList();
        _localPlayer = players.First(p => p.isLocalPlayer);
        _opponentPlayer = players.First(p => !p.isLocalPlayer);
        CommandController = FindObjectOfType<MPCommandController>();
    }

    [Server]
    public void SetupMPPlayer(GameNetworkPlayer networkPlayer, MPGamePlayer gamePlayer) {
        if ((MPGamePlayer) _localPlayer == gamePlayer || (MPGamePlayer) _opponentPlayer == gamePlayer) {
            Debug.LogError($"Game scene loaded for player {networkPlayer.DisplayName}, but we already detected the game scene loading for them.");
            return;
        }
        if (networkPlayer.isLocalPlayer) {
            _localPlayer = gamePlayer;
        } else {
            _opponentPlayer = gamePlayer;
        }

        gamePlayer.SetIndex(networkPlayer.index);
        // TODO set color

        // Set up the game once both players have connected
        if (_localPlayer != null && _opponentPlayer != null) {
            SetupMPGame();
        }
    }

    private void SetupSPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        SetupCommandController(false);

        _localPlayer = Instantiate(SPGamePlayerPrefab);
        _localPlayer.SetIndex(0);
        // TODO set color

        _opponentPlayer = Instantiate(SPGamePlayerPrefab);
        _opponentPlayer.SetIndex(1);
        // TODO set color
        
        _gameInitialized = true;
    }
    
    private void SetupCommandController(bool multiplayer) {
        if (multiplayer) {
            if (!NetworkServer.active) {
                // The server will handle this
                return;
            } 
            MPCommandController newController = Instantiate(MPCommandControllerPrefab, transform);    // TODO make sure we instantiate this correctly so it syncs to all clients, see point dot example from sample scene
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
}
