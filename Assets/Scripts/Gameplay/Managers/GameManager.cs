using System;
using System.Linq;
using Game.Network;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.Managers;
using Gameplay.UI;
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
    
    public PathfinderService PathfinderService;
    public EntitySelectionManager EntitySelectionManager;
    public GameEndManager GameEndManager;
    
    public IGamePlayer LocalPlayer { get; private set; }
    public IGamePlayer OpponentPlayer { get; private set; }
    
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("GameManager instance is not null!!");
        }
        
        Instance = this;
        GameEndManager = new GameEndManager(this);
    }

    private void Start() {
        GameSetupManager.Initialize();
        SelectionInterface.Initialize();
        PathfinderService = new PathfinderService();
        GridController.Initialize();
        EntitySelectionManager = new EntitySelectionManager(this);
        GridInputController.Initialize(EntitySelectionManager, this);
    }

    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtLocation(Vector2Int location) {
        return CommandManager?.GetEntitiesAtCell(location);
    }

    public GridEntity GetTopEntityAtLocation(Vector2Int location) {
        return GetEntitiesAtLocation(location)?.GetTopEntity().Entity;
    }

    public Vector2Int GetLocationForEntity(GridEntity entity) {
        if (CommandManager == null) {
            throw new Exception($"{nameof(GetLocationForEntity)} failed: Command controller not yet initialized");
        }
        return CommandManager.GetLocationForEntity(entity);
    }

    public IGamePlayer GetPlayerForTeam(GridEntity.Team team) {
        if (LocalPlayer.Data.Team == team) {
            return LocalPlayer;
        } else if (OpponentPlayer.Data.Team == team) {
            return OpponentPlayer;
        } else {
            throw new ArgumentException($"Invalid team ({team}");
        }
    }

    #region Game setup
    
    public void SetPlayers(IGamePlayer localPlayer, IGamePlayer opponentPlayer) {
        LocalPlayer = localPlayer;
        localPlayer.Initialize(Configuration.GetUpgrades());

        OpponentPlayer = opponentPlayer;
        opponentPlayer.Initialize(Configuration.GetUpgrades());

        ResourcesInterface.Initialize(LocalPlayer.ResourcesController);
    }

    public void SetupCommandManager(ICommandManager commandManager) {
        CommandManager = commandManager;
        CommandManager.Initialize(SpawnBucketPrefab, GameEndManager);
    }

    #endregion
    
    #region Game end

    public void ReturnToLobby() {
        if (!NetworkClient.active) {
            // SP. Just reload the game scene
            SceneManager.LoadScene("GamePlay");
            return;
        }

        // MP, so return to the lobby
        GameNetworkManager gameNetworkManager = FindObjectOfType<GameNetworkManager>();
        gameNetworkManager.ServerChangeScene(gameNetworkManager.RoomScene);
    }

    #endregion
}
