using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    public class WagonUpgrade : AbstractUpgrade<WagonUpgradeData> {
        public WagonUpgrade(WagonUpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // Nothing to do
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            if (friendlyEntity.EntityData != Data.WagonData) return;
            friendlyEntity.UpdateArmor(Data.ArmorBonus);
            friendlyEntity.UpdateCarryCapacity(Data.CapacityBonus);
        }

        protected override void RemoveGlobalEffect() {
            // Nothing to do
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            // Nothing to do
        }

        protected override void TimerStartedLocally() {
            // Nothing to do
        }
    }
}