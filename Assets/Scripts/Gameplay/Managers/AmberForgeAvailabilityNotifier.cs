using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles tracking Amber Forge upgrades being available for research.
    /// Client-side. 
    /// </summary>
    public class AmberForgeAvailabilityNotifier {
        public event Action<GridEntity, bool> AmberForgeAvailabilityChanged;
        
        private Dictionary<GridEntity, bool> _amberForgeEntityAvailability;
        private GridEntity _friendlyKingEntity;
        
        /// <summary>
        /// Initialize must be called after registering all starting entities (or at least the kings and amber forge)
        /// </summary>
        public void Initialize() {
            EntityData kingEntityData = GameManager.Instance.Configuration.KingEntityData;
            EntityData amberForgeEntityData = GameManager.Instance.Configuration.AmberForgeEntityData;
            List<GridEntity> allEntities = GameManager.Instance.CommandManager.EntitiesOnGrid.AllEntities();
            _amberForgeEntityAvailability = allEntities.Where(e => e.EntityData == amberForgeEntityData).ToDictionary(e => e, _ => false);
            
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam is GameTeam.Player1 or GameTeam.Player2) {
                _friendlyKingEntity = allEntities.Where(e => e.EntityData == kingEntityData)
                    .FirstOrDefault(e => e.Team == localTeam);
                
                // Listen for relevant updates
                GameManager.Instance.CommandManager.EntityCollectionChangedEvent += UpdateAvailability;
                PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(localTeam).ResourcesController;
                resourcesController.BalanceChangedEvent += _ => UpdateAvailability();
                foreach (GridEntity amberForge in _amberForgeEntityAvailability.Keys) {
                    amberForge.AbilityTimerStartedEvent += AmberForgeAbilityTimerStarted;
                    amberForge.AbilityTimerExpiredEvent += AmberForgeAbilityTimerExpired;
                }

                PlayerOwnedPurchasablesController ownedPurchasables = GameManager.Instance.GetPlayerForTeam(localTeam).OwnedPurchasablesController;
                ownedPurchasables.OwnedPurchasablesChangedEvent += UpdateAvailability;
            }
        }

        private void UpdateAvailability() {
            List<GridEntity> updatedAmberForgeAvailabilities = new List<GridEntity>();
            foreach (KeyValuePair<GridEntity,bool> kvp in _amberForgeEntityAvailability) {
                bool newAvailability = IsAmberForgeUpgradeAvailable(kvp.Key);
                if (newAvailability != kvp.Value) {
                    updatedAmberForgeAvailabilities.Add(kvp.Key);
                }
            }

            foreach (GridEntity amberForge in updatedAmberForgeAvailabilities) {
                bool newAvailability = !_amberForgeEntityAvailability[amberForge];
                _amberForgeEntityAvailability[amberForge] = !_amberForgeEntityAvailability[amberForge];
                AmberForgeAvailabilityChanged?.Invoke(amberForge, newAvailability);
            }
        }

        private void AmberForgeAbilityTimerStarted(IAbility ability, AbilityTimer abilityTimer) {
            if (ability.PerformerTeam != _friendlyKingEntity.Team) return;
            if (ability is not BuildAbility) return;
            
            UpdateAvailability();
        }

        private void AmberForgeAbilityTimerExpired(IAbility ability, AbilityTimer abilityTimer) {
            if (ability.PerformerTeam != _friendlyKingEntity.Team) return;
            if (ability is not BuildAbility) return;
            
            UpdateAvailability();
            
            // A build ability for the local team just expired, so the Amber Forge is available. Check if there are any AF upgrades left to get. 
            List<UpgradeData> availableUpgrades = AvailableAmberForgeUpgrades(ability.Performer, GameManager.Instance.GetPlayerForTeam(ability.PerformerTeam));
            if (availableUpgrades == null || availableUpgrades.Count == 0) return;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("The Amber Forge is available.");
        }

        /// <summary>
        /// Check to see if the friendly king is adjacent to the Amber Forge and whether we have enough money to buy
        /// an available AF upgrade.
        /// </summary>
        private bool IsAmberForgeUpgradeAvailable(GridEntity amberForge) {
            if (!_friendlyKingEntity) return false;
            if (!amberForge || amberForge.DeadOrDying || amberForge.Location == null) return false;

            // Check cooldown timers
            if (amberForge.ActiveTimers.Any(t => t.Ability is BuildAbility && t.Team == _friendlyKingEntity.Team)) return false;
            
            // Check if upgrades are available
            IGamePlayer localPlayer = GameManager.Instance.GetPlayerForTeam(GameManager.Instance.LocalTeam);
            PlayerResourcesController resourcesController = localPlayer.ResourcesController;
            List<UpgradeData> availableUpgrades = AvailableAmberForgeUpgrades(amberForge, localPlayer);
            if (availableUpgrades == null) return false;
            
            // Check requirements (including friendly King adjacency)
            if (availableUpgrades.All(u => !localPlayer.OwnedPurchasablesController.HasRequirementsForPurchase(u, amberForge, out _))) return false;
            
            // Check affordability
            return availableUpgrades.Select(e => e.Cost).Any(resourceAmounts => resourcesController.CanAfford(resourceAmounts));
        }

        private List<UpgradeData> AvailableAmberForgeUpgrades(GridEntity amberForge, IGamePlayer player) {
            return amberForge.GetAbilityData<BuildAbilityData>()?.Buildables
                .Select(b => b.data)
                .Cast<UpgradeData>()
                .Where(u => !player.OwnedPurchasablesController.HasUpgrade(u))
                .ToList();
        }
    }
}