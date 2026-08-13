using System;
using Gameplay.Entities;

namespace Gameplay.Managers {
    /// <summary>
    /// Client manager that tracks when the lock status changes for any entities owned by particular players.
    /// "Locked" here refers to an entity's <see cref="GridEntity.BlockingFriendlyUnitsEntering"/> state.
    /// </summary>
    public class EntityLockTracker {
        public event Action<GameTeam> LockStatusChanged;
        public void TriggerLockStatusEvent(GameTeam team) {
            LockStatusChanged?.Invoke(team);
        }
    }
}