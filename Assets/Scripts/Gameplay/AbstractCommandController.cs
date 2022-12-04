using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public abstract class AbstractCommandController : NetworkBehaviour, ICommandController {
    [SyncVar]
    public Transform SpawnBucket;
        // TODO so I think that SyncVars can not have prefab (uninstantiated) values set to them, they need to be instantiated instances.
                 // Now, I don't think I actually need to sync these since only the server needs to spawn stuff, but still. If I DO want to do this,
                 // I think it would make more sense to store a reference to a unit-type-specific scriptableobject and use that to configure a
                 // generic GridEntry or GridUnit prefab to spawn. That's probably the better direction to go anyway.
                 // For now, we'll just have the server get these references applied, and the clients won't have them. 
    public GridEntity Unit1; // TODO
    public GridEntity Unit2;

    protected GridController GridController => GameManager.Instance.GridController;
    
    public abstract IDictionary<Vector3Int, GridEntity> EntitiesOnGrid { get; }

    public void Initialize(GridUnit unit1, GridUnit unit2, Transform spawnBucket) {
        Unit1 = unit1;
        Unit2 = unit2;
        SpawnBucket = spawnBucket;
    }
    
    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    /// <returns>True if the spawn was successful, otherwise false if no entity was created</returns>
    public abstract void SpawnEntity(int entityID, Vector3Int spawnLocation);
    // TODO need to have some way of verifying that these commands are legal for the client to do - especially doing stuff with GridEntites, we gotta own em
    // Maybe we can just make these abstract methods virtual, include a check at the beginning, and then have the overrides call base() at the start
    public abstract void RegisterEntity(GridEntity entity, Vector3Int position);
    
    public void RegisterEntity(GridEntity entity) {
        if (entity.Registered)
            return;

        RegisterEntity(entity, GridController.GetCellPosition(entity.transform.position));
    }

    public abstract void UnRegisterAndDestroyEntity(GridEntity entity);
    
    public abstract void MoveEntityToCell(GridEntity entity, Vector3Int destination);

    public abstract void SnapEntityToCell(GridEntity entity, Vector3Int destination);

    public GridEntity GetEntityAtCell(Vector3Int location) {
        EntitiesOnGrid.TryGetValue(location, out GridEntity ret);
        return ret;
    }

    

    protected void DoSpawnEntity(int entityID, Vector3Int spawnLocation, Func<GridEntity> spawnFunc) {
        if (EntitiesOnGrid.ContainsKey(spawnLocation) && EntitiesOnGrid[spawnLocation] != null) {
            return;
        }

        GridEntity entityInstance = spawnFunc();
        RegisterEntity(entityInstance, spawnLocation);
        SnapEntityToCell(entityInstance, spawnLocation);
    }
    
    protected void DoRegisterEntity(GridEntity entity, Vector3Int position) {
        if (entity.Registered)
            return;
        if (EntitiesOnGrid.ContainsKey(position) && EntitiesOnGrid[position] != null) {
            throw new IllegalEntityPlacementException(position, entity, EntitiesOnGrid[position]);
        }
        
        EntitiesOnGrid[position] = entity;
        entity.Registered = true;
        Debug.Log($"Registered new entity {entity.UnitName} at position {position}");
    }

    protected void DoUnRegisterEntity(GridEntity entity) {
        if (!EntitiesOnGrid.Values.Contains(entity)) {
            Debug.LogWarning("Attempted to unregister an entity that is not registered");
            return;
        }
        
        KeyValuePair<Vector3Int, GridEntity> item = EntitiesOnGrid.First(kvp => kvp.Value == entity);

        EntitiesOnGrid.Remove(item);
    }

    protected void DoMoveEntityToCell(GridEntity entity, Vector3Int destination) {
        if (EntitiesOnGrid.ContainsKey(destination) && EntitiesOnGrid[destination] != null) {
            throw new IllegalEntityPlacementException(destination, entity, EntitiesOnGrid[destination]);
        }

        // Remove the entity from its previous position
        if (EntitiesOnGrid.Values.Contains(entity)) {
            // TODO Perhaps a dictionary isn't the best data structure for this, or I should switch the keys and values
            KeyValuePair<Vector3Int, GridEntity> item = EntitiesOnGrid.First(kvp => kvp.Value == entity);
            EntitiesOnGrid.Remove(item.Key);
        }
        
        // Record the location of the entity
        EntitiesOnGrid[destination] = entity;
        
        // Perform the move
        SnapEntityToCell(entity, destination);
    }
    
    protected void DoSnapEntityToCell(GridEntity entity, Vector3Int destination) {
        entity.transform.position = GridController.GetWorldPosition(destination);
    }
    
    private class IllegalEntityPlacementException : Exception {
        public IllegalEntityPlacementException(Vector3Int location, 
            GridEntity attemptedRegistryEntity, 
            GridEntity entityAtLocation) 
            : base($"Failed to place {nameof(GridEntity)} ({attemptedRegistryEntity.UnitName}) at location {location}"
                   + $" because another {nameof(GridEntity)} ({entityAtLocation.UnitName}) already exists there") { }
    }
}