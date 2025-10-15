using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
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

        private AbilitySlotBackgroundView _abilitySlotBackgroundView;

        public AbilitySlotInfo AbilitySlotInfo => _buildAbilityData.AbilitySlotInfo;
        public bool IsAvailabilitySensitiveToResources => true;
        public bool CaresAboutAbilityChannels => false;
        public bool CaresAboutInProgressAbilities => false;
        public bool CaresAboutLeaderPosition => true;
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
                if (!SelectedEntity.BuildQueue.HasSpace(GameManager.Instance.LocalTeam)) {
                    return;
                }
                Vector2Int? selectedEntityLocation = SelectedEntity.Location;
                if (selectedEntityLocation == null) return;
                AbilityAssignmentManager.StartPerformingAbility(SelectedEntity, _buildAbilityData, new BuildAbilityParameters {
                    Buildable = Buildable, 
                    BuildLocation = selectedEntityLocation.Value
                }, true, true, false, overrideTeam:GameManager.Instance.LocalTeam);
            }
        }

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            if (availability == AbilitySlot.AvailabilityResult.Unselectable) {
                string alertMessage;
                if (GameManager.Instance.LocalTeam == GameTeam.Spectator) {
                    alertMessage = "Can not affect the game as a spectator!";
                } else if (FulfillsRequirementsToBuild(out alertMessage)) {
                    alertMessage = "Can not afford.";
                }
                
                GameManager.Instance.AlertTextDisplayer.DisplayAlert(alertMessage);
            } // Otherwise it is unavailable - don't even acknowledge the selection attempt
        }

        public virtual AbilitySlot.AvailabilityResult GetAvailability() {
            if (!SelectedEntity.InteractBehavior!.IsLocalTeam) {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

            UpgradeData upgradeData = Buildable as UpgradeData;
            if (upgradeData != null && (ownedPurchasables.Contains(Buildable) 
                                || player.OwnedPurchasablesController.InProgressUpgrades.Contains(Buildable))) {
                // Upgrade that we already own or are currently building somewhere
                return AbilitySlot.AvailabilityResult.Hidden;
            }

            if (!FulfillsRequirementsToBuild(out _)) {
                // Upgrade that we do not fulfill the requirements for
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            
            AbilityLegality legality = AbilityAssignmentManager.CanEntityUseAbility(SelectedEntity, _buildAbilityData, _buildAbilityData.SelectableWhenBlocked, GameManager.Instance.LocalTeam);
            if (legality == AbilityLegality.Legal && player.ResourcesController.CanAfford(Buildable.Cost)) {
                // This entity can build this and we can afford this
                return AbilitySlot.AvailabilityResult.Selectable;
            }
            
            return AbilitySlot.AvailabilityResult.Unselectable;
        }

        private bool FulfillsRequirementsToBuild(out string whyNot) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity);
            return player.OwnedPurchasablesController.HasRequirementsForPurchase(Buildable, SelectedEntity, out whyNot);
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, AbilitySlotBackgroundView abilitySlotBackground) {
            abilityImage.sprite = Buildable.BaseSpriteIconOverride == null ? Buildable.BaseSprite : Buildable.BaseSpriteIconOverride;

            if (Buildable.TeamColorSprite == null) {
                secondaryAbilityImage.gameObject.SetActive(false);
            } else {
                secondaryAbilityImage.sprite = Buildable.TeamColorSprite;
                secondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(SelectedEntity).Data.TeamColor;
                secondaryAbilityImage.gameObject.SetActive(true);
            }

            if (abilitySlotBackground) {
                IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity);
                abilitySlotBackground.SetUpSlot(player.Data.ColoredButtonData.Normal);
                _abilitySlotBackgroundView = abilitySlotBackground;
            }
        }

        public void SetUpTimerView() {
            if (_abilitySlotBackgroundView && _buildAbilityData.ShowTimerOnSelectionInterface) {
                _abilitySlotBackgroundView.SetUpTimer(SelectedEntity, _buildAbilityData.Channel);
            }
        }

        public void ClearTimerView() {
            if (_abilitySlotBackgroundView) {
                _abilitySlotBackgroundView.UnsubscribeFromTimers();
            }
        }
    }
}