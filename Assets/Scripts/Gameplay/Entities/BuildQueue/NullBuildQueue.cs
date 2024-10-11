using System;
using System.Collections.Generic;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities.BuildQueue {
    /// <summary>
    /// A <see cref="IBuildQueue"/> implementation for <see cref="GridEntity"/>s that can not build things
    /// </summary>
    public class NullBuildQueue : IBuildQueue {
        public event Action<List<BuildAbility>> BuildQueueUpdated;
        public List<BuildAbility> Queue => new();
        public bool HasSpace => false;
        public void CancelBuild(int index) {
            // Nothing to do
        }
    }
}