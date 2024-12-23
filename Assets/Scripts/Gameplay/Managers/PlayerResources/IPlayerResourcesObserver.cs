using System;
using System.Collections.Generic;
using Gameplay.Entities;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles watching events related to one or more <see cref="IGamePlayer"/>s' <see cref="PlayerResourcesController"/>s
    /// </summary>
    public interface IPlayerResourcesObserver {
        event Action<GameTeam, List<ResourceAmount>> BalanceChanged;
        List<IGamePlayer> ObservedPlayers { get; }
    }
}