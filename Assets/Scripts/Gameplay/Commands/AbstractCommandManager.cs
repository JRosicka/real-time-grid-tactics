using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using Mirror;
using Sirenix.Utilities;
using UnityEngine;
using Util;

/// <summary>
/// Interface through which to do any game-related actions, like moving a unit, spawning a unit, using an ability, etc.
/// In multiplayer games, these commands are networked to the server.
///
/// Also keeps a collection of all the currently active <see cref="GridEntity"/>s and their locations on the grid. 
/// </summary>
public abstract class AbstractCommandManager : NetworkBehaviour, ICommandManager {
    [field:SyncVar]
    public Transform SpawnBucket { get; protected set; }
    public AbilityQueueExecutor AbilityQueueExecutor;

    public GridEntity GridEntityPrefab;
    
    // Server only
    private List<int> _canceledAbilities = new List<int>();
    // Server only
    private bool AbilityAlreadyCanceled(IAbility ability) => _canceledAbilities.Contains(ability.UID);

    protected GridController GridController => GameManager.Instance.GridController;
    private AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;
    
    // TODO this is where I could add some "is this player allowed to call this on the entity" checks
    [SyncVar(hook = nameof(OnEntityCollectionChanged))] 
    private GridEntityCollection _entitiesOnGrid = new GridEntityCollection();
    private void OnEntityCollectionChanged(GridEntityCollection oldValue, GridEntityCollection newValue) {
        LogTimestamp(nameof(OnEntityCollectionChanged));
        EntityCollectionChangedEvent?.Invoke();
    }

    public abstract void CancelAbility(IAbility ability);
    public abstract void UpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata);

    /// <summary>
    /// An entity was just registered (spawned). Triggered on server. 
    /// </summary>
    public event Action<GameTeam> EntityRegisteredEvent;
    /// <summary>
    /// An entity was just unregistered (killed). Triggered on server. 
    /// </summary>
    public event Action<GameTeam> EntityUnregisteredEvent;
    public event Action EntityCollectionChangedEvent;

    public GridEntityCollection EntitiesOnGrid => _entitiesOnGrid;

    public abstract void Initialize(Transform spawnBucketPrefab, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager);
    
    /// <summary>
    /// Attempts to spawn a new instance of the provided <see cref="GridEntity"/> at the specified location on the game
    /// grid. No-op if another entity already exists in the specified location. 
    /// </summary>
    public abstract void SpawnEntity(EntityData data, Vector2Int spawnLocation, GameTeam team, GridEntity spawnerEntity, bool movementOnCooldown);
    // TODO I don't think this needs to be in CommandManager since this is only called by the server and it doesn't contain any RPC calls
    public abstract void AddUpgrade(UpgradeData data, GameTeam team);

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
        // We need to update the entity location first so that EntityCollectionChanged listeners get the updated value for the entity location
        entity.UpdateEntityLocation(destination);
        SyncEntityCollection();
        // Now that the entity collection is synced, we can let listeners know that the entity moved 
        entity.TriggerEntityMovedEvent();
    }
    
    public GridEntityCollection.PositionedGridEntityCollection GetEntitiesAtCell(Vector2Int location) {
        return _entitiesOnGrid.EntitiesAtLocation(location);
    }

    public abstract void PerformAbility(IAbility ability, bool clearQueueFirst, bool handleCost, bool fromInput);
    public abstract void QueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront, bool fromInput);
    public abstract void RemoveAbilityFromQueue(GridEntity entity, IAbility queuedAbility);
    public abstract void ClearAbilityQueue(GridEntity entity);

    public abstract void MarkAbilityCooldownExpired(IAbility ability);

    protected void DoSpawnEntity(EntityData data, Vector2Int spawnLocation, Func<GridEntity> spawnFunc, GameTeam team, GridEntity spawnerEntity, bool movementOnCooldown) {
        List<GridEntity> entitiesToIgnore = spawnerEntity != null ? new List<GridEntity> {spawnerEntity} : null;
        if (!PathfinderService.CanEntityEnterCell(spawnLocation, data, team, entitiesToIgnore)) {
            return;
        }

        GridEntity entityInstance = spawnFunc();
        RegisterEntity(entityInstance, data, spawnLocation, spawnerEntity);

        if (movementOnCooldown) {
            GameplayTile tile = GameManager.Instance.GridController.GridData.GetCell(spawnLocation)!.Tile;
            GameManager.Instance.AbilityAssignmentManager.AddMovementTime(entityInstance, entityInstance.MoveTimeToTile(tile));
        }
        
        // Handle starting movement
        if (spawnerEntity != null && spawnerEntity.TargetLocationLogicValue.CanRally) {
            if (data.Tags.Contains(EntityTag.Worker)) {
                // Workers get move-commanded
                entityInstance.TryMoveToCell(spawnerEntity.TargetLocationLogicValue.CurrentTarget, false);
            } else {
                // Everything else attack-moves
                entityInstance.TryAttackMoveToCell(spawnerEntity.TargetLocationLogicValue.CurrentTarget);
            }
        }
        
        // Now that the entity is registered, perform any on-start abilities
        if (GameManager.Instance.GameSetupManager.GameInitialized) {
            AbilityAssignmentManager.PerformOnStartAbilitiesForEntity(entityInstance);
        }
    }

    protected void DoAddUpgrade(UpgradeData data, GameTeam team) {
        GameManager.Instance.GetPlayerForTeam(team).OwnedPurchasablesController.AddUpgrade(data);
    }
    
    protected void DoRegisterEntity(GridEntity entity, EntityData data, Vector2Int position, GridEntity entityToIgnore) {
        if (entity.Registered)
            return;
        
        _entitiesOnGrid.RegisterEntity(entity, position, data.GetStackOrder(), entityToIgnore);
        entity.Registered = true;
        SyncEntityCollection();
        EntityRegisteredEvent?.Invoke(entity.Team);
    }

    protected void DoUnRegisterEntity(GridEntity entity) {
        entity.BuildQueue.CancelAllBuilds();
        _entitiesOnGrid.UnRegisterEntity(entity);
        SyncEntityCollection();
        EntityUnregisteredEvent?.Invoke(entity.Team);
    }

    protected void DoMarkEntityUnregistered(GridEntity entity, bool showDeathAnimation) {
        entity.OnUnregistered(showDeathAnimation);
    }
    
    protected bool DoPerformAbility(IAbility ability, bool clearQueueFirst, bool handleCost, bool fromInput) {
        // Don't do anything if the performer has been killed 
        if (ability.Performer == null) return false;

        if (fromInput) {
            ability.Performer.ToggleHoldPosition(false);
        }
        
        if (clearQueueFirst) {
            ClearAbilityQueue(ability.Performer); 
        }
        // Assign a UID here since this is guaranteed to be on the server (if MP)
        if (ability.UID == default) {
            ability.UID = IDUtil.GenerateUID();
        }
        
        if (ability.AbilityData.PayCostUpFront && handleCost) {
            // We need to pay the cost now instead of in PerformAbility because we do not pay the cost there for up-front cost 
            // abilities. This is necessary because in PerformAbility we don't know where the cost might have been payed, 
            // so we need to do it here. 
            if (!ability.AbilityData.CanPayCost(ability.BaseParameters, ability.Performer)) {
                Debug.Log($"Tried to pay the cost up front while performing the ability {ability.AbilityData.AbilitySlotInfo.ID}, but we are not able to pay the cost.");
                if (ability.WaitUntilLegal) {
                    QueueAbility(ability, false, false, false);
                }
                return false;
            }
            ability.PayCost(true);
        }

        if (ability.PerformAbility()) {
            return true;
        }

        if (ability.WaitUntilLegal) {
            QueueAbility(ability, false, false, false);
        }

        return false;
    }

    protected void DoQueueAbility(IAbility ability, bool clearQueueFirst, bool insertAtFront, bool fromInput) {
        if (clearQueueFirst) {
            ClearAbilityQueue(ability.Performer);
        }

        if (fromInput) {
            ability.Performer.ToggleHoldPosition(false);
        }

        // Assign a UID here since this is guaranteed to be on the server (if MP)
        if (ability.UID == default) {
            ability.UID = IDUtil.GenerateUID();
        }

        if (ability.AbilityData.PayCostUpFront) {
            if (!ability.AbilityData.CanPayCost(ability.BaseParameters, ability.Performer)) {
                Debug.Log($"Tried to pay the cost up front while queueing the ability {ability.AbilityData.AbilitySlotInfo.ID}, but we are not able to pay the cost. No op.");
                return;
            }
            ability.PayCost(true);
        }
        
        if (insertAtFront) {
            ability.Performer.QueuedAbilities.Insert(0, ability);
        } else {
            ability.Performer.QueuedAbilities.Add(ability);
        }
    }
    
    protected void DoUpdateAbilityQueue(GridEntity performer, List<IAbility> updatedAbilityQueue) {
        performer.UpdateAbilityQueue(updatedAbilityQueue);
    }

    protected void DoRemoveAbilityFromQueue(GridEntity entity, int abilityID) {
        IAbility queuedAbility = entity.QueuedAbilities.FirstOrDefault(t => t.UID == abilityID);
        if (queuedAbility == null) {
            // This can happen if the whole queue was cleared between sending the remove command and now
            return;
        }

        entity.QueuedAbilities.Remove(queuedAbility);
    }

    protected void DoClearAbilityQueue(GridEntity entity) {
        List<IAbility> queuedAbilities = new List<IAbility>(entity.QueuedAbilities);
        queuedAbilities.ForEach(a => GameManager.Instance.CommandManager.CancelAbility(a));
    }

    protected void DoAbilityPerformed(IAbility ability) {
        ability.Performer.AbilityPerformed(ability);
    }

    protected void DoMarkAbilityCooldownExpired(IAbility ability, bool canceled) {
        // Check to make sure that the entity performing the ability is still around
        if (ability.Performer != null) {
            AbilityAssignmentManager.ExpireTimerForAbility(ability.Performer, ability, canceled);
        } 
    }

    /// <summary>
    /// Try to cancel the given ability
    /// </summary>
    /// <returns>True if successfully canceled, otherwise false if can't be canceled or already canceled</returns>
    protected bool DoCancelAbility(IAbility ability) {
        if (!ability.AbilityData.CanBeCanceled) return false;
        if (AbilityAlreadyCanceled(ability)) return false;

        _canceledAbilities.Add(ability.UID);
        AbilityAssignmentManager.ExpireAbility(ability.Performer, ability, true);
        return true;
    }

    protected void DoAbilityFailed(IAbility ability) {
        ability.Performer.AbilityFailed(ability.AbilityData);
    }

    protected void DoUpdateNetworkableField(NetworkBehaviour parent, string fieldName, INetworkableFieldValue newValue, string metadata) {
        if (parent == null) {
            // The parent (GridEntity or otherwise) has been destroyed. Just do nothing. 
            Debug.Log($"Parent is kill for {fieldName}");
            return;
        }
        Type type = parent.GetType();
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MemberInfo info = type.GetField(fieldName, bindingFlags) as MemberInfo 
                          ?? type.GetProperty(fieldName, bindingFlags); 
        NetworkableField networkableField = (NetworkableField)info.GetMemberValue(parent);
        networkableField.DoUpdateValue(newValue, metadata);
    }
    
    /// <summary>
    /// Reset the reference for <see cref="_entitiesOnGrid"/> to force a sync across clients. Just updating fields in the class
    /// is not enough to get the sync to occur. 
    /// </summary>
    private void SyncEntityCollection() {    // TODO: If networking is horribly slow when there are a lot of GridEntities in the game... this is probably why. Kinda yucky. 
        LogTimestamp(nameof(SyncEntityCollection));
        _entitiesOnGrid = new GridEntityCollection(_entitiesOnGrid.Entities);
        if (!NetworkClient.active) {
            // SP, so syncvars won't work
            EntityCollectionChangedEvent?.Invoke();
        }
    }
    
    [System.Diagnostics.Conditional("AF_LATENCY_TESTING")]
    protected void LogTimestamp(string trigger) {
        Debug.Log($"Timestamp for ({trigger}): {DateTime.Now:h:mm:ss.fff}");
    }
}