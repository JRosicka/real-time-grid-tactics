using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

/// <summary>
/// Interface through which to do any game-related actions, like moving a unit, spawning a unit, using an ability, etc.
/// In multiplayer games, these commands are networked to the server.
/// </summary>
public interface ICommandManager {
    void Initialize(Transform spawnBucket);
    void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team);
    /// <summary>
    /// Stop keeping track of an entity and also destroy it
    /// </summary>
    /// <param name="entity"></param>
    void UnRegisterEntity(GridEntity entity);
    void DestroyEntity(GridEntity entity);
    void MoveEntityToCell(GridEntity entity, Vector2Int destination);
    GridEntity GetEntityAtCell(Vector2Int location);
    Vector2Int GetLocationForEntity(GridEntity entity);
    void PerformAbility(IAbility ability);
    void MarkAbilityCooldownExpired(IAbility ability);
}