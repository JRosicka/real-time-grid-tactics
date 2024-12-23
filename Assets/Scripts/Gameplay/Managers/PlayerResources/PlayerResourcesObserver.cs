using System;
using System.Collections.Generic;
using Gameplay.Entities;

namespace Gameplay.Managers {
    /// <summary>
    /// <see cref="IPlayerResourcesObserver"/> that tracks a single player
    /// </summary>
    public class PlayerResourcesObserver : IPlayerResourcesObserver {
        public event Action<GameTeam, List<ResourceAmount>> BalanceChanged;
        public List<IGamePlayer> ObservedPlayers => new List<IGamePlayer> { _trackedPlayer };

        private readonly IGamePlayer _trackedPlayer;

        public PlayerResourcesObserver(IGamePlayer playerToTrack) {
            _trackedPlayer = playerToTrack;
            playerToTrack.ResourcesController.BalanceChangedEvent += TrackedPlayerBalanceChanged;
        }

        private void TrackedPlayerBalanceChanged(List<ResourceAmount> newResourceTotals) {
            BalanceChanged?.Invoke(_trackedPlayer.Data.Team, newResourceTotals);
        }
    }
}