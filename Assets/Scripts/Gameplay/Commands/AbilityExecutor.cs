using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using Sirenix.Utilities;
using UnityEngine;

/// <summary>
/// Handles checking on each active <see cref="GridEntity"/>'s in-progress <see cref="IAbility"/>s and executing them.
/// Tries executing each ability in a controlled manner, notifying clients, updating state, and removing abilities when completed/failed. 
///
/// There should only ever be one of these in a game. In SP, there is just one living in the scene. In MP, only the server
/// has a copy. 
/// </summary>
public class AbilityExecutor : MonoBehaviour {
    
    public float UpdateFrequency;
    
    private bool _initialized;
    private ICommandManager _commandManager;
    private GameEndManager _gameEndManager;
    private AbilityAssignmentManager _abilityAssignmentManager;

    private float _timeUntilNextUpdate;
    public float MatchLength { get; private set; }

    private class QueuedGridEntityUnregister {
        public GridEntity Entity;
        public bool ShowDeathAnimation;
    }
    private readonly List<QueuedGridEntityUnregister> _gridEntitiesToUnRegister = new();

    private readonly List<GridEntity> _dirtyInProgressAbilityEntities = new();
    
    public void Initialize(ICommandManager commandManager, GameEndManager gameEndManager, AbilityAssignmentManager abilityAssignmentManager) {
        _commandManager = commandManager;
        _gameEndManager = gameEndManager;
        _abilityAssignmentManager = abilityAssignmentManager;
        _timeUntilNextUpdate = UpdateFrequency;

        _initialized = true;
    }

    private void Update() {
        if (!_initialized) return;

        MatchLength += Time.deltaTime;
        _timeUntilNextUpdate -= Time.deltaTime;
        if (_timeUntilNextUpdate <= 0) {
            _timeUntilNextUpdate += UpdateFrequency;
            
            // Time for an update
            ExecuteAbilities(false);
            
            // Also perform a check for game end
            _gameEndManager.CheckForGameEnd();
        }
    }

    // Server method
    public void MarkForUnRegistration(GridEntity entity, bool showDeathAnimation) {
        _gridEntitiesToUnRegister.Add(new QueuedGridEntityUnregister {
            Entity = entity,
            ShowDeathAnimation = showDeathAnimation
        });
    }
    
    // Server method
    /// <summary>
    /// Server method. Marks the given entity as having a dirty in-progress abilities set, i.e. it should get an RPC update
    /// for clients towards the end of the next execution loop. Necessary to prevent the following scenario:
    /// - Update in-progress abilities for entity e.g. by clearing the set
    /// - RPC call goes out with this empty set
    /// - Start performing an ability, putting it in the in-progress set
    /// - The RPC call goes through, clearing the set on the clients (and server), removing the ability we just added
    /// </summary>
    public void MarkInProgressAbilitiesDirty(GridEntity entity) {
        if (_dirtyInProgressAbilityEntities.Contains(entity)) return;
        _dirtyInProgressAbilityEntities.Add(entity);
    }

    /// <summary>
    /// Perform a full round of game updates on all entities
    /// </summary>
    public void ExecuteAbilities(bool resetTimeUntilNextUpdate) {
        if (resetTimeUntilNextUpdate) {
            _timeUntilNextUpdate = UpdateFrequency;
        }
        
        // First, execute abilities that involve updating the GridEntityCollection (or that don't depend on that)
        List<GridEntity> allEntities = _commandManager.EntitiesOnGrid.AllEntities();
        foreach (GridEntity entity in allEntities) {
            bool updated = ExecuteAbilitiesForEntity(entity, AbilityExecutionType.PreInteractionGridUpdate);
            if (updated) {
                _dirtyInProgressAbilityEntities.Add(entity);
            }
        }
        
        // Second, execute interaction abilities that rely on (but don't update) entity positions (i.e. attacking)
        foreach (GridEntity entity in allEntities) {
            bool updated = ExecuteAbilitiesForEntity(entity, AbilityExecutionType.Interaction);
            if (updated) {
                _dirtyInProgressAbilityEntities.Add(entity);
            }
        }
        
        // Third, execute any post-interaction abilities that update the GridEntityCollection
        foreach (GridEntity entity in allEntities) {
            bool updated = ExecuteAbilitiesForEntity(entity, AbilityExecutionType.PostInteractionGridUpdate);
            if (updated) {
                _dirtyInProgressAbilityEntities.Add(entity);
            }
        }
        
        // Then, update the in-progress abilities for each client, but only for the entities whose in-progress ability set changed
        _dirtyInProgressAbilityEntities.ForEach(e => _commandManager.UpdateInProgressAbilities(e));
        _dirtyInProgressAbilityEntities.Clear();
        
        // NO ABILITY UPDATES PAST THIS POINT
        
        // Then, apply damage from attacks
        GameManager.Instance.AttackManager.ExecuteDamageApplication();
        
        // Then, unregister any marked entities
        foreach (QueuedGridEntityUnregister unregistration in _gridEntitiesToUnRegister) {
            GameManager.Instance.CommandManager.UnRegisterEntity(unregistration.Entity, unregistration.ShowDeathAnimation);
        }
        _gridEntitiesToUnRegister.Clear();
    }

    /// <summary>
    /// Execute in-progress abilities for a single <see cref="GridEntity"/>. Runs through the abilities and tries to execute
    /// each ability that matches the execution type.
    /// - If the ability matches and is completed, it is cleared from the in-progress abilities set
    /// - Keeps performing abilities of the matching execution type until none remain. Even performs abilities that were
    ///   added during iterating. 
    /// </summary>
    /// <returns>True if the ability set was updated, otherwise false</returns>
    private bool ExecuteAbilitiesForEntity(GridEntity entity, AbilityExecutionType executionType) {
        List<IAbility> abilities = entity.InProgressAbilities;

        // Try to perform default ability
        if (executionType == AbilityExecutionType.Interaction) {
            PerformDefaultAbility(entity);
        }

        // Cycle through the abilities, trying to perform each one until one does not get completed or removed
        bool abilitySetUpdated = false;
        List<int> seenAbilityIDs = new List<int>();
        while (true) {
            // No-op if empty
            if (abilities.IsNullOrEmpty()) {
                break;
            }

            // Pick an ability that matches the type and that we have not already tried to perform this execution
            IAbility nextAbility = abilities.FirstOrDefault(a => a.ExecutionType == executionType && !seenAbilityIDs.Contains(a.UID));
            if (nextAbility == null) {
                break;
            }
            seenAbilityIDs.Add(nextAbility.UID);

            if (abilities.Count >= 20) {
                Debug.LogWarning($"{abilities.Count} abilities currently in progress for {entity.EntityData.ID}. That is way too many.");
            }

            // Try to perform the next ability
            AbilityResult result = TryPerformAbility(nextAbility);
            switch (result) {
                case AbilityResult.CompletedWithoutEffect:
                    RemoveAbility(entity, nextAbility);
                    abilitySetUpdated = true;
                    TryStartPerformingQueuedAbility(nextAbility);
                    break;
                case AbilityResult.CompletedWithEffect:
                    _commandManager.AbilityEffectPerformed(nextAbility);
                    RemoveAbility(entity, nextAbility);
                    abilitySetUpdated = true;
                    TryStartPerformingQueuedAbility(nextAbility);
                    break;
                case AbilityResult.IncompleteWithEffect:
                    _commandManager.AbilityEffectPerformed(nextAbility);
                    break;
                case AbilityResult.IncompleteWithoutEffect:
                    break;
                case AbilityResult.Failed:
                    _commandManager.AbilityFailed(nextAbility);
                    RemoveAbility(entity, nextAbility);
                    abilitySetUpdated = true;
                    TryStartPerformingQueuedAbility(nextAbility);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return abilitySetUpdated;
    }
    
    /// <summary>
    /// Attempt to perform the given in-progress ability
    /// </summary>
    private AbilityResult TryPerformAbility(IAbility ability) {
        AbilityLegality legality = ability.AbilityData.AbilityLegal(ability.BaseParameters, ability.Performer, false, ability.PerformerTeam);
        switch (legality) {
            case AbilityLegality.Legal:
                // Legal, so go on to try to perform the ability
                break;
            case AbilityLegality.NotCurrentlyLegal:
                return AbilityResult.IncompleteWithoutEffect;
            case AbilityLegality.IndefinitelyIllegal:
                return AbilityResult.Failed;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // Don't do anything if the performer has been killed 
        if (ability.Performer == null) {
            return AbilityResult.Failed;
        }
        
        return ability.PerformAbility();
    }

    /// <summary>
    /// Check if the newly completed ability has a queued ability. If it does, start performing it. 
    /// </summary>
    private void TryStartPerformingQueuedAbility(IAbility completedAbility) {
        IAbility queuedAbility = completedAbility.Performer.QueuedAbilities.FirstOrDefault(a => a.QueuedAfterAbilityID == completedAbility.UID);
        if (queuedAbility == null) return;
        
        _abilityAssignmentManager.StartPerformingAbility(queuedAbility.Performer, queuedAbility.AbilityData, 
                queuedAbility.BaseParameters, false, true, false);
    }

    /// <summary>
    /// Remove the ability from the in-progress abilities set (here on the server)
    /// </summary>
    private void RemoveAbility(GridEntity entity, IAbility ability) {
        IAbility abilityInstance = entity.InProgressAbilities.FirstOrDefault(t => t.UID == ability.UID);
        entity.InProgressAbilities.Remove(abilityInstance);
    }
    
    /// <summary>
    /// Perform the given entity's configured default ability
    /// </summary>
    private void PerformDefaultAbility(GridEntity entity) {
        Vector2Int? location = entity.Location;
        if (location == null) return;

        if (entity.EntityData.AttackByDefault) {
            AttackAbilityData data = entity.GetAbilityData<AttackAbilityData>();
            if (data != null) {
                // Don't perform default attack if there are any abilities that block it
                if (!entity.InProgressAbilities.Any(a => a.AbilityData.BlocksDefaultAttack)) {
                    _abilityAssignmentManager.StartPerformingAbility(entity, data, new AttackAbilityParameters {
                        Destination = location.Value
                    }, false, true, false);
                }
            }
        }

        foreach (AbilityDataScriptableObject ability in entity.EntityData.Abilities) {
            if (ability.Content.PerformByDefault) {
                if (entity.InProgressAbilities.All(a => a.AbilityData.GetType() != ability.Content.GetType())) {
                    _abilityAssignmentManager.StartPerformingAbility(entity, ability.Content, new ParadeAbilityParameters {
                        Target = null
                    }, false, true, false);
                }
            }
        }
    }
}