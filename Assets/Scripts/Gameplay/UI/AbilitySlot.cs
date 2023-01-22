using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A slot in the <see cref="AbilityInterface"/> that can visualize an <see cref="IAbility"/> of the selected
    /// <see cref="GridEntity"/>. This slot acts as a button that the player can click to use the ability, and it has a
    /// consistent hotkey. Blank when no entity is selected or that entity does not have a matching ability for the slot. 
    /// </summary>
    public class AbilitySlot : MonoBehaviour {
        [Tooltip("An entity's ability of this channel will get visualized here")]
        public AbilityChannel Channel;
        public string Hotkey;
        public Color SelectableColor;
        public Color UnselectableColor;
        public Color SelectedColor;

        [Header("References")]
        public Image AbilityImage;
        public Image SlotFrame;
        public TMP_Text HotkeyText;
        public AbilityInterface AbilityInterface;

        private IAbilityData _currentAbility;
        private GridEntity _selectedEntity;
        private bool _selectable;
        
        public void SetUpForAbility(IAbilityData data, GridEntity selectedEntity) {
            gameObject.SetActive(true);
            _currentAbility = data;
            _selectedEntity = selectedEntity;

            AbilityImage.sprite = _currentAbility.Icon;
            HotkeyText.text = Hotkey;

            CheckAvailability();
            AddListeners();
        }

        public void Clear() {
            RemoveListeners();
            _currentAbility = null;
            _selectedEntity = null;
            gameObject.SetActive(false);
        }
        
        public void OnButtonClick() {
            if (!_selectable) return;
            AbilityInterface.SelectAbility(this);
        }

        public bool SelectAbility() {
            if (!_selectable) return false;
            
            MarkSelected(true);
            _currentAbility?.SelectAbility(_selectedEntity);
            return true;
        }

        public void MarkSelected(bool selected) {
            if (selected) {
                SlotFrame.color = SelectedColor;
            } else {
                // Resets the color
                CheckAvailability();
            }
        }

        /// <summary>
        /// Shows additional info about an ability when hovering over slot
        /// </summary>
        public void DisplayTooltip() {
            // TODO
        }

        private void CheckAvailability() {
            if (_currentAbility == null || _selectedEntity == null) return;
            
            if (_selectedEntity.CanUseAbility(_currentAbility)) {
                SlotFrame.color = SelectableColor;
                _selectable = true;
            } else {
                SlotFrame.color = UnselectableColor;
                _selectable = false;
            }
        }
        
        #region Listeners

        private void AddListeners() {
            _selectedEntity.CooldownTimerExpiredEvent += OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent += OnAbilityTimersChanged;
        }

        private void RemoveListeners() {
            if (_selectedEntity == null) return;
            
            _selectedEntity.CooldownTimerExpiredEvent -= OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent -= OnAbilityTimersChanged;
        }

        private void OnAbilityTimersChanged(IAbility ability, AbilityCooldownTimer timer) {
            if (timer.ChannelBlockers.Contains(Channel)) {
                CheckAvailability();
            }
        }
        
        private void OnPlayerPurchasedSomething() {
            
        }

        private void OnSomethingThatThePlayerOwnedDied() {
            
        }
        
        #endregion
    }
}