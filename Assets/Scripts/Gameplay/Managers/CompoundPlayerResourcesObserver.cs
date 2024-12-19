using System;
using System.Collections.Generic;
using Gameplay.Entities;

namespace Gameplay.Managers {
    /// <summary>
    /// <see cref="IPlayerResourcesObserver"/> that tracks multiple players
    /// </summary>
    public class CompoundPlayerResourcesObserver : IPlayerResourcesObserver {
        public event Action<GameTeam, List<ResourceAmount>> BalanceChanged;
        public List<IGamePlayer> ObservedPlayers { get; }

        public CompoundPlayerResourcesObserver(List<IGamePlayer> playersToTrack) {
            ObservedPlayers = playersToTrack;
            playersToTrack.ForEach(p => p.ResourcesController.BalanceChangedEvent += 
                newResourceTotals => TrackedPlayerBalanceChanged(p.Data.Team, newResourceTotals)
            );
        }

        private void TrackedPlayerBalanceChanged(GameTeam team, List<ResourceAmount> newResourceTotals) {
            BalanceChanged?.Invoke(team, newResourceTotals);
        }
    }
}