using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Upgrade for reducing cooldown times
    /// </summary>
    public class AmberAuraUpgrade : AbstractUpgrade<AmberAuraUpgradeData> {
        public AmberAuraUpgrade(AmberAuraUpgradeData data, GameTeam team) : base(data, team) { }
        
        protected override void ApplyGlobalEffect() {
            // No effect
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            friendlyEntity.SetTimerMultiplier(Data.TimerSpeedMultiplier);
        }
        protected override void RemoveGlobalEffect() {
            // No effect
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            friendlyEntity.SetTimerMultiplier(1f);
        }

        protected override void TimerStartedLocally() { 
            UpgradeTimer.ExpiredEvent += UpgradeExpired;
        }

        private void UpgradeExpired(bool _) {
            if (Team != GameManager.Instance.LocalTeam) return;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("Amber Aura has expired.");
        }
    }
}