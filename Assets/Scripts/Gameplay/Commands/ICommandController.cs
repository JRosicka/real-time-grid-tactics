using System.Collections.Generic;
using Gameplay.Config;
using GamePlay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

public interface ICommandController {
    IDictionary<Vector2Int, GridEntity> EntitiesOnGrid { get; }
    void Initialize(GridEntity gridEntityPrefab, Transform spawnBucket);
    void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team);
    void RegisterEntity(GridEntity entity);
    void UnRegisterAndDestroyEntity(GridEntity entity);
    void MoveEntityToCell(GridEntity entity, Vector2Int destination);
    void SnapEntityToCell(GridEntity entity, Vector2Int destination);
    GridEntity GetEntityAtCell(Vector2Int location);
    Vector2Int GetLocationForEntity(GridEntity entity);
    void PerformAbility(IAbility ability, GridEntity performer);
}