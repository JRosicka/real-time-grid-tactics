using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using Mirror;
using Scenes;
using UnityEngine;

public class MPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab, GameManager gameManager, AbilityAssignmentManager abilityAssignmentManager) {
        SpawnBucket = Instantiate(spawnBucketPrefab, gameManager.transform);
        NetworkServer.Spawn(SpawnBucket.gameObject);
        
        // Only initialize this on the server. Clients do not need to handle ability execution.
        AbilityExecutor.Initialize(this, gameManager.GameEndManager, abilityAssignmentManager);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity, Vector2Int spawnerLocation, bool built) {
        LogTimestamp(nameof(SpawnEntity));
        CmdSpawnEntity(data, spawnLocation, team, spawnerEntity, spawnerLocation, built);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        LogTimestamp(nameof(RegisterEntity));
        CmdRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        LogTimestamp(nameof(UnRegisterEntity));
        CmdUnRegisterEntity(entity, showDeathAnimation);
    }

    public override void DestroyEntity(GridEntity entity) {
        CmdDestroyEntity(entity);
    }

    public override void StartPerformingAbility(IAbility ability, bool fromInput) {
        LogTimestamp(nameof(StartPerformingAbility));
        CmdStartPerformingAbility(ability, fromInput);
    }

    [Server]
    public override void AbilityEffectPerformed(IAbility ability) {
        RpcAbilityEffectPerformed(ability);
    }
    [Server]
    public override void AbilityFailed(IAbility ability) {
        RpcAbilityFailed(ability);
    }

    public override void UpdateInProgressAbilities(GridEntity entity) {
        CmdUpdateInProgressAbilities(entity);
    }

    public override void QueueAbility(IAbility ability, IAbility abilityToDependOn) {
        CmdQueueAbility(ability, abilityToDependOn);
    }
    
    public override void MarkAbilityTimerExpired(IAbility ability) {
        CmdMarkAbilityCooldownExpired(ability);
    }

    public override void UpdateUpgradeStatus(UpgradeData data, GameTeam team, UpgradeStatus newStatus) {
        CmdUpdateUpgradeStatus(data, team, newStatus);
    }

    public override void MarkUpgradeTimerExpired(UpgradeData upgradeData, GameTeam team) {
        CmdMarkUpgradeTimerExpired(upgradeData, team);
    }

    public override void CancelAbility(IAbility ability, bool recordForReplay) {
        CmdCancelAbility(ability, recordForReplay);
    }

    public override void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        CmdUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }


    [Command(requiresAuthority = false)]
    private void CmdSpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity entityToIgnore, Vector2Int spawnerLocation, bool built) {
        LogTimestamp(nameof(CmdSpawnEntity));
        DoSpawnEntity(data, spawnLocation, entityUID => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            NetworkServer.Spawn(entityInstance.gameObject);
            
            entityInstance.ServerInitialize(data, team, spawnLocation);
            entityInstance.RpcInitialize(data, team, built, entityUID);
            
            return entityInstance;
        }, team, entityToIgnore, spawnerLocation);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateUpgradeStatus(UpgradeData data, GameTeam team, UpgradeStatus newStatus) {
        DoUpdateUpgradeStatus(data, team, newStatus);
        RpcUpdateUpgradeStatus(data, team, newStatus);
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
    
    [ClientRpc]
    private void RpcAbilityEffectPerformed(IAbility ability) {
        LogTimestamp(nameof(RpcAbilityEffectPerformed));
        DoAbilityEffectPerformed(ability);
    }

    [Command(requiresAuthority = false)]
    private void CmdStartPerformingAbility(IAbility ability, bool fromInput) {
        LogTimestamp(nameof(CmdStartPerformingAbility));
        DoStartPerformingAbility(ability, fromInput);
    }
    
    [ClientRpc]
    private void RpcUpdateInProgressAbilities(GridEntity performer, List<IAbility> updatedInProgressAbilities) {
        DoUpdateInProgressAbilities(performer, updatedInProgressAbilities);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateInProgressAbilities(GridEntity entity) {
        RpcUpdateInProgressAbilities(entity, entity.InProgressAbilities);
    }

    [Command(requiresAuthority = false)]
    private void CmdQueueAbility(IAbility ability, IAbility abilityToDependOn) {
        DoQueueAbility(ability, abilityToDependOn);
    }

    [Command(requiresAuthority = false)]
    private void CmdMarkAbilityCooldownExpired(IAbility ability) {
        RpcMarkAbilityCooldownExpired(ability);
    }
    
    [ClientRpc]
    private void RpcMarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityTimerExpired(ability, false);
    }
    
    [ClientRpc]
    private void RpcUpdateUpgradeStatus(UpgradeData data, GameTeam team, UpgradeStatus newStatus) {
        DoMarkUpgradeStatusUpdated(data, team, newStatus);
    }

    [Command(requiresAuthority = false)]
    private void CmdMarkUpgradeTimerExpired(UpgradeData upgradeData, GameTeam team) {
        RpcMarkUpgradeTimerExpired(upgradeData, team);
    }
    
    [ClientRpc]
    private void RpcMarkUpgradeTimerExpired(UpgradeData upgradeData, GameTeam team) {
        DoMarkUpgradeTimerExpired(upgradeData, team);
    }

    [Command(requiresAuthority = false)]
    private void CmdCancelAbility(IAbility ability, bool recordForReplay) {
        bool success = DoCancelAbility(ability, recordForReplay);
        if (success) {
            RpcMarkAbilityCanceled(ability);
        }
    }

    [ClientRpc]
    private void RpcMarkAbilityCanceled(IAbility ability) {
        DoMarkAbilityTimerExpired(ability, true);
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

    [ClientRpc]
    private void RpcUpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        if (GameTypeTracker.Instance.HostForNetworkedGame) {
            // We already updated this on the server in the Cmd call, don't update it here again
            return;
        }
        DoUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }
}