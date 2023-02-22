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
    [SyncVar]
    public Transform SpawnBucket;
    public GridEntity GridEntityPrefab;

    protected GridController GridController => GameManager.Instance.GridController;
    
    // TODO this is where I could add some "is this player allowed to call this on the entity" checks
    [SyncVar]
    public GridEntityCollection EntitiesOnGrid = new GridEntityCollection();

    public void Initialize(Transform spawnBucket) {
        SpawnBucket = spawnBucket;
    }
    
    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    public abstract void SpawnEntity(EntityData data, Vector2Int spawnLocation, GridEntity.Team team);
    // TODO need to have some way of verifying that these commands are legal for the client to do - especially doing stuff with GridEntites, we gotta own em
    // Maybe we can just make these abstract methods virtual, include a check at the beginning, and then have the overrides call base() at the start
    /// <summary>
    /// Register a new <see cref="GridEntity"/> into the <see cref="GridEntityCollection"/> so that we can keep track of it.
    /// We need to pass both the entity and its data separately because the entity might not have bee initialized with its data yet. 
    /// </summary>
    protected abstract void RegisterEntity(GridEntity entity, EntityData data, Vector2Int position);
    
    public abstract void UnRegisterEntity(GridEntity entity);
    public abstract void DestroyEntity(GridEntity entity);

    public void MoveEntityToCell(GridEntity entity, Vector2Int destination) {
        EntitiesOnGrid.MoveEntity(entity, destination);
        SyncEntityCollection();
    }
    
    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtCell(Vector2Int location) {
        return EntitiesOnGrid.EntitiesAtLocation(location);
    }

    public Vector2Int GetLocationForEntity(GridEntity entity) {
        return EntitiesOnGrid.LocationOfEntity(entity);
    }

    public abstract void PerformAbility(IAbility ability);
    public abstract void MarkAbilityCooldownExpired(IAbility ability);

    protected void DoSpawnEntity(EntityData data, Vector2Int spawnLocation, Func<GridEntity> spawnFunc, GridEntity.Team team) {
        if (!GameManager.Instance.GridController.CanEntityEnterCell(spawnLocation, data, team)) {
            return;
        }

        GridEntity entityInstance = spawnFunc();
        RegisterEntity(entityInstance, data, spawnLocation); 
    }
    
    protected void DoRegisterEntity(GridEntity entity, EntityData data, Vector2Int position) {
        if (entity.Registered)
            return;
        
        EntitiesOnGrid.RegisterEntity(entity, position, data.GetStackOrder());
        entity.Registered = true;
        SyncEntityCollection();
        Debug.Log($"Registered new entity {entity.UnitName} at position {position}");
    }

    protected void DoUnRegisterEntity(GridEntity entity) {
        EntitiesOnGrid.UnRegisterEntity(entity);
        SyncEntityCollection();
    }

    protected void DoMarkEntityUnregistered(GridEntity entity) {
        entity.OnUnregistered();
    }
    
    protected bool DoPerformAbility(IAbility abilityInstance) {
        // Assign a UID here since this is guaranteed to be on the server (if MP)
        abilityInstance.UID = IDUtil.GenerateUID();
        return abilityInstance.PerformAbility();
    }

    protected void DoAbilityPerformed(IAbility abilityInstance) {
        abilityInstance.Performer.AbilityPerformed(abilityInstance);
    }

    protected void DoMarkAbilityCooldownExpired(IAbility ability) {
        ability.Performer.ExpireTimerForAbility(ability);
    }

    /// <summary>
    /// Reset the reference for <see cref="EntitiesOnGrid"/> to force a sync across clients. Just updating fields in the class
    /// is not enough to get the sync to occur. 
    /// </summary>
    private void SyncEntityCollection() {    // TODO: If networking is horribly slow when there are a lot of GridEntities in the game... this is probably why. Kinda yucky. 
        EntitiesOnGrid = new GridEntityCollection(EntitiesOnGrid.Entities);
    }
}