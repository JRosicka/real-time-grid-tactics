using System;
using System.Threading.Tasks;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

public class SPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
        AbilityQueueExecutor.Initialize(this, gameEndManager);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team, GridEntity spawnerEntity) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            
            entityInstance.ServerInitialize(data, team, spawnLocation); 
            entityInstance.ClientInitialize(data, team);
            
            return entityInstance;
        }, team, spawnerEntity); 
    }
 
    public override void AddUpgrade(UpgradeData data, GridEntity.Team team) {
        DoAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        DoRegisterEntity(entity, data, position, entityToIgnore);
    }

    public override async void UnRegisterEntity(GridEntity entity, bool showDeathAnimation) {
        await Task.Delay(TimeSpan.FromSeconds(AbilityQueueExecutor.UpdateFrequency));
        DoMarkEntityUnregistered(entity, showDeathAnimation);
        DoUnRegisterEntity(entity);
    }

    public override void DestroyEntity(GridEntity entity) {
        Destroy(entity.gameObject);
    }

    public override void PerformAbility(IAbility ability, bool clearQueueFirst) {
        if (DoPerformAbility(ability, clearQueueFirst)) {
            DoAbilityPerformed(ability);
        } else if (!ability.WaitUntilLegal) {
            DoAbilityFailed(ability);
        }
    }

    public override void QueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront) {
        DoQueueAbility(ability, clearQueueFirst, insertAtFront);
    }

    public override void RemoveAbilityFromQueue(GridEntity entity, IAbility queuedAbility) {
        DoRemoveAbilityFromQueue(entity, queuedAbility);
    }

    public override void ClearAbilityQueue(GridEntity entity) {
        DoClearAbilityQueue(entity);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability, false);
    }

    public override void CancelAbility(IAbility ability) {
        DoCancelAbility(ability);
        DoMarkAbilityCooldownExpired(ability, true);
    }
}