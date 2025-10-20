using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// A slot in the <see cref="AbilityInterface"/> that can visualize an <see cref="IAbility"/> of the selected
    /// <see cref="GridEntity"/>. This slot acts as a button that the player can click to use the ability, and it has a
    /// consistent hotkey. Blank when no entity is selected or that entity does not have a matching ability for the slot. 
    /// </summary>
    public class AbilitySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public enum AvailabilityResult {
            // The slot's associated action is not available again
            Hidden,
            // The slot is currently selectable
            Selectable,
            // The slot is visible but not currently selectable
            Unselectable
        }

        public AbilitySlotLocation SlotLocation;
        public string Hotkey;
        public Color SelectableColor;
        public Color UnselectableColor;
        public Color SelectedColor;
        public float ButtonDeselectDelay = .5f;
        public Color SelectedIconColor;
        public Color DeselectedIconColor;
        public Vector2 IconUpPosition;
        public Vector2 IconDownPosition;
        public float ButtonDeselectBufferForHotkeyDetection = .1f;
        
        [Header("References")]
        public Image AbilityImage;
        public Image SecondaryAbilityImage;    // For build target color icon
        public AbilitySlotBackgroundView AbilitySlotBackgroundView;
        public Image SlotFrame;
        public TMP_Text HotkeyText;
        public AbilityInterface AbilityInterface;
        public CanvasGroup CanvasGroup;
        public ListenerButton SlotButton;
        public GameObject IconsGroup;

        private GridEntity _selectedEntity;
        private bool _selected;
        private bool _shouldDeselectWhenTimerElapses;
        private float _lastAbilityClickTime;

        /// <summary>
        /// Delegation of implementation-specific behaviour conducted through here
        /// </summary>
        private IAbilitySlotBehavior _slotBehavior;

        public AvailabilityResult Availability { get; private set; }

        public bool AnyPlayerCanSelect => _slotBehavior.AnyPlayerCanSelect;

        private void Start() {
            SlotButton.Pressed += ToggleClicked;
            SlotButton.NoLongerPressed += ToggleUnClicked;
        }

        /// <summary>
        /// Update this ability slot to set its state/appearance
        /// </summary>
        public void SetUpSlot(IAbilitySlotBehavior slotBehavior, GridEntity selectedEntity) {
            _slotBehavior = slotBehavior;
            _selectedEntity = selectedEntity;
            slotBehavior.SetUpSprites(AbilityImage, SecondaryAbilityImage, AbilitySlotBackgroundView);
            slotBehavior.SetUpTimerView();
            
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1;
            
            HotkeyText.text = Hotkey;
            
            CheckAvailability();
            AddListeners();
        }

        private void Hide() {
            CanvasGroup.alpha = 0;
            Availability = AvailabilityResult.Hidden;
        }

        public void Clear() {
            RemoveListeners();
            _shouldDeselectWhenTimerElapses = false;

            _slotBehavior?.ClearTimerView();
            _slotBehavior = null;
            _selectedEntity = null;
            _selected = false;
            Availability = AvailabilityResult.Hidden;
            gameObject.SetActive(false);
        }

        public void SelectAbility() {
            if (Availability != AvailabilityResult.Selectable) {
                GameManager.Instance.GameAudio.InvalidSound();
                return;
            }
            
            GameManager.Instance.GameAudio.ButtonClickSound();
            
            MarkSelected(true);
            _slotBehavior?.SelectSlot();
        }

        public void HandleFailedToSelect() {
            GameManager.Instance.GameAudio.InvalidSound();
            _slotBehavior?.HandleFailedToSelect(Availability);
        }

        public void MarkSelected(bool selected) {
            _selected = selected;
            _shouldDeselectWhenTimerElapses = false;
            if (selected) {
                SlotFrame.color = SelectedColor;
                if (_slotBehavior is { IsAbilityTargetable: true }) {
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
                MarkAvailability(AvailabilityResult.Unselectable);
                MarkSelected(false);
            }
        }

        private void CheckAvailability() {
            if (_slotBehavior == null || _selectedEntity == null) return;
            
            AvailabilityResult availability = _slotBehavior.GetAvailability();
            switch (availability) {
                case AvailabilityResult.Hidden:
                    Hide();
                    break;
                case AvailabilityResult.Selectable:
                    MarkAvailability(AvailabilityResult.Selectable);
                    break;
                case AvailabilityResult.Unselectable:
                    MarkAvailability(AvailabilityResult.Unselectable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (_selected && Availability == AvailabilityResult.Selectable) {
                // Re-set the appearance to "selected" if we were selected and still can be
                SlotFrame.color = SelectedColor;
            }
        }

        private void MarkAvailability(AvailabilityResult availability) {
            SlotFrame.color = availability == AvailabilityResult.Selectable ? SelectableColor : UnselectableColor;
            CanvasGroup.alpha = 1;
            Availability = availability;
        }
        
        #region Listeners

        private void AddListeners() {
            if (_selectedEntity == null) return;
            
            // Entity
            _selectedEntity.AbilityTimerExpiredEvent += OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent += OnAbilityTimersChanged;
            _selectedEntity.InProgressAbilitiesUpdatedEvent += OnInProgressAbilities;
            GameManager.Instance.LeaderTracker.LeaderMoved += OnLeaderMoved;

            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_selectedEntity);
            if (player?.Data.Team is GameTeam.Player1 or GameTeam.Player2) {
                // Track resources and owned changes for the player
                player.ResourcesController.BalanceChangedEvent += OnPlayerResourcesBalanceChanged;
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent += OnPlayerOwnedEntitiesChanged;
            }
        }

        private void RemoveListeners() {
            if (_selectedEntity == null) return;
            
            _selectedEntity.AbilityTimerExpiredEvent -= OnAbilityTimersChanged;
            _selectedEntity.AbilityPerformedEvent -= OnAbilityTimersChanged;
            GameManager.Instance.LeaderTracker.LeaderMoved -= OnLeaderMoved;

            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_selectedEntity);
            if (player?.Data.Team is GameTeam.Player1 or GameTeam.Player2) {
                // Track owned changes for the player
                player.ResourcesController.BalanceChangedEvent -= OnPlayerResourcesBalanceChanged;
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent -= OnPlayerOwnedEntitiesChanged;
            }
        }

        private void OnAbilityTimersChanged(IAbility ability, AbilityTimer timer) {
            if (_slotBehavior == null) return; 
            if (_slotBehavior.CaresAboutAbilityChannels || timer.Ability.AbilityData.SlotLocation == SlotLocation) {
                CheckAvailability();
            }
        }

        private void OnInProgressAbilities(List<IAbility> abilities) {
            if (_slotBehavior == null) return; 
            if (_slotBehavior.CaresAboutInProgressAbilities) {
                CheckAvailability();
            }
        }

        private void OnLeaderMoved(GridEntity leader) {
            if (_slotBehavior == null) return; 
            if (_slotBehavior.CaresAboutLeaderPosition) {
                CheckAvailability();
            }
        }

        /// <summary>
        /// When the player gains or spends resources
        /// </summary>
        private void OnPlayerResourcesBalanceChanged(List<ResourceAmount> resourceAmounts) {
            if (_slotBehavior == null) return; 
            if (_slotBehavior.IsAvailabilitySensitiveToResources) {
                CheckAvailability();
            }
        }
        
        /// <summary>
        /// When the player gains a new entity or an entity of theirs dies
        /// </summary>
        private void OnPlayerOwnedEntitiesChanged() {
            CheckAvailability();
        }
        
        private void ToggleClicked() {
            IconsGroup.transform.localPosition = IconDownPosition;
            AbilityImage.color = SelectedIconColor;
        }
        
        private void ToggleUnClicked() {
            IconsGroup.transform.localPosition = IconUpPosition;
            AbilityImage.color = DeselectedIconColor;
            
            // The hotkey for this might have toggled a click and performed the ability logic at the same time. 
            if (Time.time - _lastAbilityClickTime > ButtonDeselectBufferForHotkeyDetection) {
                DoSelectAbility();
            }
        }

        public void DoSelectAbility() {
            AbilityInterface.SelectAbility(this);
            _lastAbilityClickTime = Time.time;
        }
        
        #endregion

        public void OnPointerEnter(PointerEventData eventData) {
            if (_slotBehavior == null) return;
            AbilityInterface.TooltipView.ToggleForHoveredAbility(_slotBehavior.AbilitySlotInfo, _slotBehavior);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (_slotBehavior == null) return; 
            AbilityInterface.TooltipView.ToggleForHoveredAbility(null, null);
        }
    }
}