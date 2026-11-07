using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    public class CatapultUpgrade : AbstractUpgrade<CatapultUpgradeData> {
        public CatapultUpgrade(CatapultUpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // Nothing to do
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            if (friendlyEntity.EntityData != Data.CatapultEntity) return;
            friendlyEntity.UpdateRange(friendlyEntity.Range + Data.RangeIncrease);
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