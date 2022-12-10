using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MPCommandController : AbstractCommandController {
    public readonly SyncDictionary<Vector3Int, GridEntity> EntitiesOnGrid_Impl = new SyncDictionary<Vector3Int, GridEntity>();
    
    // TODO this is where I could add some "is this player allowed to call this on the entity" checks
    public override IDictionary<Vector3Int, GridEntity> EntitiesOnGrid => EntitiesOnGrid_Impl;

    public override void SpawnEntity(int entityID, Vector3Int spawnLocation, GridEntity.Team team) {
        CmdSpawnEntity(entityID, spawnLocation, team);
    }

    public override void RegisterEntity(GridEntity entity, Vector3Int position) {
        CmdRegisterEntity(entity, position);
    }

    public override void UnRegisterAndDestroyEntity(GridEntity entity) {
        CmdUnRegisterEntity(entity);
    }

    public override void MoveEntityToCell(GridEntity entity, Vector3Int destination) {
        CmdMoveEntityToCell(entity, destination);
    }

    public override void SnapEntityToCell(GridEntity entity, Vector3Int destination) {
        CmdSnapEntityToCell(entity, destination);
    }

    
    
    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    private void CmdSpawnEntity(int entityID, Vector3Int spawnLocation, GridEntity.Team team) {
        DoSpawnEntity(entityID, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(entityID == 1 ? Unit1 : Unit2, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            NetworkServer.Spawn(entityInstance.gameObject);
            entityInstance.RpcInitialize(team);
            return entityInstance;
        }, team);
    }
    [Command(requiresAuthority = false)]
    private void CmdRegisterEntity(GridEntity entity, Vector3Int position) {
        DoRegisterEntity(entity, position);
    }

    [Command(requiresAuthority = false)]
    private void CmdUnRegisterEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        NetworkServer.Destroy(entity.gameObject);
    }

    [Command(requiresAuthority = false)]
    private void CmdMoveEntityToCell(GridEntity entity, Vector3Int destination) {
        DoMoveEntityToCell(entity, destination);
    }

    [Command(requiresAuthority = false)]
    private void CmdSnapEntityToCell(GridEntity entity, Vector3Int destination) {
        DoSnapEntityToCell(entity, destination);
    }
}