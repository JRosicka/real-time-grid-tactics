using Audio;
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
        
        private AbilitySlotBackgroundView _abilitySlotBackgroundView;
        
        public DefaultAbilitySlotBehavior(IAbilityData abilityData, GridEntity selectedEntity) {
            _abilityData = abilityData;
            _selectedEntity = selectedEntity;
        }

        public AbilitySlotInfo AbilitySlotInfo => _abilityData.AbilitySlotInfo;
        public bool IsAvailabilitySensitiveToResources => false;
        public bool CaresAboutAbilityChannels => true;
        public bool CaresAboutInProgressAbilities => false;
        public bool CaresAboutLeaderPosition => false;
        public bool IsAbilityTargetable => _abilityData.Targeted;
        public bool AnyPlayerCanSelect => _abilityData.SelectableForAllPlayers;

        public void SelectSlot(bool newlySelected) {
            if (_abilityData == null) return;
            
            if (newlySelected) {
                GameAudio.Instance.AbilitySelectSound(_abilityData);
            }

            _abilityData.SelectAbility(_selectedEntity);
            if (_abilityData is ITargetableAbilityData targetableAbilityData) {
                GameManager.Instance.SelectionInterface.TooltipView.ToggleForTargetableAbility(targetableAbilityData, this);
                GameManager.Instance.GridIconDisplayer.DisplayOverCurrentHoveredCell(targetableAbilityData);
            }
        }

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            // Nothing to do
        }

        public AbilitySlot.AvailabilityResult GetAvailability() {
            if (!_selectedEntity.InteractBehavior!.IsLocalTeam && !_abilityData.SelectableForAllPlayers) {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
            AbilityLegality legality = GameManager.Instance.AbilityAssignmentManager.CanEntityUseAbility(_selectedEntity, _abilityData, _abilityData.SelectableWhenBlocked, GameManager.Instance.LocalTeam);
            return legality == AbilityLegality.Legal ? AbilitySlot.AvailabilityResult.Selectable : AbilitySlot.AvailabilityResult.Unselectable;
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, AbilitySlotBackgroundView abilitySlotBackground) {
            abilityImage.sprite = _abilityData.Icon;
            secondaryAbilityImage.sprite = null;
            secondaryAbilityImage.gameObject.SetActive(false);

            if (abilitySlotBackground) {
                _abilitySlotBackgroundView = abilitySlotBackground;
                abilitySlotBackground.SetUpSlot(_abilityData.SlotSprites);
            }
        }

        public void SetUpTimerView() {
            if (_abilitySlotBackgroundView && _abilityData.ShowTimerOnSelectionInterface) {
                _abilitySlotBackgroundView.SetUpTimer(_selectedEntity, _abilityData.Channel);
            }
        }

        public void ClearTimerView() {
            if (_abilitySlotBackgroundView) {
                _abilitySlotBackgroundView.UnsubscribeFromTimers();
            }
        }
    }
}