using System.Collections;
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
        public float ButtonDeselectDelay = .5f;
        
        [Header("References")]
        public Image AbilityImage;
        public Image SecondaryAbilityImage;    // For build target color icon
        public Image SlotFrame;
        public TMP_Text HotkeyText;
        public AbilityInterface AbilityInterface;
        public Canvas TeamColorsCanvas;

        private IAbilityData _currentAbilityData;
        private GridEntity _selectedEntity;
        private BuildAbilityData _currentBuildData;
        private PurchasableData _currentEntityToBuild;
        private bool _selectable;
        private bool _displayingBuild;
        private bool _selected;
        private bool _shouldDeselectWhenTimerElapses;

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

            TeamColorsCanvas.sortingOrder = 1;

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

            TeamColorsCanvas.sortingOrder = 1;

            AbilityImage.sprite = entityToBuild.BaseSpriteIconOverride == null ? entityToBuild.BaseSprite : entityToBuild.BaseSpriteIconOverride;
            if (entityToBuild.TeamColorSprite != null) {
                SecondaryAbilityImage.sprite = entityToBuild.TeamColorSprite;
                SecondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(selectedEntity.MyTeam).Data.TeamColor;
                SecondaryAbilityImage.gameObject.SetActive(true);
                if (entityToBuild.DisplayTeamColorOverMainSprite) {
                    TeamColorsCanvas.sortingOrder = 2;
                } else {
                    TeamColorsCanvas.sortingOrder = 1;
                }
            }
            HotkeyText.text = Hotkey;
            
            CheckAvailability();
            AddListeners();
        }

        public void Clear() {
            RemoveListeners();
            _shouldDeselectWhenTimerElapses = false;

            _currentAbilityData = null;
            _currentBuildData = null;
            _currentEntityToBuild = null;
            _selectedEntity = null;
            _displayingBuild = false;
            _selected = false;
            _selectable = false;
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
                if (_currentBuildData.Targetable) {
                    GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(_currentBuildData, _currentEntityToBuild);
                } else {
                    // Try to perform the build ability
                    _selectedEntity.PerformAbility(_currentBuildData, new BuildAbilityParameters {
                        Buildable = _currentEntityToBuild, 
                        BuildLocation = _selectedEntity.Location
                    }, false);
                }
            } else {
                _currentAbilityData?.SelectAbility(_selectedEntity);
            }
            return true;
        }

        public void MarkSelected(bool selected) {
            _selected = selected;
            _shouldDeselectWhenTimerElapses = false;
            if (selected) {
                SlotFrame.color = SelectedColor;
                if (_currentAbilityData.Targeted) {
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
            if (_displayingBuild) {
                IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam);
                List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

                if (_currentEntityToBuild is UpgradeData && ownedPurchasables.Contains(_currentEntityToBuild)) {
                    // Upgrade that we already own
                    Clear();
                } else if (_selectedEntity.CanUseAbility(_currentBuildData) 
                           && GameManager.Instance.GetPlayerForTeam(_selectedEntity.MyTeam).ResourcesController.CanAfford(_currentEntityToBuild.Cost)
                           && _currentEntityToBuild.Requirements.All(r => ownedPurchasables.Contains(r))) {
                    // This entity can build this and we can afford this
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
            if (_displayingBuild) {
                // Displaying build
                CheckAvailability();
            } else {
                // Displaying abilities
                if (timer.ChannelBlockers.Contains(Channel)) {
                    CheckAvailability();
                }
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