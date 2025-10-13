using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/Test2Upgrade", fileName = "Test2Upgrade", order = 0)]
    public class Test2UpgradeData : UpgradeData {
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new Test2Upgrade(this, team);
        }
    }
}