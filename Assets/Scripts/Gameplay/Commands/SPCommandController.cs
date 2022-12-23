using System.Collections.Generic;
using Gameplay.Config;
using GamePlay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

public class SPCommandController : AbstractCommandController {
    public readonly Dictionary<Vector2Int, GridEntity> EntitiesOnGrid_Impl = new Dictionary<Vector2Int, GridEntity>();

    public override IDictionary<Vector2Int, GridEntity> EntitiesOnGrid => EntitiesOnGrid_Impl;

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
    }

    public override void SnapEntityToCell(GridEntity entity, Vector2Int destination) {
        DoSnapEntityToCell(entity, destination);
    }

    public override void PerformAbility(IAbility ability, GridEntity performer) {
        DoPerformAbility(ability, performer);
        DoAbilityPerformed(ability, performer);
    }
}