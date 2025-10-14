using Gameplay.Config.Upgrades;
using UnityEngine;

namespace Gameplay.Entities.Upgrades {
    public class Test1Upgrade : AbstractUpgrade<Test1UpgradeData> {
        public Test1Upgrade(Test1UpgradeData data, GameTeam team) : base(data, team) {}
        
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
        
        protected override void TimerStartedLocally() {
            // Nothing to do
        }
    }
}