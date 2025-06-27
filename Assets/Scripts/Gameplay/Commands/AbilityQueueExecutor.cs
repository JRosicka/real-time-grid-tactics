using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles checking on each active <see cref="GridEntity"/>'s <see cref="IAbility"/> queue and executing the queued
/// abilities at the desired time.
///
/// There should only ever be one of these in a game. In SP, there is just one living in the scene. In MP, only the server
/// has a copy. 
/// </summary>
public class AbilityQueueExecutor : MonoBehaviour {
    [FormerlySerializedAs("_updateFrequency")]
    public float UpdateFrequency;
    
    private bool _initialized;
    private ICommandManager _commandManager;
    private GameEndManager _gameEndManager;
    private AbilityAssignmentManager _abilityAssignmentManager;

    private float _timeUntilNextUpdate;
    
    public void Initialize(ICommandManager commandManager, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager) {
        _commandManager = commandManager;
        _gameEndManager = gameEndManager;
        _abilityAssignmentManager = abilityAssignmentManager;
        _timeUntilNextUpdate = UpdateFrequency;

        _initialized = true;
    }

    private void Update() {
        if (!_initialized) return;

        _timeUntilNextUpdate -= Time.deltaTime;
        if (_timeUntilNextUpdate <= 0) {
            _timeUntilNextUpdate += UpdateFrequency;
            
            // Time for an update
            ExecuteQueue();
            
            // Also perform a check for game end
            _gameEndManager.CheckForGameEnd();
        }
    }

    /// <summary>
    /// Perform a full round of game updates on all entities
    /// </summary>
    private void ExecuteQueue() {
        // First, execute abilities that involve updating the GridEntityCollection (or that don't depend on that)
        List<GridEntity> allEntities = _commandManager.EntitiesOnGrid.AllEntities();
        allEntities.ForEach(e => ExecuteQueueForEntity(e, AbilityExecutionType.DuringGridUpdates));
        
        // Second, execute abilities that rely on (but don't update) entity positions (i.e. attacking)
        allEntities.ForEach(e => ExecuteQueueForEntity(e, AbilityExecutionType.AfterGridUpdates));
        
        // Third, apply damage from attacks
        // TODO-abilities
        
        // Fourth, unregister any marked entities
        // TODO-abilities
    }

    /// <summary>
    /// Execute the queue for a single <see cref="GridEntity"/>. Runs through the queued abilities and tries to execute
    /// the one at the front IFF it matches the execution type.
    /// - If the ability matches and is completed (cleared from queue), immediately attempts to execute the next queued
    ///   ability in the same fashion
    /// - If the ability does not match, no-op even if there are later abilities in the queue that do match
    /// </summary>
    private void ExecuteQueueForEntity(GridEntity entity, AbilityExecutionType executionType) {
        List<IAbility> abilityQueue = entity.QueuedAbilities;

        // Perform default ability if queue empty
        if (abilityQueue.IsNullOrEmpty()) {
            PerformDefaultAbility(entity, executionType);
            return;
        }

        // Cycle through the queued abilities, trying to perform each one until one does not get completed or removed
        bool queueUpdated = false;
        int lastAbilityID = -1;
        while (true) {
            // No-op if the queue is empty or the next queued ability does not match type
            if (abilityQueue.IsNullOrEmpty()) {
                break;
            }
            IAbility nextAbility = abilityQueue[0];
            if (nextAbility.AbilityData.ExecutionType != executionType) {
                break;
            }
            
            // If this is the same ability that we tried to resolve last loop, then give up
            if (nextAbility.UID == lastAbilityID) {
                break;
            }

            // Try to perform the next ability
            if (PerformQueuedAbility(entity, nextAbility) || !nextAbility.WaitUntilLegal) {
                RemoveAbilityFromQueue(entity, nextAbility);
                queueUpdated = true;
                lastAbilityID = nextAbility.UID;
            } else {
                // The ability didn't get performed, and we need to wait. So our work here is done. 
                break;
            }
        }
        if (queueUpdated) {
            // TODO-abilities would it be better to update each entity at the very end of the full queue execution? Since these RPC calls will go out after potentially multiple steps of updates finish on the server. 
            _commandManager.UpdateAbilityQueue(entity); 
        }
    }
    
    private bool PerformQueuedAbility(GridEntity entity, IAbility ability) {
        if (!ability.AbilityData.AbilityLegal(ability.BaseParameters, ability.Performer)) {
            entity.AbilityFailed(ability.AbilityData);
            return false;
        }
            
        _commandManager.PerformAbility(ability, false, false, false);
        return true;
    }

    /// <summary>
    /// Remove the ability from the queue (here on the server)
    /// </summary>
    private void RemoveAbilityFromQueue(GridEntity entity, IAbility ability) {
        IAbility queuedAbility = entity.QueuedAbilities.FirstOrDefault(t => t.UID == ability.UID);
        entity.QueuedAbilities.Remove(queuedAbility);
    }
    
    /// <summary>
    /// Perform the given entity's configured default ability, but only if that ability matches the passed in execution type.
    /// Currently, this ability can only ever be attacking
    /// </summary>
    private void PerformDefaultAbility(GridEntity entity, AbilityExecutionType executionType) {
        if (!entity.EntityData.AttackByDefault) return;
        Vector2Int? location = entity.Location;
        if (location == null) return;

        AttackAbilityData data = entity.GetAbilityData<AttackAbilityData>();
        if (data == null) return;
        if (data.ExecutionType != executionType) return;
            
        _abilityAssignmentManager.PerformAbility(entity, data, new AttackAbilityParameters {
            TargetFire = false,
            Destination = location.Value
        }, false, true);
    }
}