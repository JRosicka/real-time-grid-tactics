using System;
using System.Collections.Generic;
using Game.Network;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.Managers;
using Gameplay.UI;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for accessing entities, players, and other managers
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("References")] 
    public Transform SpawnBucketPrefab;
    public GridController GridController;
    public GridInputController GridInputController;
    public ICommandManager CommandManager;
    public GameSetupManager GameSetupManager;
    public GameConfiguration Configuration;
    public SelectionInterface SelectionInterface;
    public ResourcesInterface ResourcesInterface;
    public PrefabAtlas PrefabAtlas;
    public Cheats Cheats;
    public DisconnectionDialog DisconnectionDialog;
    public AlertTextDisplayer AlertTextDisplayer;
    
    public PathfinderService PathfinderService;
    public EntitySelectionManager EntitySelectionManager;
    public GameEndManager GameEndManager;
    public DisconnectionHandler DisconnectionHandler;
    public AbilityAssignmentManager AbilityAssignmentManager;
    public QueuedStructureBuildsManager QueuedStructureBuildsManager;

    public IGamePlayer Player1;
    public IGamePlayer Player2;
    public GameTeam LocalTeam { get; private set; }
    
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("GameManager instance is not null!!");
        }
        
        Instance = this;
        GameEndManager = new GameEndManager(this);
        DisconnectionHandler = new DisconnectionHandler();
        AbilityAssignmentManager = new AbilityAssignmentManager();
        QueuedStructureBuildsManager = new QueuedStructureBuildsManager(this);
    }

    private void Start() {
        GameSetupManager.Initialize();
        SelectionInterface.Initialize();
        PathfinderService = new PathfinderService();
        GridController.Initialize();
        EntitySelectionManager = new EntitySelectionManager(this);
        GridInputController.Initialize(EntitySelectionManager, this);
        DisconnectionDialog.Initialize(DisconnectionHandler);
    }

    private void OnDestroy() {
        DisconnectionHandler?.UnregisterListeners(); 
    }

    [CanBeNull]
    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtLocation(Vector2Int location) {
        return CommandManager?.GetEntitiesAtCell(location);
    }

    [CanBeNull]
    public GridEntity GetTopEntityAtLocation(Vector2Int location) {
        return GetEntitiesAtLocation(location)?.GetTopEntity().Entity;
    }

    public IGamePlayer GetPlayerForTeam(GameTeam team) {
        if (Player1.Data.Team == team) {
            return Player1;
        } else if (Player2.Data.Team == team) {
            return Player2;
        } else {
            return null;
        }
    }

    #region Game setup
    
    public void SetPlayers(IGamePlayer player1, IGamePlayer player2, GameTeam localTeam) {
        LocalTeam = localTeam;
        
        // Set up players
        Player1 = player1;
        player1.Initialize(Configuration.GetUpgrades(), Configuration);
        Player2 = player2;
        player2.Initialize(Configuration.GetUpgrades(), Configuration);
        
        // Set up resources interface - for spectators, we want to track all players' resources
        IPlayerResourcesObserver resourcesObserver = localTeam switch {
            GameTeam.Spectator => new CompoundPlayerResourcesObserver(new List<IGamePlayer> { player1, player2 }),
            GameTeam.Player1 => new PlayerResourcesObserver(player1),
            GameTeam.Player2 => new PlayerResourcesObserver(player2),
            _ => throw new Exception($"Invalid local team: {localTeam}")
        };
        ResourcesInterface.Initialize(resourcesObserver);
    }

    public void SetupCommandManager(ICommandManager commandManager) {
        CommandManager = commandManager;
        CommandManager.Initialize(SpawnBucketPrefab, GameEndManager, AbilityAssignmentManager);
    }

    #endregion
    
    #region Game end

    public void ReturnToLobby() {
        if (!NetworkClient.active) {
            // SP. Just load to the main menu. 
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // MP, so return to the lobby
        GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
        gameNetworkManager.ServerChangeScene(gameNetworkManager.RoomScene);
    }

    public void ReturnToMainMenu() {
        if (!NetworkClient.active) {
            // SP. Just reload the game scene
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // MP, so return to the main menu
        GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
        gameNetworkManager.StopClient();
    }

    #endregion
}
