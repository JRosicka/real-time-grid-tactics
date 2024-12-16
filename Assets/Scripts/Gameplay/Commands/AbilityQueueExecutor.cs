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
            PerformDefaultAbility(entity);
            return;
        }

        // Try to perform the next ability
        IAbility nextAbility = abilityQueue[0];
        if (PerformQueuedAbility(entity, nextAbility)) {
            _commandManager.RemoveAbilityFromQueue(entity, nextAbility); // TODO Hmmm we remove the ability if we determine here on the client that it is performed successfully, but we don't yet know if it will actually be successfully performed on the server. Not sure if there is much to do about that. Well, actually, this only ever gets executed on the server, so maybe we could just skip the Command networking part and just directly do the ability. 
        } else if (!nextAbility.WaitUntilLegal) {
            _commandManager.RemoveAbilityFromQueue(entity, nextAbility);
        }
    }
    
    private bool PerformQueuedAbility(GridEntity entity, IAbility ability) {
        if (!ability.AbilityData.AbilityLegal(ability.BaseParameters, ability.Performer)) {
            entity.AbilityFailed(ability.AbilityData);
            return false;
        }
            
        _commandManager.PerformAbility(ability, false, false);
        return true;
    }
    
    private void PerformDefaultAbility(GridEntity entity) {
        if (!entity.EntityData.AttackByDefault) return;
        Vector2Int? location = entity.Location;
        if (location == null) return;
            
        AttackAbilityData data = (AttackAbilityData) entity.EntityData.Abilities
            .FirstOrDefault(a => a.Content is AttackAbilityData)?.Content;
        if (data == null) return;
            
        _abilityAssignmentManager.PerformAbility(entity, data, new AttackAbilityParameters {
            TargetFire = false,
            Destination = location.Value
        }, true);
    }
}