using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SPCommandController : AbstractCommandController {
    protected override IDictionary<Vector3Int, GridEntity> EntitiesOnGrid() {
        return EntitiesOnGrid_Impl;
    }

    public readonly Dictionary<Vector3Int, GridEntity> EntitiesOnGrid_Impl = new Dictionary<Vector3Int, GridEntity>();
    
    public override void SpawnEntity(int entityID, Vector3Int spawnLocation) {
        DoSpawnEntity(entityID, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(entityID == 1 ? Unit1 : Unit2, SpawnBucket);
            return entityInstance;
        });
    }

    public override void RegisterEntity(GridEntity entity, Vector3Int position) {
        DoRegisterEntity(entity, position);
    }

    public override void UnRegisterAndDestroyEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        Destroy(entity.gameObject);
    }

    public override void MoveEntityToCell(GridEntity entity, Vector3Int destination) {
        DoMoveEntityToCell(entity, destination);
    }

    public override void SnapEntityToCell(GridEntity entity, Vector3Int destination) {
        DoSnapEntityToCell(entity, destination);
    }
}