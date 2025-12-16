using System;
using System.Collections.Generic;
using Gameplay.Entities;

namespace Gameplay.Config {
    /// <summary>
    /// Represents the set of starting units and their spawn locations for a single player
    /// </summary>
    [Serializable]
    public struct StartingEntitySet {
        public GameTeam Team;
        public List<EntitySpawnData> Entities;
    }
}