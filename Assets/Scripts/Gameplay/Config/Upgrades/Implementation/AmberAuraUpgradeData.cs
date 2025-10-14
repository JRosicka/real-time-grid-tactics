using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    /// <summary>
    /// Upgrade for reducing cooldown times
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/AmberAura", fileName = "Amber Aura", order = 0)]
    public class AmberAuraUpgradeData : UpgradeData {
        [Tooltip("A value of 1.25 means that timers go at 1.25 times the normal speed")]
        public float TimerSpeedMultiplier;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new AmberAuraUpgrade(this, team);
        }
    }
}