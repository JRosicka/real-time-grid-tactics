using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities.BuildQueue {
    /// <summary>
    /// A <see cref="IBuildQueue"/> implementation for <see cref="GridEntity"/>s that can actually build stuff.
    /// Entirely client-side, not networked - this reacts to events that are communicated to the client (and user inputs),
    /// and any operations are sent to the <see cref="ICommandManager"/>
    /// </summary>
    public class BuildableBuildQueue : IBuildQueue {
        private readonly GridEntity _entity;
        private readonly int _maxSize;
        private List<BuildAbility> _buildQueue = new List<BuildAbility>();
        
        public event Action<GameTeam, List<BuildAbility>> BuildQueueUpdated;

        public BuildableBuildQueue(GridEntity entity, int maxSize) {
            _entity = entity;
            _maxSize = maxSize;

            entity.AbilityPerformedEvent += (_, _) => DetermineBuildQueue();
            entity.InProgressAbilitiesUpdatedEvent += _ => DetermineBuildQueue();
            entity.AbilityTimerExpiredEvent += (_, _) => DetermineBuildQueue();
            
            DetermineBuildQueue();
        }

        // TODO-abilities: Somehow this just works? Hmm. 
        public List<BuildAbility> Queue(GameTeam team) => _buildQueue;

        public bool HasSpace(GameTeam team) => _buildQueue.Count < _maxSize;
        
        public void CancelBuild(BuildAbility build, GameTeam team) {
            BuildAbility abilityInBuildQueue = _buildQueue.FirstOrDefault(b => b.UID == build.UID);
            if (abilityInBuildQueue == null) {
                Debug.LogError($"Tried to cancel build that wasn't in the queue!");
                return;
            }
            
            GameManager.Instance.CommandManager.CancelAbility(abilityInBuildQueue);
        }

        public void CancelAllBuilds(GameTeam team) {
            if (team != _entity.Team) return;
            _buildQueue.ForEach(b => GameManager.Instance.CommandManager.CancelAbility(b));
        }
        
        private void DetermineBuildQueue() {
            List<int> previousBuildQueueIDs = _buildQueue.Select(a => a.UID).ToList();
            
            List<BuildAbility> activeAbilities = _entity.ActiveTimers.Where(t => t.Ability is BuildAbility)
                .Select(t => t.Ability)
                .Cast<BuildAbility>()
                .ToList();
            if (activeAbilities.Count > 1) {
                Debug.LogError("Multiple active build abilities detected, that should not happen");
            }
            List<BuildAbility> queuedAbilities = _entity.InProgressAbilities.Where(a => a is BuildAbility)
                .Cast<BuildAbility>()
                .ToList();
            _buildQueue = activeAbilities.Concat(queuedAbilities).ToList();
            
            List<int> newBuildQueueIDs = _buildQueue.Select(a => a.UID).ToList();
            if (!newBuildQueueIDs.SequenceEqual(previousBuildQueueIDs)) {
                BuildQueueUpdated?.Invoke(_entity.Team, _buildQueue);
            }
        }
    }
}