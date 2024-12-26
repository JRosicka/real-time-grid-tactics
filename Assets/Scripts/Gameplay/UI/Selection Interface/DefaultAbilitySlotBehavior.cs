using Gameplay.Config.Abilities;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles generic non-ability behavior for an <see cref="AbilitySlot"/>
    /// </summary>
    public class DefaultAbilitySlotBehavior : IAbilitySlotBehavior {
        private readonly GridEntity _selectedEntity;
        
        public DefaultAbilitySlotBehavior(IAbilityData abilityData, GridEntity selectedEntity) {
            AbilityData = abilityData;
            _selectedEntity = selectedEntity;
        }

        public IAbilityData AbilityData { get; }
        public bool IsAvailabilitySensitiveToResources => false;
        public bool CaresAboutAbilityChannels => true;
        public bool IsAbilityTargetable => AbilityData.Targeted;

        public void SelectSlot() {
            if (AbilityData == null) return;
            
            AbilityData.SelectAbility(_selectedEntity);
            if (AbilityData is ITargetableAbilityData targetableAbilityData) {
                GameManager.Instance.SelectionInterface.TooltipView.ToggleForTargetableAbility(targetableAbilityData, this);
            }
        }

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            // Nothing to do
        }

        public AbilitySlot.AvailabilityResult GetAvailability() {
            return GameManager.Instance.AbilityAssignmentManager.CanEntityUseAbility(_selectedEntity, AbilityData, AbilityData.SelectableWhenBlocked) 
                ? AbilitySlot.AvailabilityResult.Selectable 
                : AbilitySlot.AvailabilityResult.Unselectable;
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = AbilityData.Icon;
            secondaryAbilityImage.sprite = null;
            secondaryAbilityImage.gameObject.SetActive(false);
            teamColorsCanvas.sortingOrder = 1;
        }
    }
}