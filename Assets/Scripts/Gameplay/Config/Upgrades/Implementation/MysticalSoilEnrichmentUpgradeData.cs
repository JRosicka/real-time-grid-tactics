using Gameplay.Entities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Config.Upgrades {
    /// <summary>
    /// Upgrade for giving more collectible resources for friendly villages
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/Upgrades/MysticalSoilEnrichment", fileName = "Mystical Soil Enrichment", order = 0)]
    public class MysticalSoilEnrichmentUpgradeData : UpgradeData {
        public EntityData VillageEntityData;
        public ResourceAmount ResourceAmountToAdd;
        
        public override IUpgrade CreateUpgrade(GameTeam team) {
            return new MysticalSoilEnrichmentUpgrade(this, team);
        }
    }
}