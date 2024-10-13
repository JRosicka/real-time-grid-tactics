using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A slot in the <see cref="AbilityInterface"/> that can visualize an <see cref="IAbility"/> of the selected
    /// <see cref="GridEntity"/>. This slot acts as a button that the player can click to use the ability, and it has a
    /// consistent hotkey. Blank when no entity is selected or that entity does not have a matching ability for the slot. 
    /// </summary>
    public class AbilitySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public enum AvailabilityResult {
            // The slot's associated action will never be available again
            NoLongerAvailable,
            // The slot is currently selectable
            Selectable,
            // The slot is not currently selectable
            Unselectable
        }

        public AbilitySlotLocation SlotLocation;
        public string Hotkey;
        public Color SelectableColor;
        public Color UnselectableColor;
        public Color SelectedColor;
        public float ButtonDeselectDelay = .5f;
        
        [Header("References")]
        public Image AbilityImage;
        public Image SecondaryAbilityImage;    // For build target color icon
        public Image SlotFrame;
        public TMP_Text HotkeyText;
        public AbilityInterface AbilityInterface;
        public Canvas TeamColorsCanvas;

        private GridEntity _selectedEntity;
        private bool _selected;
        private bool _shouldDeselectWhenTimerElapses;

        /// <summary>
        /// Delegation of implementation-specific behaviour conducted through here
        /// </summary>
        public IAbilitySlotBehavior SlotBehavior { get; private set; }

        public bool Selectable { get; private set; }

        private PlayerResourcesController LocalResourcesController => GameManager.Instance.LocalPlayer.ResourcesController;

        /// <summary>
        /// Update this ability slot to set its state/appearance
        /// </summary>
        public void SetUpSlot(IAbilitySlotBehavior slotBehavior, GridEntity selectedEntity) {
            SlotBehavior = slotBehavior;
            _selectedEntity = selectedEntity;
            slotBehavior.SetUpSprites(AbilityImage, SecondaryAbilityImage, TeamColorsCanvas);
            
            gameObject.SetActive(true);
            
            HotkeyText.text = Hotkey;
            
            CheckAvailability();
            AddListeners();
        }

        public void Clear() {
            RemoveListeners();
            _shouldDeselectWhenTimerElapses = false;

            SlotBehavior = null;
            _selectedEntity = null;
            _selected = false;
            Selectable = false;
            gameObject.SetActive(false);
        }
        
        public void OnButtonClick() {
            if (!Selectable) return;
            AbilityInterface.SelectAbility(this);
        }

        public void SelectAbility() {
            if (!Selectable) return;
            
            MarkSelected(true);
            SlotBehavior?.SelectSlot();
        }

        public void MarkSelected(bool selected) {
            _selected = selected;
            _shouldDeselectWhenTimerElapses = false;
            if (selected) {
                SlotFrame.color = SelectedColor;
                if (SlotBehavior != null && SlotBehavior.IsAbilityTargetable) {
                    // We want this slot to keep appearing as selected until we do something else, so don't auto-unmark it.
                } else {
                    StartCoroutine(DeselectLater());
                }

            } else {
                // Resets the color
                CheckAvailability();
            }
        }

        /// <summary>
        /// Deselect this slot in a bit so that there is an active "this is selected" look right after selecting.
        /// TODO: This doesn't really do anything right now since MarkSelectable(false) is called immediately after due to the ability being performed. If we care about this, then might be best to do some sort of animation or no-op the MarkSelectable(false) when this is active or something. 
        /// </summary>
        /// <returns></returns>
        private IEnumerator DeselectLater() {
            _shouldDeselectWhenTimerElapses = true;
            
            yield return new WaitForSeconds(ButtonDeselectDelay);

            // Only deselect if we have not done anything else meaningful with this slot while waiting
            if (_shouldDeselectWhenTimerElapses) {
                MarkSelectable(false);
                MarkSelected(false);
            }
        }

        /// <summary>
        /// Shows additional info about an ability when hovering over slot
        /// </summary>
        public void DisplayTooltip() {
            // TODO
        }

        private void CheckAvailability() {
            if (SlotBehavior == null || _selectedEntity == null) return;
            
            AvailabilityResult availability = SlotBehavior.GetAvailability();
            switch (availability) {
                case AvailabilityResult.NoLongerAvailable:
                    Clear();
                    break;
                case AvailabilityResult.Selectable:
                    MarkSelectable(true);
                    break;
                case AvailabilityResult.Unselectable:
                    MarkSelectable(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (_selected && Selectable) {
                // Re-set the appearance to "selected" if we were selected and still can be
                SlotFrame.color = SelectedColor;
            }
        }

        private void MarkSelectable(bool available) {
            SlotFrame.color = available ? SelectableColor : UnselectableColor;
            Selectable = available;
        }
        
        #region Listeners

        private void AddListeners() {
            if (_selectedEntity == null) return;
            
            _selectedEntity.CooldownTimerExpiredEvent += OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent += OnAbilityTimersChanged;
            LocalResourcesController.BalanceChangedEvent += OnPlayerResourcesBalanceChanged;
            GameManager.Instance.LocalPlayer.OwnedPurchasablesController.OwnedPurchasablesChangedEvent += OnPlayerOwnedEntitiesChanged;
        }

        private void RemoveListeners() {
            if (_selectedEntity == null) return;
            
            _selectedEntity.CooldownTimerExpiredEvent -= OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent -= OnAbilityTimersChanged;
            LocalResourcesController.BalanceChangedEvent -= OnPlayerResourcesBalanceChanged;
            GameManager.Instance.LocalPlayer.OwnedPurchasablesController.OwnedPurchasablesChangedEvent -= OnPlayerOwnedEntitiesChanged;
        }

        private void OnAbilityTimersChanged(IAbility ability, AbilityCooldownTimer timer) {
            if (SlotBehavior == null) return; 
            if (!SlotBehavior.CaresAboutAbilityChannels || timer.Ability.AbilityData.SlotLocation == SlotLocation) {
                CheckAvailability();
            }
        }
        
        /// <summary>
        /// When the player gains or spends resources
        /// </summary>
        private void OnPlayerResourcesBalanceChanged(List<ResourceAmount> resourceAmounts) {
            if (SlotBehavior == null) return; 
            if (SlotBehavior.IsAvailabilitySensitiveToResources) {
                CheckAvailability();
            }
        }
        
        /// <summary>
        /// When the player gains a new entity or an entity of theirs dies
        /// </summary>
        private void OnPlayerOwnedEntitiesChanged() {
            CheckAvailability();
        }
        
        #endregion

        public void OnPointerEnter(PointerEventData eventData) {
            if (SlotBehavior == null) return;
            AbilityInterface.TooltipView.ToggleForHoveredAbility(SlotBehavior.AbilityData, SlotBehavior);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (SlotBehavior == null) return; 
            AbilityInterface.TooltipView.ToggleForHoveredAbility(null, null);
        }
    }
}