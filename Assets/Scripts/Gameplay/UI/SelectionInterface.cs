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

        [HideInInspector]
        public GridEntity SelectedEntity;
        
        private AbilityCooldownTimer _activeMoveCooldownTimer;
        
        public void Initialize() {
            ToggleViews(false);
        }

        public void SelectEntity(GridEntity entity) {
            DeselectCurrentEntity();
            if (entity == null) return;
            
            SelectedEntity = entity;
            HealthBar.SetTarget(entity);

            UpdateEntityInfo();
            
            AbilityInterface.SetUpForEntity(entity);

            entity.MovesChangedEvent += UpdateEntityInfo;
            entity.HPChangedEvent += UpdateEntityInfo;
            entity.AbilityPerformedEvent += OnEntityAbilityPerformed;
            entity.KilledEvent += OnEntityKilled;
            
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
        }

        public void SelectBuildAbility(BuildAbilityData buildData) {
            AbilityInterface.SelectBuildAbility(buildData, SelectedEntity);
        }

        private void DeselectCurrentEntity() {
            if (SelectedEntity == null) return;
            
            SelectedEntity.MovesChangedEvent -= UpdateEntityInfo;
            SelectedEntity.HPChangedEvent -= UpdateEntityInfo;
            SelectedEntity.AbilityPerformedEvent -= OnEntityAbilityPerformed;
            SelectedEntity.KilledEvent -= OnEntityKilled;

            _activeMoveCooldownTimer = null;
            SelectedEntity = null;
            
            // Hide everything
            ToggleViews(false);
        }

        private void ToggleViews(bool active) {
            View.SetActive(active);

            if (!active) {
                AbilityInterface.ClearInfo();
            }
        }

        private void OnEntityAbilityPerformed(IAbility iAbility, AbilityCooldownTimer abilityCooldownTimer) {
            UpdateEntityInfo();
        }
        
        private void OnEntityKilled() {
            DeselectCurrentEntity();
        } 

        private void UpdateEntityInfo() {
            if (SelectedEntity == null) return;
            
            EntityIcon.sprite = SelectedEntity.EntityData.BaseSprite;
            EntityColorsIcon.sprite = SelectedEntity.EntityData.TeamColorSprite;
            EntityColorsIcon.color = GameManager.Instance.GetPlayerForTeam(SelectedEntity.MyTeam).Data.TeamColor;

            NameField.text = SelectedEntity.DisplayName;
            DescriptionField.text = SelectedEntity.EntityData.Description;
            TagsField.text = string.Join(", ", SelectedEntity.EntityData.Tags);

            if (SelectedEntity.IsAbilityChannelOnCooldown(MoveChannel, out _activeMoveCooldownTimer)) {
                MovesField.text = $"{SelectedEntity.CurrentMoves} / {SelectedEntity.MaxMove}";
                MoveTimer.gameObject.SetActive(true);
                MoveTimer.Initialize(_activeMoveCooldownTimer, false);
            } else {
                MovesField.text = $"{SelectedEntity.MaxMove} / {SelectedEntity.MaxMove}";
                MoveTimer.gameObject.SetActive(false);
            }
            AttackField.text = SelectedEntity.Damage.ToString();
        }
    }
}