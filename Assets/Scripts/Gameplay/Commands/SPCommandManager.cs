using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using Mirror;
using UnityEngine;

public class SPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
        AbilityExecutor.Initialize(this, gameEndManager, abilityAssignmentManager);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity, Vector2Int spawnerLocation, bool built) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            
            entityInstance.ServerInitialize(data, team, spawnLocation); 
            entityInstance.ClientInitialize(data, team, built);
            
            return entityInstance;
        }, team, spawnerEntity, spawnerLocation); 
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        DoRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        DoMarkEntityUnregistered(entity, showDeathAnimation);
        DoUnRegisterEntity(entity);
    }

    public override void DestroyEntity(GridEntity entity) {
        Destroy(entity.gameObject);
    }

    public override void StartPerformingAbility(IAbility ability, bool fromInput) {
        DoStartPerformingAbility(ability, fromInput);
    }

    public override void AbilityEffectPerformed(IAbility ability) {
        DoAbilityEffectPerformed(ability);
    }
    public override void AbilityFailed(IAbility ability) {
        DoAbilityFailed(ability);
    }

    public override void UpdateInProgressAbilities(GridEntity entity) {
        DoUpdateInProgressAbilities(entity, entity.InProgressAbilities);
    }

    public override void QueueAbility(IAbility ability, IAbility abilityToDependOn) {
        DoQueueAbility(ability, abilityToDependOn);
    }
    
    public override void MarkAbilityTimerExpired(IAbility ability) {
        DoMarkAbilityTimerExpired(ability, false);
    }

    public override void UpdateUpgradeStatus(UpgradeData data, GameTeam team, UpgradeStatus newStatus) {
        DoUpdateUpgradeStatus(data, team, newStatus);
        DoMarkUpgradeStatusUpdated(data, team, newStatus);
    }

    public override void MarkUpgradeTimerExpired(UpgradeData upgradeData, GameTeam team) {
        DoMarkUpgradeTimerExpired(upgradeData, team);
    }

    public override void CancelAbility(IAbility ability) {
        bool success = DoCancelAbility(ability);
        if (success) {
            DoMarkAbilityTimerExpired(ability, true);
        }
    }

    public override void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        DoUpdateNetworkableField(parent, fieldName, newValue, metadata);
    }
}