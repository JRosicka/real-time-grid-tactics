using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/WagonUpgrade", fileName = "Wagon Upgrade", order = 0)]
    public class WagonUpgradeData : UpgradeData {
        public EntityData WagonData;
        public int ArmorBonus;
        public int CapacityBonus;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new WagonUpgrade(this, team);
        }
    }
}