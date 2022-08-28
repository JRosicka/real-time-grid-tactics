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
public class EntityManager : NetworkBehaviour {
    [Header("Unit 1")] 
    public Vector3Int SpawnLocationUnit1;
    public GridUnit Unit1;
    public void SpawnUnit1() {
        CmdSpawnEntity(1, SpawnLocationUnit1);
    }

    [Header("Unit 2")] 
    public Vector3Int SpawnLocationUnit2;
    public GridUnit Unit2;
    public void SpawnUnit2() {
        CmdSpawnEntity(2, SpawnLocationUnit2);
    }

    public Transform SpawnBucket;
    [HideInInspector]
    public GridEntity SelectedEntity;

    private GridController gridController => GameManager.Instance.GridController;
    public readonly SyncDictionary<Vector3Int, GridEntity> _entitiesOnGrid = new SyncDictionary<Vector3Int, GridEntity>();
    
    
    void Start() {
        // SpawnEntity(Unit1, SpawnLocationUnit1);
        // SpawnEntity(Unit2, SpawnLocationUnit2);
    }

    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    /// <returns>True if the spawn was successful, otherwise false if no entity was created</returns>
    [Command(requiresAuthority = false)]
    public void CmdSpawnEntity(int entityID, Vector3Int spawnLocation) {
        if (_entitiesOnGrid.ContainsKey(spawnLocation) && _entitiesOnGrid[spawnLocation] != null) {
            return;
        }

        GridEntity entityInstance = Instantiate(entityID == 1 ? Unit1 : Unit2, SpawnBucket);
        NetworkServer.Spawn(entityInstance.gameObject);
        RegisterEntity(entityInstance, spawnLocation);
        CmdSnapEntityToCell(entityInstance, spawnLocation);
    }

    public void RegisterEntity(GridEntity entity) {
        if (entity.Registered)
            return;

        RegisterEntity(entity, gridController.GetCellPosition(entity.transform.position));
    }
    
    public void RegisterEntity(GridEntity entity, Vector3Int position) {
        if (entity.Registered)
            return;
        if (_entitiesOnGrid.ContainsKey(position) && _entitiesOnGrid[position] != null) {
            throw new IllegalEntityPlacementException(position, entity, _entitiesOnGrid[position]);
        }
        
        _entitiesOnGrid[position] = entity;
        entity.Registered = true;
        Debug.Log($"Registered new entity {entity.UnitName} at position {position}");
    }

    /// <summary>
    /// Immediately moves the specified entity's location to the specified cell destination
    /// </summary>
    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    public void CmdSnapEntityToCell(GridEntity entity, Vector3Int destination) {
        entity.transform.position = gridController.GetWorldPosition(destination);
    }

    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    public void CmdMoveEntityToPosition(GridEntity entity, Vector3Int destination) {
        if (_entitiesOnGrid.ContainsKey(destination) && _entitiesOnGrid[destination] != null) {
            throw new IllegalEntityPlacementException(destination, entity, _entitiesOnGrid[destination]);
        }

        // Remove the entity from its previous position
        if (_entitiesOnGrid.Values.Contains(entity)) {
            // TODO Perhaps a dictionary isn't the best data structure for this, or I should switch the keys and values
            KeyValuePair<Vector3Int, GridEntity> item = _entitiesOnGrid.First(kvp => kvp.Value == entity);
            _entitiesOnGrid.Remove(item.Key);
        }
        
        // Record the location of the entity
        _entitiesOnGrid[destination] = entity;
        
        // Perform the move
        CmdSnapEntityToCell(entity, destination);
    }

    public GridEntity GetEntityAtLocation(Vector3Int location) {
        _entitiesOnGrid.TryGetValue(location, out GridEntity ret);
        return ret;
    }

    private class IllegalEntityPlacementException : Exception {
        public IllegalEntityPlacementException(Vector3Int location, 
                GridEntity attemptedRegistryEntity, 
                GridEntity entityAtLocation) 
            : base($"Failed to place {nameof(GridEntity)} ({attemptedRegistryEntity.UnitName}) at location {location}"
                   + $" because another {nameof(GridEntity)} ({entityAtLocation.UnitName}) already exists there") { }
    }
}
