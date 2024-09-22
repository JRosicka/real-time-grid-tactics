using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
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

    private float _timeUntilNextUpdate;
    
    public void Initialize(ICommandManager commandManager, GameEndManager gameEndManager) {
        _commandManager = commandManager;
        _gameEndManager = gameEndManager;
        _timeUntilNextUpdate = UpdateFrequency;

        _initialized = true;
    }

    private void Update() {
        if (!_initialized) return;

        _timeUntilNextUpdate -= Time.deltaTime;
        if (_timeUntilNextUpdate <= 0) {
            _timeUntilNextUpdate += UpdateFrequency;
            
            // Time for an update, so execute each entity's queue.
            _commandManager.EntitiesOnGrid.AllEntities().ForEach(ExecuteQueue);
            
            // Also perform a check for game end
            _gameEndManager.CheckForGameEnd();
        }
    }

    /// <summary>
    /// Execute the queue for a single <see cref="GridEntity"/>
    /// </summary>
    private void ExecuteQueue(GridEntity entity) {
        List<IAbility> abilityQueue = entity.QueuedAbilities;
        if (abilityQueue.IsNullOrEmpty()) {
            entity.PerformDefaultAbility();
            return;
        }

        // Try to perform the next ability
        IAbility nextAbility = abilityQueue[0];
        if (entity.PerformQueuedAbility(nextAbility)) {
            _commandManager.RemoveAbilityFromQueue(entity, nextAbility); // TODO Hmmm we remove the ability if we determine here on the client that it is performed successfully, but we don't yet know if it will actually be successfully performed on the server. Not sure if there is much to do about that. Well, actually, this only ever gets executed on the server, so maybe we could just skip the Command networking part and just directly do the ability. 
        } else if (!nextAbility.WaitUntilLegal) {
            _commandManager.RemoveAbilityFromQueue(entity, nextAbility);
        }
    }
}