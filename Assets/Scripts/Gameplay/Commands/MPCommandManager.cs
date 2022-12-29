using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

public class MPCommandManager : AbstractCommandManager {
    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        CmdSpawnEntity(data, spawnLocation, team);
    }

    public override void RegisterEntity(GridEntity entity, Vector2Int position) {
        CmdRegisterEntity(entity, position);
    }

    public override void UnRegisterAndDestroyEntity(GridEntity entity) {
        CmdUnRegisterEntity(entity);
    }
    
    public override void PerformAbility(IAbility ability) {
        CmdPerformAbility(ability);
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
    private void CmdPerformAbility(IAbility abilityInstance) {
        bool success = DoPerformAbility(abilityInstance);
        if (success) {
            RpcAbilityPerformed(abilityInstance);
        } else {
            RpcAbilityFailed(abilityInstance);
        }
    }

    [ClientRpc]
    private void RpcAbilityPerformed(IAbility abilityInstance) {
        DoAbilityPerformed(abilityInstance);
    }

    [ClientRpc]    // TODO probably just target the client of the player who tried to do the ability
    private void RpcAbilityFailed(IAbility abilityInstance) {
        abilityInstance.Performer.AbilityFailed(abilityInstance.AbilityData);
    }
}