using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

public class MPCommandManager : AbstractCommandManager {
    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        CmdSpawnEntity(data, spawnLocation, team);
    }

    public override void AddUpgrade(UpgradeData data, GridEntity.Team team) {
        CmdAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position) {
        CmdRegisterEntity(entity, data, position);
    }

    public override void UnRegisterEntity(GridEntity entity) {
        CmdUnRegisterEntity(entity);
    }

    public override void DestroyEntity(GridEntity entity) {
        CmdDestroyEntity(entity);
    }

    public override void PerformAbility(IAbility ability) {
        CmdPerformAbility(ability);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        CmdMarkAbilityCooldownExpired(ability);
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
    private void CmdAddUpgrade(UpgradeData data, GridEntity.Team team) {
        DoAddUpgrade(data, team);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdRegisterEntity(GridEntity entity, EntityData data, Vector2Int position) {
        DoRegisterEntity(entity, data, position);
    }

    [Command(requiresAuthority = false)]
    private void CmdUnRegisterEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        RpcEntityUnregistered(entity);
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroyEntity(GridEntity entity) {
        NetworkServer.Destroy(entity.gameObject);
    }

    [ClientRpc]
    private void RpcEntityUnregistered(GridEntity entity) {
        DoMarkEntityUnregistered(entity);
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

    [Command(requiresAuthority = false)]
    private void CmdMarkAbilityCooldownExpired(IAbility ability) {
        RpcMarkAbilityCooldownExpired(ability);
    }

    [ClientRpc]
    private void RpcMarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability);
    }

    [ClientRpc]    // TODO probably just target the client of the player who tried to do the ability
    private void RpcAbilityFailed(IAbility abilityInstance) {
        abilityInstance.Performer.AbilityFailed(abilityInstance.AbilityData);
    }
}