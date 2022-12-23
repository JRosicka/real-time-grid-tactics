using System;
using Gameplay.Config;
using GamePlay.Entities;
using UnityEngine;

/// <summary>
/// Central manager for accessing entities, players, and other managers
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("References")] 
    public Transform SpawnBucket;
    public GridController GridController;
    public ICommandController CommandController;
    public GameSetupManager GameSetupManager;
    public GridEntity GridEntityPrefab;
    
    public IGamePlayer LocalPlayer { get; private set; }
    public IGamePlayer OpponentPlayer { get; private set; }

    [Header("Unit 1")] 
    public bool Unit1_LocalTeam;
    public EntityData Unit1;
    public void SpawnUnit1() {
        IGamePlayer player = Unit1_LocalTeam ? LocalPlayer : OpponentPlayer;
        CommandController.SpawnEntity(Unit1, player.Data.SpawnLocation, player.Data.Team);
    }

    [Header("Unit 2")] 
    public bool Unit2_LocalTeam;
    public EntityData Unit2;
    public void SpawnUnit2() {
        IGamePlayer player = Unit2_LocalTeam ? LocalPlayer : OpponentPlayer;
        CommandController.SpawnEntity(Unit2, player.Data.SpawnLocation, player.Data.Team);
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
        GameSetupManager.Initialize();
    }
    
    public GridEntity GetEntityAtLocation(Vector2Int location) {
        return CommandController?.GetEntityAtCell(location);
    }

    public Vector2Int GetLocationForEntity(GridEntity entity) {
        if (CommandController == null) {
            throw new Exception($"{nameof(GetLocationForEntity)} failed: Command controller not yet initialized");
        }
        return CommandController.GetLocationForEntity(entity);
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
    }

    public void SetupCommandController(ICommandController commandController) {
        CommandController = commandController;
        CommandController.Initialize(GridEntityPrefab, SpawnBucket);
    }

    #endregion
}
