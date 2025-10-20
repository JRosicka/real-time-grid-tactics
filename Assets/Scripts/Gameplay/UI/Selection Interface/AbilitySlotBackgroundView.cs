using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// View logic for the background of a <see cref="AbilitySlot"/>
    /// </summary>
    public class AbilitySlotBackgroundView : MonoBehaviour {
        [SerializeField] private Image _slotImage;
        [SerializeField] private Image _grayedOutSlotImage;
        [FormerlySerializedAs("_cooldownTimerView")] 
        [SerializeField] private AbilityTimerView _abilityTimerView;
        [SerializeField] private ListenerButton _slotButton;
        
        private GridEntity _gridEntity;
        private AbilityChannel _abilityChannel;
        private ColoredButtonData _slotSprites;
        
        private AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;

        private void Start() {
            _slotButton.Pressed += SlotButtonPressed;
            _slotButton.NoLongerPressed += SlotButtonUnPressed;
        }

        public void SetUpSlot(ColoredButtonData slotSprites) {
            _slotSprites = slotSprites;
            _slotImage.sprite = _slotSprites.Normal;
            _grayedOutSlotImage.sprite = _slotSprites.Normal;
            _slotImage.gameObject.SetActive(true);
            
            _slotButton.spriteState = new SpriteState {
                highlightedSprite = _slotSprites.Hovered,
                pressedSprite = _slotSprites.Pressed,
                selectedSprite = _slotSprites.Normal,
                disabledSprite = _slotSprites.Pressed
            };
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

        private void SlotButtonPressed() {
            _slotImage.sprite = _slotSprites.Pressed;
        }
        
        private void SlotButtonUnPressed() {
            _slotImage.sprite = _slotSprites.Normal;
        }
    }
}