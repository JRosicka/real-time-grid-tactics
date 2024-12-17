using System;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using Mirror;
using UnityEngine;

/// <summary>
/// Interface through which to do any game-related actions, like moving a unit, spawning a unit, using an ability, etc.
/// In multiplayer games, these commands are networked to the server.
/// </summary>
public interface ICommandManager {
    void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager);
    void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity);
    void AddUpgrade(UpgradeData data, GameTeam team);
    /// <summary>
    /// Stop keeping track of an entity and also destroy it.
    /// Waits for one update cycle before doing so, so that any commands that the entity is executing in the execution
    /// cycle can be performed.
    /// </summary>
    void UnRegisterEntity(GridEntity entity, bool showDeathAnimation);
    void DestroyEntity(GridEntity entity);
    void MoveEntityToCell(GridEntity entity, Vector2Int destination);
    GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtCell(Vector2Int location);
    Vector2Int? GetLocationForEntity(GridEntity entity);
    void PerformAbility(IAbility ability, bool clearQueueFirst, bool handleCost);
    void QueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront);
    void RemoveAbilityFromQueue(GridEntity entity, IAbility queuedAbility);
    void ClearAbilityQueue(GridEntity entity);
    void MarkAbilityCooldownExpired(IAbility ability);
    void CancelAbility(IAbility ability);
    void UpdateNetworkableField<T>(NetworkBehaviour parent, string fieldName, T newValue);
    /// <summary>
    /// An entity was just registered (spawned). Triggered on server. 
    /// </summary>
    event Action<GameTeam> EntityRegisteredEvent;
    /// <summary>
    /// An entity was just unregistered (killed). Triggered on server. 
    /// </summary>
    event Action<GameTeam> EntityUnregisteredEvent;
    event Action EntityCollectionChangedEvent;
    GridEntityCollection EntitiesOnGrid { get; }
    Transform SpawnBucket { get; }
}