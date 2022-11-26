using System.Collections.Generic;
using UnityEngine;

public interface ICommandController {
    IDictionary<Vector3Int, GridEntity> EntitiesOnGrid { get; }
    void Initialize(GridUnit unit1, GridUnit unit2, Transform spawnBucket);
    void SpawnEntity(int entityID, Vector3Int spawnLocation);
    void RegisterEntity(GridEntity entity);
    void UnRegisterAndDestroyEntity(GridEntity entity);
    void MoveEntityToCell(GridEntity entity, Vector3Int destination);
    void SnapEntityToCell(GridEntity entity, Vector3Int destination);
    GridEntity GetEntityAtCell(Vector3Int location);
}