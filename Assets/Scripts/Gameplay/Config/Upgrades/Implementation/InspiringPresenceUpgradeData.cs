using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    /// <summary>
    /// Upgrade for giving an attack bonus to units adjacent to the King
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/InspiringPresence", fileName = "Inspiring Presence", order = 0)]
    public class InspiringPresenceUpgradeData : UpgradeData {
        public int BonusDamage;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new InspiringPresenceUpgrade(this, team);
        }
    }
}