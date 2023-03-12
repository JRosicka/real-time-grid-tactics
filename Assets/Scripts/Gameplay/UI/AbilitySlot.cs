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

        private IAbilityData _currentAbility;
        private GridEntity _selectedEntity;
        private BuildAbilityData _currentBuildData;
        private PurchasableData _currentEntityToBuild;
        private bool _selectable;
        private bool _displayingBuild;
        
        public void SetUpForAbility(IAbilityData data, GridEntity selectedEntity) {
            gameObject.SetActive(true);
            _currentAbility = data;
            _selectedEntity = selectedEntity;
            _displayingBuild = false;

            AbilityImage.sprite = _currentAbility.Icon;
            SecondaryAbilityImage.sprite = null;
            SecondaryAbilityImage.gameObject.SetActive(false);
            HotkeyText.text = Hotkey;

            CheckAvailability();
            AddListeners();
        }

        public void SetUpForBuildTarget(BuildAbilityData buildData, PurchasableData entityToBuild, GridEntity selectedEntity) {
            gameObject.SetActive(true);
            _currentBuildData = buildData;
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
            _currentAbility = null;
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
                _currentAbility?.SelectAbility(_selectedEntity);
            }
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
            if (_displayingBuild) {
                // TODO check if we can afford this and if the selected entity is currently building anything
                SlotFrame.color = SelectableColor;
                _selectable = true;
            } else {
                if (_currentAbility == null || _selectedEntity == null) return;

                if (_selectedEntity.CanUseAbility(_currentAbility)) {
                    SlotFrame.color = SelectableColor;
                    _selectable = true;
                } else {
                    SlotFrame.color = UnselectableColor;
                    _selectable = false;
                }
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
            if (_displayingBuild) return;
            
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