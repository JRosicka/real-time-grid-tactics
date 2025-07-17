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
    void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity, bool movementOnCooldown);
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
    /// <summary>
    /// Tell the server to start the process of performing an ability. Triggers an execution of active abilities in <see cref="AbilityExecutor"/>
    /// </summary>
    void StartPerformingAbility(IAbility ability, bool fromInput);
    void AbilityEffectPerformed(IAbility ability);
    void AbilityFailed(IAbility ability);
    void UpdateInProgressAbilities(GridEntity entity);
    void QueueAbility(IAbility ability, IAbility abilityToDependOn);
    void MarkAbilityCooldownExpired(IAbility ability);
    void CancelAbility(IAbility ability);
    void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metaData);
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
    AbilityExecutor AbilityExecutor { get; }
}