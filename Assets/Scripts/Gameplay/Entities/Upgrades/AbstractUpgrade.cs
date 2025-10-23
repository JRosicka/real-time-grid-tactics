using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    /// <summary>
    /// Base implementation of <see cref="IUpgrade"/>
    /// </summary>
    public abstract class AbstractUpgrade<T> : IUpgrade where T : UpgradeData {
        protected readonly T Data;
        public UpgradeData UpgradeData => Data;
        public UpgradeStatus Status { get; set; }
        public UpgradeDurationTimer UpgradeTimer { get; private set; }
        
        protected readonly GameTeam Team;

        protected AbstractUpgrade(T data, GameTeam team) {
            Data = data;
            Team = team;
        }

        #region Server methods
        
        public void UpgradeFinished() {
            ApplyGlobalEffect();
            if (Data.ApplyToGridEntitiesUponCompletion) {
                List<GridEntity> allFriendlyEntities = GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(Team);
                allFriendlyEntities.ForEach(ApplyUpgrade);
            }
        }

        public void RemoveUpgrade() {
            RemoveGlobalEffect();
            if (Data.ApplyToGridEntitiesUponCompletion || Data.ApplyToGridEntitiesWhenTheySpawn) {
                FriendlyEntities.ForEach(RemoveUpgrade);
            }
        }
        
        /// <summary>
        /// One-time effect that gets applied globally (rather than via scanning each GridEntity) when the upgrade completes.
        /// </summary>
        protected abstract void ApplyGlobalEffect();
        /// <summary>
        /// Applies the upgrade effect to the given friendly GridEntity if relevant.
        /// Server method.
        /// </summary>
        public abstract void ApplyUpgrade(GridEntity friendlyEntity);

        /// <summary>
        /// One-time effect that gets removed globally (rather than via scanning each GridEntity) when the upgrade completes.
        /// </summary>
        protected abstract void RemoveGlobalEffect();
        /// <summary>
        /// Removes the upgrade effect from the given friendly GridEntity if relevant.
        /// Server method.
        /// </summary>
        public abstract void RemoveUpgrade(GridEntity friendlyEntity);

        protected abstract void TimerStartedLocally();
        
        #endregion
        
        // RPC client method
        public void UpdateStatus(UpgradeStatus newStatus) {
            if (Status == newStatus) return;
            
            Status = newStatus;
            if (newStatus == UpgradeStatus.Owned && Data.Timed) {
                StartUpgradeTimer();
            }

            if (Data.ApplyToGridEntitiesUponCompletion && newStatus == UpgradeStatus.Owned) {
                FriendlyEntities.ForEach(e => e.UpgradeApplied(this));
            } else if ((Data.ApplyToGridEntitiesUponCompletion || Data.ApplyToGridEntitiesWhenTheySpawn) 
                       && newStatus == UpgradeStatus.NeitherOwnedNorInProgress) {
                FriendlyEntities.ForEach(e => e.UpgradeRemoved(this));
            }
        }

        private void StartUpgradeTimer() {
            UpgradeTimer = new UpgradeDurationTimer(this, Team, Data.ExpirationSeconds);
            TimerStartedLocally();
        }

        public bool ExpireUpgradeTimer() {
            if (UpgradeTimer == null) return false;
            
            UpgradeTimer.Expire(false);
            UpgradeTimer = null;
            
            if (Data.Repeatable) {
                GameManager.Instance.CommandManager.UpdateUpgradeStatus(Data, Team, UpgradeStatus.NeitherOwnedNorInProgress);
            }
            
            return true;
            // TODO trigger event for view logic if/when we add that
        }

        public void UpdateTimer(float deltaTime) {
            UpgradeTimer?.UpdateTimer(deltaTime);
        }
        
        public virtual int GetAttackBonus(GridEntity attackingEntity) => 0;

        public virtual string GetAttackTooltipMessage(GridEntity attackingEntity) => null;
        public virtual string GetMoveTooltipMessage(GridEntity moveEntity) => null;

        private List<GridEntity> FriendlyEntities => GameManager.Instance.CommandManager.EntitiesOnGrid.ActiveEntitiesForTeam(Team);
    }
}