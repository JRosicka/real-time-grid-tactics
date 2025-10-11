using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Base implementation of <see cref="IUpgrade"/>
    /// </summary>
    public class AbstractUpgrade : IUpgrade {
        public UpgradeData Data { get; }
        public UpgradeStatus Status { get; set; }

        public AbstractUpgrade(UpgradeData data) {
            Data = data;
        }

        public void UpgradeFinished() {
            throw new System.NotImplementedException();
        }
        public void RemoveUpgrade() {
            throw new System.NotImplementedException();
        }
        public void ApplyUpgrade(GridEntity entity) {
            throw new System.NotImplementedException();
        }
    }
}