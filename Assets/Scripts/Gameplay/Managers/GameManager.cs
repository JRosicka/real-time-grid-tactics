using System;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.UI;
using UnityEngine;

/// <summary>
/// Central manager for accessing entities, players, and other managers
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("References")] 
    public Transform SpawnBucket;
    public GridController GridController;
    public ICommandManager CommandManager;
    public GameSetupManager GameSetupManager;
    public GameConfiguration Configuration;
    public SelectionInterface SelectionInterface;
    public ResourcesInterface ResourcesInterface;
    
    public PathfinderService PathfinderService;
    
    public IGamePlayer LocalPlayer { get; private set; }
    public IGamePlayer OpponentPlayer { get; private set; }
    
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("GameManager instance is not null!!");
        }
        
        Instance = this;
    }

    private void Start() {
        GameSetupManager.Initialize();
        SelectionInterface.Initialize();
        PathfinderService = new PathfinderService();
    }

    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtLocation(Vector2Int location) {
        return CommandManager?.GetEntitiesAtCell(location);
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
        OpponentPlayer = opponentPlayer;
        ResourcesInterface.Initialize(LocalPlayer.ResourcesController);
    }

    public void SetupCommandManager(ICommandManager commandManager) {
        CommandManager = commandManager;
        CommandManager.Initialize(SpawnBucket);
    }

    #endregion
}
