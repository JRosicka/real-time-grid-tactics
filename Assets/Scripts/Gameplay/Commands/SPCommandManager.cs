using System.Collections.Generic;
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

    public override void RegisterEntity(GridEntity entity, Vector2Int position) {
        DoRegisterEntity(entity, position);
    }

    public override void UnRegisterAndDestroyEntity(GridEntity entity) {
        DoUnRegisterEntity(entity);
        Destroy(entity.gameObject);
    }

    public override void MoveEntityToCell(GridEntity entity, Vector2Int destination) {
        DoMoveEntityToCell(entity, destination);
        DoEntityMoved(entity, destination);
    }

    public override void PerformAbility(IAbility ability) {
        DoPerformAbility(ability);
        DoAbilityPerformed(ability);
    }
}