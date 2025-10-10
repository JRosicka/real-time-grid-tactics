using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles tracking Amber Forge enhancements being available for research.
    /// Client-side. 
    /// </summary>
    public class AmberForgeAvailabilityNotifier {
        public event Action<bool> EnhancementAvailabilityChanged;

        public bool EnhancementAvailable { get; private set; }
        
        private GridEntity _amberForgeEntity;
        private GridEntity _friendlyKingEntity;
        
        /// <summary>
        /// Initialize must be called after registering all starting entities (or at least the kings and amber forge)
        /// </summary>
        public void Initialize() {
            EntityData kingEntityData = GameManager.Instance.Configuration.KingEntityData;
            EntityData amberForgeEntityData = GameManager.Instance.Configuration.AmberForgeEntityData;
            List<GridEntity> allEntities = GameManager.Instance.CommandManager.EntitiesOnGrid.AllEntities();
            _amberForgeEntity = allEntities.FirstOrDefault(e => e.EntityData == amberForgeEntityData);
            
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam is GameTeam.Player1 or GameTeam.Player2) {
                _friendlyKingEntity = allEntities.Where(e => e.EntityData == kingEntityData)
                    .FirstOrDefault(e => e.Team == localTeam);
                
                // Listen for relevant updates
                GameManager.Instance.CommandManager.EntityCollectionChangedEvent += UpdateAvailability;
                PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(localTeam).ResourcesController;
                resourcesController.BalanceChangedEvent += _ => UpdateAvailability();
                if (_amberForgeEntity != null) {
                    _amberForgeEntity.CooldownTimerStartedEvent += AmberForgeCooldownStarted;
                    _amberForgeEntity.CooldownTimerExpiredEvent += AmberForgeCooldownExpired;
                }
            }
        }

        private void UpdateAvailability() {
            bool newAvailability = IsEnhancementAvailable();
            if (newAvailability == EnhancementAvailable) return;
            
            EnhancementAvailable = newAvailability;
            EnhancementAvailabilityChanged?.Invoke(EnhancementAvailable);
        }

        private void AmberForgeCooldownStarted(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            if (ability.PerformerTeam != _friendlyKingEntity.Team) return;
            if (ability is not BuildAbility) return;
            
            UpdateAvailability();
        }

        private void AmberForgeCooldownExpired(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            if (ability.PerformerTeam != _friendlyKingEntity.Team) return;
            if (ability is not BuildAbility) return;
            
            UpdateAvailability();
            
            // A build ability for the local team just expired, so the Amber Forge is available. Check if there are any enhancements left to get. 
            List<UpgradeData> availableEnhancements = AvailableEnhancements(GameManager.Instance.GetPlayerForTeam(ability.PerformerTeam));
            if (availableEnhancements == null || availableEnhancements.Count == 0) return;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("The Amber Forge is available.");
        }

        /// <summary>
        /// Check to see if the friendly king is adjacent to the Amber Forge and whether we have enough money to buy
        /// an available enhancement.
        /// </summary>
        private bool IsEnhancementAvailable() {
            if (!_friendlyKingEntity) return false;
            if (!_amberForgeEntity || _amberForgeEntity.DeadOrDying || _amberForgeEntity.Location == null) return false;

            // Check cooldown timers
            if (_amberForgeEntity.ActiveTimers.Any(t => t.Ability is BuildAbility && t.Team == _friendlyKingEntity.Team)) return false;
            
            // Check friendly king adjacency
            Vector2Int amberForgeLocation = _amberForgeEntity.Location!.Value;
            List<Vector2Int> adjacentCells = GameManager.Instance.GridController.GridData
                .GetAdjacentCells(amberForgeLocation).Select(c => c.Location).ToList();
            Vector2Int? friendlyKingLocation =
                GameManager.Instance.CommandManager.EntitiesOnGrid.LocationOfEntity(_friendlyKingEntity);
            if (friendlyKingLocation == null || !adjacentCells.Contains(friendlyKingLocation.Value)) return false;

            // Check if enhancements are available
            IGamePlayer localPlayer = GameManager.Instance.GetPlayerForTeam(GameManager.Instance.LocalTeam);
            PlayerResourcesController resourcesController = localPlayer.ResourcesController;
            List<UpgradeData> availableEnhancements = AvailableEnhancements(localPlayer);
            if (availableEnhancements == null) return false;
            
            // Check affordability
            return availableEnhancements.Select(e => e.Cost).Any(resourceAmounts => resourcesController.CanAfford(resourceAmounts));
        }

        private List<UpgradeData> AvailableEnhancements(IGamePlayer player) {
            return _amberForgeEntity.GetAbilityData<BuildAbilityData>()?.Buildables
                .Select(b => b.data)
                .Cast<UpgradeData>()
                .Where(u => !player.OwnedPurchasablesController.HasUpgrade(u))
                .ToList();
        }
    }
}