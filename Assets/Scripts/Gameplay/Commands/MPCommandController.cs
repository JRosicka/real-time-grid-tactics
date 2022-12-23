using System.Collections.Generic;
using Gameplay.Config;
using GamePlay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

public class MPCommandController : AbstractCommandController {
    public readonly SyncDictionary<Vector2Int, GridEntity> EntitiesOnGrid_Impl = new SyncDictionary<Vector2Int, GridEntity>();
    
    // TODO this is where I could add some "is this player allowed to call this on the entity" checks
    public override IDictionary<Vector2Int, GridEntity> EntitiesOnGrid => EntitiesOnGrid_Impl;

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        CmdSpawnEntity(data, spawnLocation, team);
    }

    public override void RegisterEntity(GridEntity entity, Vector2Int position) {
        CmdRegisterEntity(entity, position);
    }

    public override void UnRegisterAndDestroyEntity(GridEntity entity) {
        CmdUnRegisterEntity(entity);
    }

    public override void MoveEntityToCell(GridEntity entity, Vector2Int destination) {
        CmdMoveEntityToCell(entity, destination);
    }

    public override void SnapEntityToCell(GridEntity entity, Vector2Int destination) {
        CmdSnapEntityToCell(entity, destination);
    }

    public override void PerformAbility(IAbility ability, GridEntity performer) {
        CmdPerformAbility(ability, performer);
    }


    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    private void CmdSpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            NetworkServer.Spawn(entityInstance.gameObject);
            entityInstance.RpcInitialize(data, team);
            return entityInstance;
        }, team);
    }
    [Command(requiresAuthority = false)]
    private void CmdRegisterEntity(GridEntity entity, Vector2Int position) {
        DoRegisterEntity(entity, position);
    }

    [Command(requiresAuthority = false)]
    private void CmdUnRegisterEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        NetworkServer.Destroy(entity.gameObject);
    }

    [Command(requiresAuthority = false)]
    private void CmdMoveEntityToCell(GridEntity entity, Vector2Int destination) {
        DoMoveEntityToCell(entity, destination);
    }

    [Command(requiresAuthority = false)]
    private void CmdSnapEntityToCell(GridEntity entity, Vector2Int destination) {
        DoSnapEntityToCell(entity, destination);
    }

    [Command(requiresAuthority = false)]
    private void CmdPerformAbility(IAbility abilityInstance, GridEntity performer) {
        DoPerformAbility(abilityInstance, performer);
        RpcAbilityPerformed(abilityInstance, performer);
    }

    [ClientRpc]
    private void RpcAbilityPerformed(IAbility abilityInstance, GridEntity performer) {
        DoAbilityPerformed(abilityInstance, performer);
    }
}