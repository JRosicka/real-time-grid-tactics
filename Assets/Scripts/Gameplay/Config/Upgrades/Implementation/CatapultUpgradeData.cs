using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/CatapultUpgrade", fileName = "Catapult Upgrade", order = 0)]
    public class CatapultUpgradeData : UpgradeData {
        public EntityData CatapultEntity;
        public int RangeIncrease;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new CatapultUpgrade(this, team);
        }
    }
}