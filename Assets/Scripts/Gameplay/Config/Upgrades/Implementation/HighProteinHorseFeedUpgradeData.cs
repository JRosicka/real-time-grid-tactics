using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/HighProteinHorseFeed", fileName = "High Protein Horse Feed", order = 0)]
    public class HighProteinHorseFeedUpgradeData : UpgradeData {
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new HighProteinHorseFeedUpgrade(this, team);
        }
    }
}