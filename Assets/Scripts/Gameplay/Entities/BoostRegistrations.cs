using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    /// <summary>
    /// Tracks state of sources and boost amounts from <see cref="BoostStructureAbility"/> instances
    /// </summary>
    public class BoostRegistrations {
        public readonly List<(GridEntity, float)> Boosts;

        public BoostRegistrations(List<(GridEntity, float)> boosts) {
            Boosts = boosts;
        }

        public BoostRegistrations() {
            Boosts = new List<(GridEntity, float)>();
        }

        public float GetTotalBoostAmount() {
            if (Boosts.Count == 0) return 1f;
            return Boosts.Select(b => b.Item2).Aggregate((a, b) => a * b);
        }
    }
}