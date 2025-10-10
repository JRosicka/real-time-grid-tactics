using System;
using System.Collections.Generic;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities.BuildQueue {
    /// <summary>
    /// A <see cref="IBuildQueue"/> implementation for <see cref="GridEntity"/>s that can not build things
    /// </summary>
    public class NullBuildQueue : IBuildQueue {
        public event Action<GameTeam, List<BuildAbility>> BuildQueueUpdated;
        List<BuildAbility> IBuildQueue.Queue(GameTeam team) => new();
        bool IBuildQueue.HasSpace(GameTeam team) => false;
        public void CancelBuild(BuildAbility build, GameTeam team) {
            // Nothing to do
        }
        public void CancelAllBuilds(GameTeam team) {
            // Nothing to do
        }
    }
}