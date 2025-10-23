using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    public class ArcherUpgrade : AbstractUpgrade<ArcherUpgradeData> {
        public ArcherUpgrade(ArcherUpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // Nothing to do
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            if (friendlyEntity.EntityData != Data.ArcherData) return;
            friendlyEntity.SetSlowMoveSpeedMultiplier(Data.SlowTerrainSpeedModifierIntensity);
            friendlyEntity.SetAdditionalMovementTimeFromAttacking(Data.AdditionalMovementTimeFromAttacking);
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
        
        public override string GetMoveTooltipMessage(GridEntity movingEntity) {
            if (movingEntity.EntityData != Data.ArcherData) return "";
            return Data.MoveTooltipMessage;
        }
    }
}