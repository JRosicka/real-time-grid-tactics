using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using Mirror;
using UnityEngine;

public class MPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
        NetworkServer.Spawn(SpawnBucket.gameObject);
        
        // Only initialize this on the server. Clients do not need to handle an ability queue.
        AbilityQueueExecutor.Initialize(this, gameEndManager, abilityAssignmentManager);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity) {
        LogTimestamp(nameof(SpawnEntity));
        CmdSpawnEntity(data, spawnLocation, team, spawnerEntity);
    }

    public override void AddUpgrade(UpgradeData data, GameTeam team) {
        CmdAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        LogTimestamp(nameof(RegisterEntity));
        CmdRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override async void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        await Task.Delay(TimeSpan.FromSeconds(AbilityQueueExecutor.UpdateFrequency));    // TODO maybe would be better to have this be handled in the AbilityQueueExecutor queue execution directly
        LogTimestamp(nameof(UnRegisterEntity));
        CmdUnRegisterEntity(entity, showDeathAnimation);
    }

    public override void DestroyEntity(GridEntity entity) {
        CmdDestroyEntity(entity);
    }

    public override void PerformAbility(IAbility ability, bool clearQueueFirst, bool handleCost, bool fromInput) {
        LogTimestamp(nameof(PerformAbility));
        CmdPerformAbility(ability, clearQueueFirst, handleCost, fromInput);
    }

    public override void QueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront, bool fromInput) {
        LogTimestamp(nameof(QueueAbility));
        CmdQueueAbility(ability, clearQueueFirst, insertAtFront, fromInput);
    }

    public override void RemoveAbilityFromQueue(GridEntity entity, IAbility queuedAbility) {
        LogTimestamp(nameof(RemoveAbilityFromQueue));
        CmdRemoveAbilityFromQueue(entity, queuedAbility.UID);
    }

    public override void ClearAbilityQueue(GridEntity entity) {
        LogTimestamp(nameof(ClearAbilityQueue));
        // I think this is safe to do?
        if (entity.QueuedAbilities.Count == 0) return;
        CmdClearAbilityQueue(entity);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        CmdMarkAbilityCooldownExpired(ability);
    }

    public override void CancelAbility(IAbility ability) {
        CmdCancelAbility(ability);
    }

    public override void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        CmdUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }


    [Command(requiresAuthority = false)] // TODO this should definitely require authority
    private void CmdSpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity entityToIgnore) {
        LogTimestamp(nameof(CmdSpawnEntity));
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            NetworkServer.Spawn(entityInstance.gameObject);
            
            entityInstance.ServerInitialize(data, team, spawnLocation);
            entityInstance.RpcInitialize(data, team);
            
            return entityInstance;
        }, team, entityToIgnore);
    }

    [Command(requiresAuthority = false)]
    private void CmdAddUpgrade(UpgradeData data, GameTeam team) {
        DoAddUpgrade(data, team);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdRegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        LogTimestamp(nameof(CmdRegisterEntity));
        DoRegisterEntity(entity, data, position, entityToIgnore);
    }

    [Command(requiresAuthority = false)]
    private void CmdUnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        LogTimestamp(nameof(CmdUnRegisterEntity));
        // TODO it would be better to have these both (entity collection sync and this rpc call) go out at the same time.
        // Can't do that cleanly currently since the entity collection is a syncvar.
        RpcEntityUnregistered(entity, showDeathAnimation);
        DoUnRegisterEntity(entity);
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroyEntity(GridEntity entity) {
        NetworkServer.Destroy(entity.gameObject);
    }

    [ClientRpc]
    private void RpcEntityUnregistered(GridEntity entity, bool showDeathAnimation) {
        LogTimestamp(nameof(RpcEntityUnregistered));
        DoMarkEntityUnregistered(entity, showDeathAnimation);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdPerformAbility(IAbility ability, bool clearQueueFirst, bool handleCost, bool fromInput) {
        LogTimestamp(nameof(CmdPerformAbility));
        
        if (Cheats.NeedsToDisconnect) {
            Cheats.NeedsToDisconnect = false;
            throw new Exception("Forced exception from cheats");
        }
        
        bool success = DoPerformAbility(ability, clearQueueFirst, handleCost, fromInput);
        if (success) {
            RpcAbilityPerformed(ability);
        } else if (!ability.WaitUntilLegal) {
            RpcAbilityFailed(ability);
        }
    }

    [ClientRpc]
    private void RpcAbilityPerformed(IAbility ability) {
        LogTimestamp(nameof(RpcAbilityPerformed));
        DoAbilityPerformed(ability);
    }

    [Command(requiresAuthority = false)]
    private void CmdQueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront, bool fromInput) {
        LogTimestamp(nameof(CmdQueueAbility));
        DoQueueAbility(ability, clearQueueFirst, insertAtFront, fromInput);
        RpcUpdateAbilityQueue(ability.Performer, ability.Performer.QueuedAbilities);
    }
    
    [ClientRpc]
    private void RpcUpdateAbilityQueue(GridEntity performer, List<IAbility> updatedAbilityQueue) {
        DoUpdateAbilityQueue(performer, updatedAbilityQueue);
    }

    [Command(requiresAuthority = false)]
    private void CmdRemoveAbilityFromQueue(GridEntity entity, int abilityID) {
        DoRemoveAbilityFromQueue(entity, abilityID);
        RpcUpdateAbilityQueue(entity, entity.QueuedAbilities);
    }

    [Command(requiresAuthority = false)]
    private void CmdClearAbilityQueue(GridEntity entity) {
        DoClearAbilityQueue(entity);
        RpcUpdateAbilityQueue(entity, entity.QueuedAbilities);
    }

    [Command(requiresAuthority = false)]
    private void CmdMarkAbilityCooldownExpired(IAbility ability) {
        RpcMarkAbilityCooldownExpired(ability);
    }
    
    [ClientRpc]
    private void RpcMarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability, false);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdCancelAbility(IAbility ability) {
        DoCancelAbility(ability);
        RpcMarkAbilityCanceled(ability);
    }

    [ClientRpc]
    private void RpcMarkAbilityCanceled(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability, true);
    }

    [ClientRpc]    // TODO probably just target the client of the player who tried to do the ability
    private void RpcAbilityFailed(IAbility ability) {
        LogTimestamp(nameof(RpcAbilityFailed));
        DoAbilityFailed(ability);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        // Update the field right away on the server
        DoUpdateNetworkableField(parent, fieldName, newValue, metadata);
        RpcUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }

    [ClientRpc(includeOwner = false)]
    private void RpcUpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        DoUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }
}