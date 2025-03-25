using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles build-ability-specific behavior for an <see cref="AbilitySlot"/>
    /// </summary>
    public class BuildAbilitySlotBehavior : IAbilitySlotBehavior {
        public PurchasableData Buildable { get; }
        private readonly BuildAbilityData _buildAbilityData;
        protected readonly GridEntity SelectedEntity;

        public AbilitySlotInfo AbilitySlotInfo => _buildAbilityData.AbilitySlotInfo;
        public bool IsAvailabilitySensitiveToResources => true;
        public bool CaresAboutAbilityChannels => false;
        public bool CaresAboutQueuedAbilities => false;
        public bool IsAbilityTargetable => _buildAbilityData.Targeted;
        public bool AnyPlayerCanSelect => false;
        private GridController GridController => GameManager.Instance.GridController;
        private AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;

        public BuildAbilitySlotBehavior(BuildAbilityData buildData, PurchasableData buildable, GridEntity selectedEntity) {
            _buildAbilityData = buildData;
            Buildable = buildable;
            SelectedEntity = selectedEntity;
        }
        
        public virtual void SelectSlot() {
            if (_buildAbilityData.Targetable) {
                GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(_buildAbilityData, SelectedEntity.Team, Buildable);
                GameManager.Instance.SelectionInterface.TooltipView.ToggleForTargetableAbility(_buildAbilityData, this);
                List<Vector2Int> viableTargets = _buildAbilityData.GetViableTargets(SelectedEntity, Buildable);
                if (viableTargets != null) {
                    GridController.UpdateSelectableCells(viableTargets, SelectedEntity);
                }
            } else {
                // Try to perform the build ability, but only if the build queue is not full
                if (!SelectedEntity.BuildQueue.HasSpace) {
                    return;
                }
                Vector2Int? selectedEntityLocation = SelectedEntity.Location;
                if (selectedEntityLocation == null) return;
                AbilityAssignmentManager.PerformAbility(SelectedEntity, _buildAbilityData, new BuildAbilityParameters {
                    Buildable = Buildable, 
                    BuildLocation = selectedEntityLocation.Value
                }, true, true, false);
            }
        }

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            if (availability == AbilitySlot.AvailabilityResult.Unselectable) {
                string alertMessage = FulfillsRequirementsToBuild() ? "Can not afford." : "Requirements not met.";
                GameManager.Instance.AlertTextDisplayer.DisplayAlert(alertMessage);
            } // Otherwise it is unavailable - don't even acknowledge the selection attempt
        }

        public virtual AbilitySlot.AvailabilityResult GetAvailability() {
            if (!SelectedEntity.InteractBehavior!.IsLocalTeam) {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity.Team);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

            UpgradeData upgradeData = Buildable as UpgradeData;
            if (upgradeData != null && (ownedPurchasables.Contains(Buildable) 
                                || player.OwnedPurchasablesController.InProgressUpgrades.Contains(Buildable))) {
                // Upgrade that we already own or are currently building somewhere
                return AbilitySlot.AvailabilityResult.Hidden;
            }

            if (!FulfillsRequirementsToBuild()) {
                // Upgrade that we do not fulfill the requirements for
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            
            if (AbilityAssignmentManager.CanEntityUseAbility(SelectedEntity, _buildAbilityData, _buildAbilityData.SelectableWhenBlocked)
                       && player.ResourcesController.CanAfford(Buildable.Cost)
                       && Buildable.Requirements.All(r => ownedPurchasables.Contains(r))) {
                // This entity can build this and we can afford this
                return AbilitySlot.AvailabilityResult.Selectable;
            }
            
            return AbilitySlot.AvailabilityResult.Unselectable;
        }

        private bool FulfillsRequirementsToBuild() {
            if (Buildable is not UpgradeData upgradeData) return true;
            
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity.Team);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;
            return upgradeData.Requirements.All(r => ownedPurchasables.Contains(r));
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = Buildable.BaseSpriteIconOverride == null ? Buildable.BaseSprite : Buildable.BaseSpriteIconOverride;

            if (Buildable.TeamColorSprite == null) {
                teamColorsCanvas.sortingOrder = 1;
            } else {
                secondaryAbilityImage.sprite = Buildable.TeamColorSprite;
                secondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(SelectedEntity.Team).Data.TeamColor;
                secondaryAbilityImage.gameObject.SetActive(true);
                teamColorsCanvas.sortingOrder = Buildable.DisplayTeamColorOverMainSprite ? 2 : 1;
            }
        }
    }
}