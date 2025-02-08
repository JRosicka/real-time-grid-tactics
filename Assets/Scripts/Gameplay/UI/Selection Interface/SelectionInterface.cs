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
        
        [SerializeField] private HealthDisplay _healthDisplay;
        [SerializeField] private GameObject MovesRow;
        [SerializeField] private TMP_Text MovesField;
        [SerializeField] private AbilityTimerCooldownView MoveTimer;    // TODO I'll probably want to try out using a move meter instead of a timer for movement. 
        [SerializeField] private GameObject AttackRow;
        [SerializeField] private TMP_Text AttackField;
        [SerializeField] private GameObject ResourceRow;
        [SerializeField] private TMP_Text ResourceLabel;
        [SerializeField] private TMP_Text ResourceField;

        [SerializeField] private BuildQueueView _buildQueueForStructure;
        [SerializeField] private BuildQueueView _buildQueueForWorker;
        
        // TODO Row of icons representing upgrades? Hoverable to get info about them?
        
        // TODO Player name at bottom, text color == team color

        [SerializeField] private AbilityInterface AbilityInterface;

        private GridEntity _displayedEntity;
        private AbilityCooldownTimer _activeMoveCooldownTimer;

        public bool BuildMenuOpenFromSelection => AbilityInterface.BuildMenuOpenFromSelection;
        
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
            _healthDisplay.SetTarget(entity);

            UpdateEntityInfo();
            
            AbilityInterface.SetUpForEntity(entity);

            entity.AbilityPerformedEvent += OnEntityAbilityPerformed;
            entity.CooldownTimerExpiredEvent += OnEntityAbilityCooldownExpired;
            entity.CurrentResources.ValueChanged += OnEntityResourceAmountChanged;
            
            ToggleViews(true);
            
            PerformAutoSelectionAbilities(entity);
        }

        public void HandleAbilityHotkey(string input) {
            AbilityInterface.HandleHotkey(input);
        }
        
        public void DeselectActiveAbility() {
            AbilityInterface.DeselectActiveAbility();
            TooltipView.ToggleForTargetableAbility(null, null);
        }

        public void DeselectBuildAbility() {
            if (_displayedEntity != null) {
                AbilityInterface.SetUpForEntity(_displayedEntity);
            }
        }

        public void SetUpBuildSelection(BuildAbilityData buildData) {
            AbilityInterface.SetUpBuildSelection(buildData, _displayedEntity);
        }

        private void DeselectCurrentEntity() {
            if (_displayedEntity == null) return;
            
            _displayedEntity.AbilityPerformedEvent -= OnEntityAbilityPerformed;
            _displayedEntity.CooldownTimerExpiredEvent -= OnEntityAbilityCooldownExpired;
            _displayedEntity.CurrentResources.ValueChanged -= OnEntityResourceAmountChanged;

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
                _buildQueueForStructure.Clear();
                _buildQueueForWorker.Clear();
            }
        }

        private void OnEntityAbilityPerformed(IAbility iAbility, AbilityCooldownTimer abilityCooldownTimer) {
            UpdateEntityInfo();
        }

        private void OnEntityAbilityCooldownExpired(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            UpdateEntityInfo();
        }

        private void OnEntityResourceAmountChanged(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, object metadata) {
            UpdateEntityInfo();
        }

        private void UpdateEntityInfo() {
            if (_displayedEntity == null) return;

            EntityData entityData = _displayedEntity.EntityData;
            EntityIcon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            EntityColorsIcon.sprite = entityData.TeamColorSprite;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(_displayedEntity.Team);
            EntityColorsIcon.color = player != null ? player.Data.TeamColor : Color.clear;

            NameField.text = _displayedEntity.DisplayName;
            DescriptionField.text = entityData.ShortDescription;
            TagsField.text = string.Join(", ", entityData.Tags);

            _healthDisplay.gameObject.SetActive(entityData.HP > 0);

            if (_displayedEntity.CanMove) {
                MovesRow.SetActive(true);
                MovesField.text = $"{_displayedEntity.MoveTime}";
                if (GameManager.Instance.AbilityAssignmentManager.IsAbilityChannelOnCooldownForEntity(_displayedEntity, MoveChannel, out _activeMoveCooldownTimer)) {
                    MoveTimer.gameObject.SetActive(true);
                    MoveTimer.Initialize(_activeMoveCooldownTimer, false, true);
                } else {
                    MoveTimer.gameObject.SetActive(false);
                }
            } else {
                MovesRow.SetActive(false);
            }

            if (entityData.Damage > 0) {
                AttackRow.SetActive(true);
                AttackField.text = _displayedEntity.Damage.ToString();
            } else {
                AttackRow.SetActive(false);
            }

            if (entityData.StartingResourceSet.Amount > 0) {
                ResourceRow.gameObject.SetActive(true);
                ResourceLabel.text = $"{_displayedEntity.CurrentResourcesValue.Type.DisplayName()}:";
                ResourceField.text = _displayedEntity.CurrentResourcesValue.Amount.ToString();
            } else {
                ResourceRow.gameObject.SetActive(false);
            }
            
            // Build queue
            if (entityData.CanBuild) {
                if (entityData.IsStructure) {
                    _buildQueueForStructure.SetUpForEntity(_displayedEntity);
                } else {
                    _buildQueueForWorker.SetUpForEntity(_displayedEntity);
                }
            }
        }
        
        /// <summary>
        /// Auto-select any abilities that we have configured as auto-selectable.
        /// This probably won't behave well if this entity has multiple abilities configured as auto-selectable... 
        /// </summary>
        private static void PerformAutoSelectionAbilities(GridEntity entity) {
            foreach (IAbilityData abilityData in entity.Abilities.Where(a => a.AutoSelect)) {
                if (abilityData.SelectableForAllPlayers || entity.Team == GameManager.Instance.LocalTeam) {
                    abilityData.SelectAbility(entity);
                }
            }
        }
    }
}
