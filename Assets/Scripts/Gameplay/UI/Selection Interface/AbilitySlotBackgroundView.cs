using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// View logic for the background of a <see cref="AbilitySlot"/>
    /// </summary>
    public class AbilitySlotBackgroundView : MonoBehaviour {
        [SerializeField] private Image _slotImage;
        [SerializeField] private Image _grayedOutSlotImage;
        [FormerlySerializedAs("_cooldownTimerView")] 
        [SerializeField] private AbilityTimerCooldownView _abilityTimerView;
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
            gridEntity.AbilityTimerExpiredEvent += AbilityCooldownExpired;         
            
            UpdateTimer();
        }
        
        public void UnsubscribeFromTimers() {
            if (_abilityTimerView != null) {
                _abilityTimerView.UnsubscribeFromTimers();
            }
            if (_gridEntity != null) {
                _gridEntity.AbilityPerformedEvent += AbilityPerformed;
                _gridEntity.AbilityTimerExpiredEvent += AbilityCooldownExpired;
            }
            _gridEntity = null;
            _abilityChannel = null;
        }
        
        private void UpdateTimer() {
            if (AbilityAssignmentManager.IsAbilityChannelOnCooldownForEntity(_gridEntity, _abilityChannel, out List<AbilityTimer> activeAbilityTimers)) {
                GameTeam localTeam = GameManager.Instance.LocalTeam;
                AbilityTimer activeTimerForPlayer = activeAbilityTimers.FirstOrDefault(t => t.Team == localTeam);
                if (activeTimerForPlayer != null) {
                    _abilityTimerView.Initialize(activeTimerForPlayer, false, true, true);
                    return;
                }
            }
            
            _slotImage.gameObject.SetActive(true);
        }

        private void AbilityPerformed(IAbility ability, AbilityTimer abilityTimer) {
            if (ability.AbilityData.Channel == _abilityChannel) {
                UpdateTimer();
            }
        }

        private void AbilityCooldownExpired(IAbility ability, AbilityTimer abilityTimer) {
            if (ability.AbilityData.Channel == _abilityChannel) {
                UpdateTimer();
            }
        }
    }
}