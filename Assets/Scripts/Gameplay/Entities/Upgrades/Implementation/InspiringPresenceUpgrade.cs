using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Upgrade for giving an attack bonus to units adjacent to the King
    /// </summary>
    public class InspiringPresenceUpgrade : AbstractUpgrade<InspiringPresenceUpgradeData> {
        public InspiringPresenceUpgrade(InspiringPresenceUpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // No effect
        }

        private bool AppliesToEntity(GridEntity entity) {
            if (entity.Location == null) return false;
            return GameManager.Instance.LeaderTracker.IsAdjacentToFriendlyLeader(entity.Location.Value, entity.Team);
        }
        
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            // No effect
        }

        protected override void RemoveGlobalEffect() {
            // No effect
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            // No effect
        }
        
        protected override void TimerStartedLocally() {
            // Nothing to do
        }

        public override int GetAttackBonus(GridEntity attackingEntity) {
            return AppliesToEntity(attackingEntity)
                ? Data.BonusDamage
                : 0;
        }

        public override string GetAttackTooltipMessage(GridEntity attackingEntity) {
            return AppliesToEntity(attackingEntity) 
                ? $"Deals {Data.BonusDamage} additional damage from Inspiring Presence." 
                : "";
        }
    }
}