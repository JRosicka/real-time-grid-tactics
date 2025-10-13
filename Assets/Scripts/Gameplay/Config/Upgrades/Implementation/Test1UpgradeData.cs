using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/Test1Upgrade", fileName = "Test1Upgrade", order = 0)]
    public class Test1UpgradeData : UpgradeData {
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new Test1Upgrade(this, team);
        }
    }
}