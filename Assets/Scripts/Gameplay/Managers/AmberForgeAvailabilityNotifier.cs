using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
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
            }
        }

        private void UpdateAvailability() {
            bool newAvailability = IsEnhancementAvailable();
            if (newAvailability == EnhancementAvailable) return;
            
            EnhancementAvailable = newAvailability;
            EnhancementAvailabilityChanged?.Invoke(EnhancementAvailable);
        }

        private bool IsEnhancementAvailable() {
            if (!_friendlyKingEntity) return false;
            if (!_amberForgeEntity || _amberForgeEntity.DeadOrDying || _amberForgeEntity.Location == null) return false;

            // Check to see if the friendly king is adjacent to the Amber Forge
            Vector2Int amberForgeLocation = _amberForgeEntity.Location.Value;
            List<Vector2Int> adjacentCells = GameManager.Instance.GridController.GridData.GetAdjacentCells(amberForgeLocation).Select(c => c.Location).ToList();
            Vector2Int? friendlyKingLocation = GameManager.Instance.CommandManager.EntitiesOnGrid.LocationOfEntity(_friendlyKingEntity);
            if (friendlyKingLocation == null || !adjacentCells.Contains(friendlyKingLocation.Value)) return false;
            
            // Check to see if we have enough money to buy an enhancement
            PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(GameManager.Instance.LocalTeam).ResourcesController;
            // TODO replace the new list with the list of available enhancements
            return true;
            // return new List<List<ResourceAmount>>().Any(resourceAmounts => resourcesController.CanAfford(resourceAmounts));
        }
    }
}