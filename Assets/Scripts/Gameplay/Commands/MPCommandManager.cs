using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

public class MPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
        NetworkServer.Spawn(SpawnBucket.gameObject);
        
        // Only initialize this on the server. Clients do not need to handle an ability queue.
        AbilityQueueExecutor.Initialize(this);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team, GridEntity entityToIgnore) {
        CmdSpawnEntity(data, spawnLocation, team, entityToIgnore);
    }

    public override void AddUpgrade(UpgradeData data, GridEntity.Team team) {
        CmdAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        CmdRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        CmdUnRegisterEntity(entity, showDeathAnimation);
    }

    public override void DestroyEntity(GridEntity entity) {
        CmdDestroyEntity(entity);
    }

    public override void PerformAbility(IAbility ability, bool clearQueueFirst) {
        CmdPerformAbility(ability, clearQueueFirst);
    }

    public override void QueueAbility(IAbility ability) {
        CmdQueueAbility(ability);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        CmdMarkAbilityCooldownExpired(ability);
    }


    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    private void CmdSpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team, GridEntity entityToIgnore) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            NetworkServer.Spawn(entityInstance.gameObject);
            
            // Set the items that we can't wait until client initialization for
            entityInstance.EntityData = data;
            entityInstance.MyTeam = team;

            entityInstance.RpcInitialize(data, team);
            return entityInstance;
        }, team, entityToIgnore);
    }

    [Command(requiresAuthority = false)]
    private void CmdAddUpgrade(UpgradeData data, GridEntity.Team team) {
        DoAddUpgrade(data, team);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdRegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        DoRegisterEntity(entity, data, position, entityToIgnore);
    }

    [Command(requiresAuthority = false)]
    private void CmdUnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        DoUnRegisterEntity(entity);
        RpcEntityUnregistered(entity, showDeathAnimation);
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroyEntity(GridEntity entity) {
        NetworkServer.Destroy(entity.gameObject);
    }

    [ClientRpc]
    private void RpcEntityUnregistered(GridEntity entity, bool showDeathAnimation) {
        DoMarkEntityUnregistered(entity, showDeathAnimation);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdPerformAbility(IAbility ability, bool clearQueueFirst) {
        bool success = DoPerformAbility(ability, clearQueueFirst);
        if (success) {
            RpcAbilityPerformed(ability);
        } else if (!ability.WaitUntilLegal) {
            RpcAbilityFailed(ability);
        }
    }

    [ClientRpc]
    private void RpcAbilityPerformed(IAbility ability) {
        DoAbilityPerformed(ability);
    }

    [Command(requiresAuthority = false)]
    private void CmdQueueAbility(IAbility ability) {
        DoQueueAbility(ability);
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
    private void RpcAbilityFailed(IAbility ability) {
        DoAbilityFailed(ability);
    }
}