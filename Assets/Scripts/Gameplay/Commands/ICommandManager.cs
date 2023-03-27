using System;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

/// <summary>
/// Interface through which to do any game-related actions, like moving a unit, spawning a unit, using an ability, etc.
/// In multiplayer games, these commands are networked to the server.
/// </summary>
public interface ICommandManager {
    void Initialize(Transform spawnBucketPrefab);
    void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team);
    void AddUpgrade(UpgradeData data, GridEntity.Team team);
    /// <summary>
    /// Stop keeping track of an entity and also destroy it
    /// </summary>
    /// <param name="entity"></param>
    void UnRegisterEntity(GridEntity entity);
    void DestroyEntity(GridEntity entity);
    void MoveEntityToCell(GridEntity entity, Vector2Int destination);
    GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtCell(Vector2Int location);
    Vector2Int GetLocationForEntity(GridEntity entity);
    void PerformAbility(IAbility ability);
    void MarkAbilityCooldownExpired(IAbility ability);
    /// <summary>
    /// An entity was just registered (spawned). Triggered on server. 
    /// </summary>
    event Action<GridEntity.Team> EntityRegisteredEvent;
    /// <summary>
    /// An entity was just unregistered (killed). Triggered on server. 
    /// </summary>
    event Action<GridEntity.Team> EntityUnregisteredEvent;
    GridEntityCollection EntitiesOnGrid { get; }
    Transform SpawnBucket { get; }
}