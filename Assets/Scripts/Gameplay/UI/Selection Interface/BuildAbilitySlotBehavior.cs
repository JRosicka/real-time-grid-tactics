using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles build-ability-specific behavior for an <see cref="AbilitySlot"/>
    /// </summary>
    public class BuildAbilitySlotBehavior : IAbilitySlotBehavior {
        private readonly BuildAbilityData _buildAbilityData;
        private readonly PurchasableData _buildable;
        private readonly GridEntity _selectedEntity;
        
        public bool IsAvailabilitySensitiveToResources => true;
        public bool CaresAboutAbilityChannels => false;
        public bool IsAbilityTargetable => _buildAbilityData.Targeted;

        public BuildAbilitySlotBehavior(BuildAbilityData buildData, PurchasableData buildable, GridEntity selectedEntity) {
            _buildAbilityData = buildData;
            _buildable = buildable;
            _selectedEntity = selectedEntity;
        }
        
        public void SelectSlot() {
            if (_buildAbilityData.Targetable) {
                GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(_buildAbilityData, _buildable);
            } else {
                // Try to perform the build ability
                _selectedEntity.PerformAbility(_buildAbilityData, new BuildAbilityParameters {
                    Buildable = _buildable, 
                    BuildLocation = _selectedEntity.Location
                }, false);
            }
        }

        public AbilitySlot.AvailabilityResult GetAvailability() {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

            if (_buildable is UpgradeData && ownedPurchasables.Contains(_buildable)) {
                // Upgrade that we already own
                return AbilitySlot.AvailabilityResult.NoLongerAvailable;
            } else if (_selectedEntity.CanUseAbility(_buildAbilityData) 
                       && GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam).ResourcesController.CanAfford(_buildable.Cost)
                       && _buildable.Requirements.All(r => ownedPurchasables.Contains(r))) {
                // This entity can build this and we can afford this
                return AbilitySlot.AvailabilityResult.Selectable;
            } else {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = _buildable.BaseSpriteIconOverride == null ? _buildable.BaseSprite : _buildable.BaseSpriteIconOverride;

            if (_buildable.TeamColorSprite == null) {
                teamColorsCanvas.sortingOrder = 1;
            } else {
                secondaryAbilityImage.sprite = _buildable.TeamColorSprite;
                secondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam).Data.TeamColor;
                secondaryAbilityImage.gameObject.SetActive(true);
                teamColorsCanvas.sortingOrder = _buildable.DisplayTeamColorOverMainSprite ? 2 : 1;
            }
        }
    }
}