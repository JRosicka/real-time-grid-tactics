using System.Collections;
using System.Collections.Generic;
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
        SpawnEntity(Unit1, SpawnLocationUnit1);
    }

    [Header("Unit 2")] 
    public Vector3Int SpawnLocationUnit2;
    public GridUnit Unit2;
    public void SpawnUnit2() {
        SpawnEntity(Unit2, SpawnLocationUnit2);
    }

    public Transform SpawnBucket;
    
    private GridController gridController => GameManager.Instance.GridController;
    private Dictionary<Vector3Int, GridEntity> _entitiesOnGrid = new Dictionary<Vector3Int, GridEntity>();
    
    
    void Start() {
        SpawnEntity(Unit1, SpawnLocationUnit1);
        SpawnEntity(Unit2, SpawnLocationUnit2);
    }

    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    /// <returns>True if the spawn was successful, otherwise false if no entity was created</returns>
    public bool SpawnEntity(GridEntity entityPrefab, Vector3Int spawnLocation) {
        if (_entitiesOnGrid.ContainsKey(spawnLocation) && _entitiesOnGrid[spawnLocation] != null) {
            return false;
        }

        GridEntity entityInstance = Instantiate(entityPrefab, SpawnBucket);
        _entitiesOnGrid[spawnLocation] = entityInstance;
        SnapEntityToCell(entityInstance, spawnLocation);

        return true;
    }

    /// <summary>
    /// Immediately moves the specified entity's location to the specified cell destination
    /// </summary>
    public void SnapEntityToCell(GridEntity entity, Vector3Int destination) {
        entity.transform.position = gridController.GetWorldPosition(destination);
    }
}
