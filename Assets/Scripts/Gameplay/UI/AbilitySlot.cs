using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
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
        public Image SecondaryAbilityImage;    // For build target color icon
        public Image SlotFrame;
        public TMP_Text HotkeyText;
        public AbilityInterface AbilityInterface;

        private IAbilityData _currentAbilityData;
        private GridEntity _selectedEntity;
        private BuildAbilityData _currentBuildData;
        private PurchasableData _currentEntityToBuild;
        private bool _selectable;
        private bool _displayingBuild;
        private bool _selected;

        private PlayerResourcesController LocalResourcesController => GameManager.Instance.LocalPlayer.ResourcesController;

        public void SetUpForAbility(IAbilityData data, GridEntity selectedEntity) {
            gameObject.SetActive(true);
            _currentAbilityData = data;
            _selectedEntity = selectedEntity;
            _displayingBuild = false;

            AbilityImage.sprite = _currentAbilityData.Icon;
            SecondaryAbilityImage.sprite = null;
            SecondaryAbilityImage.gameObject.SetActive(false);
            HotkeyText.text = Hotkey;

            CheckAvailability();
            AddListeners();
        }

        public void SetUpForBuildTarget(BuildAbilityData buildData, PurchasableData entityToBuild, GridEntity selectedEntity) {
            gameObject.SetActive(true);
            _currentBuildData = buildData;
            _currentAbilityData = _currentBuildData;
            _currentEntityToBuild = entityToBuild;
            _selectedEntity = selectedEntity;
            _displayingBuild = true;

            AbilityImage.sprite = entityToBuild.BaseSprite;
            if (entityToBuild.TeamColorSprite != null) {
                SecondaryAbilityImage.sprite = entityToBuild.TeamColorSprite;
                SecondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(selectedEntity.MyTeam).Data.TeamColor;
                SecondaryAbilityImage.gameObject.SetActive(true);
            }
            HotkeyText.text = Hotkey;
            
            CheckAvailability();
            AddListeners();
        }

        public void Clear() {
            RemoveListeners();
            _currentAbilityData = null;
            _currentBuildData = null;
            _currentEntityToBuild = null;
            _selectedEntity = null;
            _displayingBuild = false;
            gameObject.SetActive(false);
        }
        
        public void OnButtonClick() {
            if (!_selectable) return;
            AbilityInterface.SelectAbility(this);
        }

        public bool SelectAbility() {
            if (!_selectable) return false;
            
            MarkSelected(true);

            if (_displayingBuild) {
                // Try to perform the build ability
                _selectedEntity.DoAbility(_currentBuildData, new BuildAbilityParameters {
                    Buildable = _currentEntityToBuild, 
                    BuildLocation = _selectedEntity.Location
                });
            } else {
                _currentAbilityData?.SelectAbility(_selectedEntity);
            }
            return true;
        }

        public void MarkSelected(bool selected) {
            _selected = selected;
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
            if (_displayingBuild) {
                IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam);
                List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

                // Check if this entity can build this and if we can afford this
                if (_selectedEntity.CanUseAbility(_currentBuildData) 
                    && GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam).ResourcesController.CanAfford(_currentEntityToBuild.Cost)
                    && _currentEntityToBuild.Requirements.All(r => ownedPurchasables.Contains(r))) {
                    MarkSelectable(true);
                } else {
                    MarkSelectable(false);
                }
            } else {
                if (_currentAbilityData == null || _selectedEntity == null) return;

                if (_currentAbilityData.SelectableWhenBlocked || _selectedEntity.CanUseAbility(_currentAbilityData)) {
                    MarkSelectable(true);
                } else {
                    MarkSelectable(false);
                }
            }
            
            if (_selected && _selectable) {
                // Re-set the appearance to "selected" if we were selected and still can be
                SlotFrame.color = SelectedColor;
            }
        }

        private void MarkSelectable(bool available) {
            SlotFrame.color = available ? SelectableColor : UnselectableColor;
            _selectable = available;
        }
        
        #region Listeners

        private void AddListeners() {
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
            if (_displayingBuild) return;
            
            if (timer.ChannelBlockers.Contains(Channel)) {
                CheckAvailability();
            }
        }
        
        /// <summary>
        /// When the player gains or spends resources
        /// </summary>
        private void OnPlayerResourcesBalanceChanged(List<ResourceAmount> resourceAmounts) {
            if (_displayingBuild) {
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
    }
}