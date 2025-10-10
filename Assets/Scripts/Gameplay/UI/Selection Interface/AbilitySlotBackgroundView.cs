using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// View logic for the background of a <see cref="AbilitySlot"/>
    /// </summary>
    public class AbilitySlotBackgroundView : MonoBehaviour {
        [SerializeField] private Image _slotImage;
        [SerializeField] private Image _grayedOutSlotImage;
        [SerializeField] private AbilityTimerCooldownView _cooldownTimerView;
        [SerializeField] private bool _startGrayedOut;
        
        private GridEntity _gridEntity;
        private AbilityChannel _abilityChannel;
        
        private AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;
        
        public void SetUpSlot(Sprite slotSprite) {
            _slotImage.sprite = slotSprite;
            _grayedOutSlotImage.sprite = slotSprite;
            _slotImage.gameObject.SetActive(!_startGrayedOut);
        }

        public void SetUpTimer(GridEntity gridEntity, AbilityChannel abilityChannel) {
            _gridEntity = gridEntity;
            _abilityChannel = abilityChannel;
            
            gridEntity.AbilityPerformedEvent += AbilityPerformed;
            gridEntity.CooldownTimerExpiredEvent += AbilityCooldownExpired;         
            
            UpdateTimer();
        }
        
        public void UnsubscribeFromTimers() {
            if (_cooldownTimerView != null) {
                _cooldownTimerView.UnsubscribeFromTimers();
            }
            if (_gridEntity != null) {
                _gridEntity.AbilityPerformedEvent += AbilityPerformed;
                _gridEntity.CooldownTimerExpiredEvent += AbilityCooldownExpired;
            }
            _gridEntity = null;
            _abilityChannel = null;
        }
        
        private void UpdateTimer() {
            if (AbilityAssignmentManager.IsAbilityChannelOnCooldownForEntity(_gridEntity, _abilityChannel, out List<AbilityCooldownTimer> activeCooldownTimers)) {
                GameTeam localTeam = GameManager.Instance.LocalTeam;
                AbilityCooldownTimer activeTimerForPlayer = activeCooldownTimers.FirstOrDefault(t => t.Team == localTeam);
                if (activeTimerForPlayer != null) {
                    _cooldownTimerView.Initialize(activeTimerForPlayer, false, true, true);
                    return;
                }
            }
            
            _slotImage.gameObject.SetActive(true);
        }

        private void AbilityPerformed(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            if (ability.AbilityData.Channel == _abilityChannel) {
                UpdateTimer();
            }
        }

        private void AbilityCooldownExpired(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            if (ability.AbilityData.Channel == _abilityChannel) {
                UpdateTimer();
            }
        }
    }
}