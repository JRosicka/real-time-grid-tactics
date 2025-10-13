using System.Threading.Tasks;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// A <see cref="NetworkableTimer"/> that handles tracking the duration of particular <see cref="IUpgrade"/>s.
    /// When the timer elapses, the server handles removing the upgrade from the collection. 
    /// </summary>
    public class UpgradeDurationTimer : NetworkableTimer {
        private readonly IUpgrade _upgrade;
        
        public UpgradeDurationTimer(IUpgrade upgrade, GameTeam team, float timeRemaining) : base(team, timeRemaining) {
            _upgrade = upgrade;
        }

        protected override Task TryCompleteTimerAsync() {
            GameManager.Instance.CommandManager.MarkUpgradeTimerExpired(_upgrade.Data, Team);
            return Task.CompletedTask;
        }
    }
}