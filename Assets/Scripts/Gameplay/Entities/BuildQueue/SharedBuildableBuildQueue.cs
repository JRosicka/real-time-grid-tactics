using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities.BuildQueue {
    /// <summary>
    /// A <see cref="IBuildQueue"/> implementation for <see cref="GridEntity"/>s that can build things for both players
    /// simultaneously.
    /// Entirely client-side, not networked - this reacts to events that are communicated to the client (and user inputs),
    /// and any operations are sent to the <see cref="ICommandManager"/>
    /// </summary>
    public class SharedBuildableBuildQueue : IBuildQueue {
        private readonly GridEntity _entity;
        private readonly int _maxSize;
        private readonly Dictionary<GameTeam, List<BuildAbility>> _buildAbilities;
        
        public event Action<GameTeam, List<BuildAbility>> BuildQueueUpdated;

        public SharedBuildableBuildQueue(GridEntity entity, int maxSize) {
            _entity = entity;
            _maxSize = maxSize;

            _buildAbilities = new Dictionary<GameTeam, List<BuildAbility>> {
                { GameTeam.Player1, new List<BuildAbility>() },
                { GameTeam.Player2, new List<BuildAbility>() }
            };

            entity.AbilityPerformedEvent += (_, _) => DetermineBuildQueue();
            entity.InProgressAbilitiesUpdatedEvent += _ => DetermineBuildQueue();
            entity.AbilityTimerExpiredEvent += (_, _) => DetermineBuildQueue();
            
            DetermineBuildQueue();
        }

        // TODO-abilities: Somehow this just works? Hmm. 
        public List<BuildAbility> Queue(GameTeam team) {
            if (team == GameTeam.Spectator) {
                // Just serve the player 1 queue to spectators for now. 
                team = GameTeam.Player1;
            }
            return _buildAbilities[team];
        }

        public bool HasSpace(GameTeam team) => Queue(team).Count < _maxSize;
        
        public void CancelBuild(BuildAbility build, GameTeam team) {
            BuildAbility abilityInBuildQueue = Queue(team).FirstOrDefault(b => b.UID == build.UID);
            if (abilityInBuildQueue == null) {
                Debug.LogError("Tried to cancel build that wasn't in the queue!");
                return;
            }
            
            GameManager.Instance.CommandManager.CancelAbility(abilityInBuildQueue, true);
        }

        public void CancelAllBuilds(GameTeam team) {
            Queue(team).ForEach(b => GameManager.Instance.CommandManager.CancelAbility(b, false));
        }
        
        private void DetermineBuildQueue() {
            DetermineBuildQueueForTeam(GameTeam.Player1);
            DetermineBuildQueueForTeam(GameTeam.Player2);
        }

        private void DetermineBuildQueueForTeam(GameTeam team) {
            List<string> previousBuildQueueIDs = Queue(team).Select(a => a.UID).ToList();
            
            List<BuildAbility> activeAbilities = _entity.ActiveTimers.Where(t => t.Ability is BuildAbility)
                .Where(t => t.Team == team)
                .Select(t => t.Ability)
                .Cast<BuildAbility>()
                .ToList();
            if (activeAbilities.Count > 1) {
                Debug.LogError("Multiple active build abilities detected, that should not happen");
            }
            List<BuildAbility> queuedAbilities = _entity.InProgressAbilities.Where(a => a is BuildAbility)
                .Where(a => a.PerformerTeam == team)
                .Cast<BuildAbility>()
                .ToList();
            _buildAbilities[team] = activeAbilities.Concat(queuedAbilities).ToList();
            
            List<string> newBuildQueueIDs = Queue(team).Select(a => a.UID).ToList();
            if (!newBuildQueueIDs.SequenceEqual(previousBuildQueueIDs)) {
                BuildQueueUpdated?.Invoke(team, Queue(team));
            }
        }
    }
}