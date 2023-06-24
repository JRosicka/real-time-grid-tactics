using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

/// <summary>
/// Interface through which to do any game-related actions, like moving a unit, spawning a unit, using an ability, etc.
/// In multiplayer games, these commands are networked to the server.
///
/// Also keeps a collection of all the currently active <see cref="GridEntity"/>s and their locations on the grid. 
/// </summary>
public abstract class AbstractCommandManager : NetworkBehaviour, ICommandManager {
    [field:SyncVar]
    public Transform SpawnBucket { get; protected set; }
    public GridEntity GridEntityPrefab;

    protected GridController GridController => GameManager.Instance.GridController;
    
    // TODO this is where I could add some "is this player allowed to call this on the entity" checks
    [SyncVar]
    private GridEntityCollection _entitiesOnGrid = new GridEntityCollection();

    protected AbilityQueueExecutor AbilityQueueExecutor;

    /// <summary>
    /// An entity was just registered (spawned). Triggered on server. 
    /// </summary>
    public event Action<GridEntity.Team> EntityRegisteredEvent;
    /// <summary>
    /// An entity was just unregistered (killed). Triggered on server. 
    /// </summary>
    public event Action<GridEntity.Team> EntityUnregisteredEvent;

    public GridEntityCollection EntitiesOnGrid => _entitiesOnGrid;

    public abstract void Initialize(Transform spawnBucketPrefab);
    
    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    public abstract void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team, GridEntity entityToIgnore);
    public abstract void AddUpgrade(UpgradeData data, GridEntity.Team team);

    // TODO need to have some way of verifying that these commands are legal for the client to do - especially doing stuff with GridEntites, we gotta own em
    // Maybe we can just make these abstract methods virtual, include a check at the beginning, and then have the overrides call base() at the start
    /// <summary>
    /// Register a new <see cref="GridEntity"/> into the <see cref="GridEntityCollection"/> so that we can keep track of it.
    /// We need to pass both the entity and its data separately because the entity might not have bee initialized with its data yet. 
    /// </summary>
    protected abstract void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore);
    
    public abstract void UnRegisterEntity(GridEntity entity, bool showDeathAnimation);
    public abstract void DestroyEntity(GridEntity entity);

    public void MoveEntityToCell(GridEntity entity, Vector2Int destination) {
        _entitiesOnGrid.MoveEntity(entity, destination);
        SyncEntityCollection();
    }
    
    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtCell(Vector2Int location) {
        return _entitiesOnGrid.EntitiesAtLocation(location);
    }

    public Vector2Int GetLocationForEntity(GridEntity entity) {
        return _entitiesOnGrid.LocationOfEntity(entity);
    }

    public abstract void PerformAbility(IAbility ability);
    public abstract void MarkAbilityCooldownExpired(IAbility ability);

    protected void DoSpawnEntity(EntityData data, Vector2Int spawnLocation, Func<GridEntity> spawnFunc, GridEntity.Team team, GridEntity entityToIgnore) {
        List<GridEntity> entitiesToIgnore = entityToIgnore != null ? new List<GridEntity> {entityToIgnore} : null;
        if (!GameManager.Instance.GridController.CanEntityEnterCell(spawnLocation, data, team, entitiesToIgnore)) {
            return;
        }

        GridEntity entityInstance = spawnFunc();
        RegisterEntity(entityInstance, data, spawnLocation, entityToIgnore); 
    }

    protected void DoAddUpgrade(UpgradeData data, GridEntity.Team team) {
        GameManager.Instance.GetPlayerForTeam(team).OwnedPurchasablesController.AddUpgrade(data);
    }
    
    protected void DoRegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        if (entity.Registered)
            return;
        
        _entitiesOnGrid.RegisterEntity(entity, position, data.GetStackOrder(), entityToIgnore);
        entity.Registered = true;
        SyncEntityCollection();
        Debug.Log($"Registered new entity {entity.UnitName} at position {position}");
        EntityRegisteredEvent?.Invoke(entity.MyTeam);
    }

    protected void DoUnRegisterEntity(GridEntity entity) {
        _entitiesOnGrid.UnRegisterEntity(entity);
        SyncEntityCollection();
        EntityUnregisteredEvent?.Invoke(entity.MyTeam);
    }

    protected void DoMarkEntityUnregistered(GridEntity entity, bool showDeathAnimation) {
        entity.OnUnregistered(showDeathAnimation);
    }
    
    protected bool DoPerformAbility(IAbility abilityInstance) {
        // Clear the queue
        abilityInstance.Performer.ClearAbilityQueue();
        // Assign a UID here since this is guaranteed to be on the server (if MP)
        abilityInstance.UID = IDUtil.GenerateUID();
        return abilityInstance.PerformAbility();
    }

    protected void DoAbilityPerformed(IAbility abilityInstance) {
        abilityInstance.Performer.AbilityPerformed(abilityInstance);
    }

    protected void DoMarkAbilityCooldownExpired(IAbility ability) {
        // Check to make sure that the entity performing the ability is still around
        if (ability.Performer != null) {
            ability.Performer.ExpireTimerForAbility(ability);
        } 
    }

    /// <summary>
    /// Reset the reference for <see cref="_entitiesOnGrid"/> to force a sync across clients. Just updating fields in the class
    /// is not enough to get the sync to occur. 
    /// </summary>
    private void SyncEntityCollection() {    // TODO: If networking is horribly slow when there are a lot of GridEntities in the game... this is probably why. Kinda yucky. 
        _entitiesOnGrid = new GridEntityCollection(_entitiesOnGrid.Entities);
    }
}