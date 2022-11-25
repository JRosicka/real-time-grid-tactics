using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Handles spawning, updating positions, and generally keeping track of <see cref="GridEntity"/>s on the grid
/// </summary>
public class EntityManager : MonoBehaviour {
    [Header("Unit 1")] 
    public Vector3Int SpawnLocationUnit1;
    public GridUnit Unit1;
    public void SpawnUnit1() {
        SpawnEntity(1, SpawnLocationUnit1);
    }

    [Header("Unit 2")] 
    public Vector3Int SpawnLocationUnit2;
    public GridUnit Unit2;
    public void SpawnUnit2() {
        SpawnEntity(2, SpawnLocationUnit2);
    }

    [Header("Config")]
    public Transform SpawnBucket;
    public MPCommandController MPCommandControllerPrefab;
    public SPCommandController SPCommandControllerPrefab;
    
    [HideInInspector]
    public GridEntity SelectedEntity;

    private GridController gridController => GameManager.Instance.GridController;

    private AbstractCommandController _gameNetworkController;

    public void SetupCommandController(bool multiplayer) {
        if (multiplayer) {
            _gameNetworkController = Instantiate(MPCommandControllerPrefab, transform);    // TODO make sure we instantiate this correctly so it syncs to all clients, see point dot example from sample scene
            NetworkServer.Spawn(_gameNetworkController.gameObject);
        } else {
            _gameNetworkController = Instantiate(SPCommandControllerPrefab, transform);
        }

        _gameNetworkController.Unit1 = Unit1;
        _gameNetworkController.Unit2 = Unit2;
        _gameNetworkController.SpawnBucket = SpawnBucket;
    }

    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    public void SpawnEntity(int entityID, Vector3Int spawnLocation) {
        _gameNetworkController.SpawnEntity(entityID, spawnLocation);
    }

    public void RegisterEntity(GridEntity entity) {
        _gameNetworkController.RegisterEntity(entity);
    }
    
    public void RegisterEntity(GridEntity entity, Vector3Int position) {
        _gameNetworkController.RegisterEntity(entity, position);
    }

    public void UnRegisterAndDestroyEntity(GridEntity entity) {
        _gameNetworkController.UnRegisterAndDestroyEntity(entity);
    }

    /// <summary>
    /// Immediately moves the specified entity's location to the specified cell destination
    /// </summary>
    public void SnapEntityToCell(GridEntity entity, Vector3Int destination) {
        _gameNetworkController.SnapEntityToCell(entity, destination);
    }

    public void MoveEntityToCell(GridEntity entity, Vector3Int destination) {
        _gameNetworkController.MoveEntityToCell(entity, destination);
    }

    public GridEntity GetEntityAtLocation(Vector3Int location) {
        if (_gameNetworkController == null)
            return null;
        return _gameNetworkController.GetEntityAtCell(location);
    }

    private class IllegalEntityPlacementException : Exception {
        public IllegalEntityPlacementException(Vector3Int location, 
                GridEntity attemptedRegistryEntity, 
                GridEntity entityAtLocation) 
            : base($"Failed to place {nameof(GridEntity)} ({attemptedRegistryEntity.UnitName}) at location {location}"
                   + $" because another {nameof(GridEntity)} ({entityAtLocation.UnitName}) already exists there") { }
    }
}
