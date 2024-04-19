using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying information about selected <see cref="GridEntity"/>s and <see cref="IAbility"/>s
    /// </summary>
    public class SelectionInterface : MonoBehaviour {
        [SerializeField] private AbilityChannel MoveChannel;

        [Header("References")] 
        [SerializeField] private GameObject View;
        [SerializeField] private TooltipView _tooltipView;
        public TooltipView TooltipView => _tooltipView;
        
        [SerializeField] private Image EntityIcon;
        [SerializeField] private Image EntityColorsIcon;
        [SerializeField] private TMP_Text NameField;
        [SerializeField] private TMP_Text DescriptionField;
        [SerializeField] private TMP_Text TagsField;
        
        [SerializeField] private HealthBar HealthBar;
        [SerializeField] private TMP_Text MovesField;
        [SerializeField] private AbilityTimerCooldownView MoveTimer;    // TODO I'll probably want to try out using a move meter instead of a timer for movement. 
        [SerializeField] private TMP_Text AttackField;
        
        // TODO Row of icons representing upgrades? Hoverable to get info about them?
        
        // TODO Player name at bottom, text color == team color

        [SerializeField] private AbilityInterface AbilityInterface;

        private GridEntity _displayedEntity;
        private AbilityCooldownTimer _activeMoveCooldownTimer;
        
        public void Initialize() {
            ToggleViews(false);
        }

        /// <summary>
        /// Update the view to display the new selected entity
        /// </summary>
        public void UpdateSelectedEntity(GridEntity entity) {
            DeselectCurrentEntity();
            if (entity == null) return;
            
            _displayedEntity = entity;
            HealthBar.SetTarget(entity);

            UpdateEntityInfo();
            
            AbilityInterface.SetUpForEntity(entity);

            entity.AbilityPerformedEvent += OnEntityAbilityPerformed;
            entity.CooldownTimerExpiredEvent += OnEntityAbilityCooldownExpired;
            
            ToggleViews(true);

            if (entity.MyTeam == GameManager.Instance.LocalPlayer.Data.Team) {
                entity.PerformAutoSelection();
            }
        }

        public void HandleAbilityHotkey(string input) {
            AbilityInterface.HandleHotkey(input);
        }
        
        public void DeselectActiveAbility() {
            AbilityInterface.DeselectActiveAbility();
            TooltipView.ToggleForTargetableAbility(null, null);
        }

        public void SelectBuildAbility(BuildAbilityData buildData) {
            AbilityInterface.SelectBuildAbility(buildData, _displayedEntity);
        }

        public void DeselectCurrentEntity() {
            if (_displayedEntity == null) return;
            
            _displayedEntity.AbilityPerformedEvent -= OnEntityAbilityPerformed;
            _displayedEntity.CooldownTimerExpiredEvent -= OnEntityAbilityCooldownExpired;

            _activeMoveCooldownTimer = null;
            _displayedEntity = null;
            if (MoveTimer != null) {
                MoveTimer.UnsubscribeFromTimers();
            }
            
            // Hide everything
            ToggleViews(false);
            
            DeselectActiveAbility();
        }

        private void ToggleViews(bool active) {
            View.SetActive(active);
            _tooltipView.ToggleForEntity(_displayedEntity);

            if (!active) {
                AbilityInterface.ClearInfo();
            }
        }

        private void OnEntityAbilityPerformed(IAbility iAbility, AbilityCooldownTimer abilityCooldownTimer) {
            UpdateEntityInfo();
        }

        private void OnEntityAbilityCooldownExpired(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            UpdateEntityInfo();
        }

        private void UpdateEntityInfo() {
            if (_displayedEntity == null) return;

            EntityData entityData = _displayedEntity.EntityData;
            EntityIcon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            EntityColorsIcon.sprite = entityData.TeamColorSprite;
            EntityColorsIcon.color = GameManager.Instance.GetPlayerForTeam(_displayedEntity.MyTeam).Data.TeamColor;

            NameField.text = _displayedEntity.DisplayName;
            DescriptionField.text = entityData.Description;
            TagsField.text = string.Join(", ", entityData.Tags);
            MovesField.text = $"{_displayedEntity.MoveTime}";

            if (_displayedEntity.IsAbilityChannelOnCooldown(MoveChannel, out _activeMoveCooldownTimer)) {
                MoveTimer.gameObject.SetActive(true);
                MoveTimer.Initialize(_activeMoveCooldownTimer, false, true);
            } else {
                MoveTimer.gameObject.SetActive(false);
            }
            AttackField.text = _displayedEntity.Damage.ToString();
        }
    }
}