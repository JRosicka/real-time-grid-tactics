using Gameplay.Config.Abilities;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles generic non-ability behavior for an <see cref="AbilitySlot"/>
    /// </summary>
    public class DefaultAbilitySlotBehavior : IAbilitySlotBehavior {
        private readonly IAbilityData _abilityData;
        private readonly GridEntity _selectedEntity;
        
        public DefaultAbilitySlotBehavior(IAbilityData abilityData, GridEntity selectedEntity) {
            _abilityData = abilityData;
            _selectedEntity = selectedEntity;
        }

        public bool IsAvailabilitySensitiveToResources => false;
        public bool CaresAboutAbilityChannels => true;
        public bool IsAbilityTargetable => _abilityData.Targeted;

        public void SelectSlot() {
            _abilityData?.SelectAbility(_selectedEntity);
        }

        public AbilitySlot.AvailabilityResult GetAvailability() {
            if (_abilityData.SelectableWhenBlocked || _selectedEntity.CanUseAbility(_abilityData)) {
                return AbilitySlot.AvailabilityResult.Selectable;
            } else {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = _abilityData.Icon;
            secondaryAbilityImage.sprite = null;
            secondaryAbilityImage.gameObject.SetActive(false);
            teamColorsCanvas.sortingOrder = 1;
        }
    }
}