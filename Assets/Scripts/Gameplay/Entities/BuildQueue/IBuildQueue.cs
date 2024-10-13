using System;
using System.Collections.Generic;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities.BuildQueue {
    /// <summary>
    /// A client-side logic class for tracking the state of building/queued builds for a <see cref="GridEntity"/>.
    /// Does not actually store/update data - instead relies on <see cref="GridEntity.QueuedAbilities"/>.
    /// </summary>
    public interface IBuildQueue {
        event Action<List<BuildAbility>> BuildQueueUpdated; 
        List<BuildAbility> Queue { get; }
        bool HasSpace { get; }
        void CancelBuild(BuildAbility build);
    }
}