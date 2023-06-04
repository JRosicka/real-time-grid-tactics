using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

public class SPCommandManager : AbstractCommandManager {
    public override void Initialize(Transform spawnBucketPrefab) {
        SpawnBucket = Instantiate(spawnBucketPrefab);
    }

    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            
            // Set the items that we can't wait until client initialization for
            entityInstance.EntityData = data;
            entityInstance.MyTeam = team;
            
            entityInstance.DoInitialize(data, team);
            return entityInstance;
        }, team);
    }
 
    public override void AddUpgrade(UpgradeData data, GridEntity.Team team) {
        DoAddUpgrade(data, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position) {
        DoRegisterEntity(entity, data, position);
    }

    public override void UnRegisterEntity(GridEntity entity, bool animateDeath) {
        DoUnRegisterEntity(entity);
        if (animateDeath) {
            DoMarkEntityDead(entity);
        }
    }

    public override void DestroyEntity(GridEntity entity, bool unregisterFirst) {
        if (unregisterFirst) {
            DoUnRegisterEntity(entity);
        }
        Destroy(entity.gameObject);
    }

    public override void PerformAbility(IAbility ability) {
        DoPerformAbility(ability);
        DoAbilityPerformed(ability);
    }

    public override void MarkAbilityCooldownExpired(IAbility ability) {
        DoMarkAbilityCooldownExpired(ability);
    }
}