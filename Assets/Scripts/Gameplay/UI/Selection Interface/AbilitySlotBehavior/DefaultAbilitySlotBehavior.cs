using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
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

        public AbilitySlotInfo AbilitySlotInfo => _abilityData.AbilitySlotInfo;
        public bool IsAvailabilitySensitiveToResources => false;
        public bool CaresAboutAbilityChannels => true;
        public bool CaresAboutInProgressAbilities => false;
        public bool IsAbilityTargetable => _abilityData.Targeted;
        public bool AnyPlayerCanSelect => _abilityData.SelectableForAllPlayers;

        public void SelectSlot() {
            if (_abilityData == null) return;
            
            _abilityData.SelectAbility(_selectedEntity);
            if (_abilityData is ITargetableAbilityData targetableAbilityData) {
                GameManager.Instance.SelectionInterface.TooltipView.ToggleForTargetableAbility(targetableAbilityData, this);
            }
        }

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            // Nothing to do
        }

        public AbilitySlot.AvailabilityResult GetAvailability() {
            if (!_selectedEntity.InteractBehavior!.IsLocalTeam && !_abilityData.SelectableForAllPlayers) {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            AbilityLegality legality = GameManager.Instance.AbilityAssignmentManager.CanEntityUseAbility(_selectedEntity, _abilityData, _abilityData.SelectableWhenBlocked);
            return legality == AbilityLegality.Legal ? AbilitySlot.AvailabilityResult.Selectable : AbilitySlot.AvailabilityResult.Unselectable;
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = _abilityData.Icon;
            secondaryAbilityImage.sprite = null;
            secondaryAbilityImage.gameObject.SetActive(false);
            teamColorsCanvas.sortingOrder = 1;
        }
    }
}