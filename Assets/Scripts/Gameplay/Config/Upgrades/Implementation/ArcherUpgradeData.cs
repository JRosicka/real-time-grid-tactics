using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/ArcherUpgrade", fileName = "Archer Upgrade", order = 0)]
    public class ArcherUpgradeData : UpgradeData {
        public EntityData ArcherData;
        [Tooltip("How much of the original slow terrain multiplier gets applied to the Archer. 0 means it doesn't get applied at all (i.e. slow terrain has no move effect), 1 means it gets applied its original amount.")]
        public float SlowTerrainSpeedModifierIntensity;
        public float AdditionalMovementTimeFromAttacking;
        public string MoveTooltipMessage;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new ArcherUpgrade(this, team);
        }
    }
}