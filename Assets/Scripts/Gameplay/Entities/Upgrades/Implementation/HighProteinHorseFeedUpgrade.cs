using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    public class HighProteinHorseFeedUpgrade : AbstractUpgrade {
        public HighProteinHorseFeedUpgrade(UpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // Nothing to do
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            // Nothing to do
        }

        protected override void RemoveGlobalEffect() {
            // Nothing to do
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            // Nothing to do
        }
    }
}