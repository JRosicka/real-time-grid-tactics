using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

public class SPCommandManager : AbstractCommandManager {
    public override void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team) {
        DoSpawnEntity(data, spawnLocation, () => {
            GridEntity entityInstance = Instantiate(GridEntityPrefab, GridController.GetWorldPosition(spawnLocation), Quaternion.identity, SpawnBucket);
            entityInstance.DoInitialize(data, team);
            return entityInstance;
        }, team);
    }

    protected override void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position) {
        DoRegisterEntity(entity, data, position);
    }

    public override void UnRegisterEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        DoMarkEntityUnregistered(entity);
    }

    public override void DestroyEntity(GridEntity entity) {
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