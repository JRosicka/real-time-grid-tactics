using System;
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
            ExecuteQueue(false);
            
            // Also perform a check for game end
            _gameEndManager.CheckForGameEnd();
        }
    }

    /// <summary>
    /// Perform a full round of game updates on all entities
    /// </summary>
    public void ExecuteQueue(bool resetTimeUntilNextUpdate) {
        if (resetTimeUntilNextUpdate) {
            _timeUntilNextUpdate = UpdateFrequency;
        }
        
        // First, execute abilities that involve updating the GridEntityCollection (or that don't depend on that)
        List<GridEntity> allEntities = _commandManager.EntitiesOnGrid.AllEntities();
        allEntities.ForEach(e => ExecuteQueueForEntity(e, AbilityExecutionType.PreInteractionGridUpdate));
        
        // Second, execute interaction abilities that rely on (but don't update) entity positions (i.e. attacking)
        allEntities.ForEach(e => ExecuteQueueForEntity(e, AbilityExecutionType.Interaction));
        
        // Third, execute any post-interaction abilities that update the GridEntityCollection
        allEntities.ForEach(e => ExecuteQueueForEntity(e, AbilityExecutionType.PostInteractionGridUpdate));
        
        // Fourth, apply damage from attacks
        // TODO-abilities
        
        // Fifth, unregister any marked entities
        // TODO-abilities
    }

    /// <summary>
    /// Execute the queue for a single <see cref="GridEntity"/>. Runs through the queued abilities and tries to execute
    /// each ability that matches the execution type.
    /// - If the ability matches and is completed, it is cleared from the queue
    /// - Keeps performing abilities of the matching execution type until none remain. Even performs abilities that were
    ///   added during iterating. 
    /// </summary>
    private void ExecuteQueueForEntity(GridEntity entity, AbilityExecutionType executionType) {
        List<IAbility> abilityQueue = entity.QueuedAbilities;

        // Perform default ability if queue empty
        if (abilityQueue.IsNullOrEmpty() && executionType == AbilityExecutionType.Interaction) {
            PerformDefaultAbility(entity);
            return;
        }

        // Cycle through the queued abilities, trying to perform each one until one does not get completed or removed
        bool queueUpdated = false;
        List<int> seenAbilityIDs = new List<int>();
        while (true) {
            // No-op if the queue is empty
            if (abilityQueue.IsNullOrEmpty()) {
                break;
            }

            // Pick an ability that has matches the type and that we have not already tried to perform this execution
            IAbility nextAbility = abilityQueue.FirstOrDefault(a => a.ExecutionType == executionType && !seenAbilityIDs.Contains(a.UID));
            if (nextAbility == null) {
                break;
            }
            seenAbilityIDs.Add(nextAbility.UID);

            // Try to perform the next ability
            AbilityResult result = TryPerformAbility(nextAbility);
            switch (result) {
                case AbilityResult.CompletedWithoutEffect:
                    RemoveAbilityFromQueue(entity, nextAbility);
                    queueUpdated = true;
                    break;
                case AbilityResult.CompletedWithEffect:
                    _commandManager.AbilityEffectPerformed(nextAbility);
                    RemoveAbilityFromQueue(entity, nextAbility);
                    queueUpdated = true;
                    break;
                case AbilityResult.IncompleteWithEffect:
                    _commandManager.AbilityEffectPerformed(nextAbility);
                    break;
                case AbilityResult.IncompleteWithoutEffect:
                    break;
                case AbilityResult.Failed:
                    _commandManager.AbilityFailed(nextAbility);
                    RemoveAbilityFromQueue(entity, nextAbility);
                    queueUpdated = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        if (queueUpdated) {
            // TODO-abilities would it be better to update each entity at the very end of the full queue execution? Since these RPC calls will go out after potentially multiple steps of updates finish on the server. 
            _commandManager.UpdateAbilityQueue(entity); 
        }
    }
    
    /// <summary>
    /// Attempt to perform the given in-progress ability
    /// </summary>
    private AbilityResult TryPerformAbility(IAbility ability) {
        // TODO-abilities we will need to be careful about this, we don't want abilities to stay active forever if they are not legal.
        (bool success, AbilityResult? result) = ability.AbilityData.AbilityLegal(ability.BaseParameters, ability.Performer);
        if (!success) {
            return result.Value;
        }
        
        // Don't do anything if the performer has been killed 
        if (ability.Performer == null) {
            return AbilityResult.Failed;
        }
        
        // TODO-abilities rename to DoAbilityEffect
        return ability.PerformAbility();
    }

    /// <summary>
    /// Remove the ability from the queue (here on the server)
    /// </summary>
    private void RemoveAbilityFromQueue(GridEntity entity, IAbility ability) {
        IAbility queuedAbility = entity.QueuedAbilities.FirstOrDefault(t => t.UID == ability.UID);
        entity.QueuedAbilities.Remove(queuedAbility);
    }
    
    /// <summary>
    /// Perform the given entity's configured default ability
    /// </summary>
    private void PerformDefaultAbility(GridEntity entity) {
        if (!entity.EntityData.AttackByDefault) return;
        Vector2Int? location = entity.Location;
        if (location == null) return;

        AttackAbilityData data = entity.GetAbilityData<AttackAbilityData>();
        if (data == null) return;
            
        _abilityAssignmentManager.PerformAbility(entity, data, new AttackAbilityParameters {
            TargetFire = false,
            Destination = location.Value
        }, false, true, true);
    }
}