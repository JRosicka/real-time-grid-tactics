using Gameplay.Config.Upgrades;
using UnityEngine;

namespace Gameplay.Entities.Upgrades {
    public class Test1Upgrade : AbstractUpgrade {
        public Test1Upgrade(UpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            Debug.Log("Test 1 upgrade applied");
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            Debug.Log($"Test 1 upgrade applied to entity {friendlyEntity.EntityData.ID}");
        }

        protected override void RemoveGlobalEffect() {
            Debug.Log("Test 1 upgrade removed");
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            Debug.Log($"Test 1 upgrade removed from entity {friendlyEntity.EntityData.ID}");
        }
    }
}