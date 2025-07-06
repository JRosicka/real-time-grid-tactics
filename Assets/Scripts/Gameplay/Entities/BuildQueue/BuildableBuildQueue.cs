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
        
        public event Action<List<BuildAbility>> BuildQueueUpdated;

        public BuildableBuildQueue(GridEntity entity, int maxSize) {
            _entity = entity;
            _maxSize = maxSize;

            entity.AbilityPerformedEvent += (_, _) => DetermineBuildQueue();
            entity.AbilityQueueUpdatedEvent += _ => DetermineBuildQueue();
            entity.CooldownTimerExpiredEvent += (_, _) => DetermineBuildQueue();
            
            DetermineBuildQueue();
        }

        // TODO-abilities: Somehow this just works? Hmm. 
        public List<BuildAbility> Queue { get; private set; } = new();

        public bool HasSpace => Queue.Count < _maxSize;
        
        public void CancelBuild(BuildAbility build) {
            BuildAbility abilityInBuildQueue = Queue.FirstOrDefault(b => b.UID == build.UID);
            if (abilityInBuildQueue == null) {
                Debug.LogError($"Tried to cancel build that wasn't in the queue!");
                return;
            }
            
            GameManager.Instance.CommandManager.CancelAbility(abilityInBuildQueue);
        }

        public void CancelAllBuilds() {
            Queue.ForEach(b => GameManager.Instance.CommandManager.CancelAbility(b));
        }
        
        private void DetermineBuildQueue() {
            List<int> previousBuildQueueIDs = Queue.Select(a => a.UID).ToList();
            
            List<BuildAbility> activeAbilities = _entity.ActiveTimers.Where(t => t.Ability is BuildAbility)
                .Select(t => t.Ability)
                .Cast<BuildAbility>()
                .ToList();
            if (activeAbilities.Count > 1) {
                Debug.LogError("Multiple active build abilities detected, that should not happen");
            }
            List<BuildAbility> queuedAbilities = _entity.QueuedAbilities.Where(a => a is BuildAbility)
                .Cast<BuildAbility>()
                .ToList();
            Queue = activeAbilities.Concat(queuedAbilities).ToList();
            
            List<int> newBuildQueueIDs = Queue.Select(a => a.UID).ToList();
            if (!newBuildQueueIDs.SequenceEqual(previousBuildQueueIDs)) {
                BuildQueueUpdated?.Invoke(Queue);
            }
        }
    }
}