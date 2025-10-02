using System.Linq;
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
        [Header("References")] 
        [SerializeField] private GameObject View;
        [SerializeField] private TooltipView _tooltipView;
        public TooltipView TooltipView => _tooltipView;

        [SerializeField] private Image TeamColorBanner;
        [SerializeField] private Image EntityIcon;
        [SerializeField] private Image EntityColorsIcon;
        [SerializeField] private TMP_Text NameField;
        [SerializeField] private TMP_Text DescriptionField;
        [SerializeField] private TMP_Text TagsField;
        
        [SerializeField] private HealthDisplay _healthDisplay;
        [SerializeField] private GameObject MovesRow;
        [SerializeField] private TMP_Text MovesField;
        [SerializeField] private GameObject AttackRow;
        [SerializeField] private TMP_Text AttackField;
        [SerializeField] private GameObject ResourceRow;
        [SerializeField] private TMP_Text ResourceLabel;
        [SerializeField] private TMP_Text ResourceField;
        [SerializeField] private GameObject _rangeRow;
        [SerializeField] private TMP_Text _rangeField;
        [SerializeField] private GameObject _killCountRow;
        [SerializeField] private TMP_Text _killCountField;
        [SerializeField] private GameObject _incomeRateRow;
        [SerializeField] private TMP_Text _incomeRateField;

        [SerializeField] private HoverableInfoIcon _defenseHoverableInfoIcon;
        [SerializeField] private HoverableInfoIcon _attackHoverableInfoIcon;
        [SerializeField] private HoverableInfoIcon _moveHoverableInfoIcon;

        [SerializeField] private BuildQueueView _buildQueueForStructure;
        [SerializeField] private BuildQueueView _buildQueueForWorker;
        
        [SerializeField] private AbilityInterface AbilityInterface;

        private ISelectableObjectLogic _displayedSelectable;

        public bool BuildMenuOpenFromSelection => AbilityInterface.BuildMenuOpenFromSelection;
        
        public void Initialize() {
            ToggleViews(false, null);
        }

        /// <summary>
        /// Update the view to display the new selected entity
        /// </summary>
        public void UpdateSelectedEntity(GridEntity entity) {
            DeselectCurrentSelectable();
            if (entity == null) return;
            
            _healthDisplay.SetTarget(entity);

            SelectableGridEntityLogic selectedEntity = new SelectableGridEntityLogic(entity);
            _displayedSelectable = selectedEntity;
            UpdateEntityInfo();
            
            AbilityInterface.SetUpForEntity(entity);
            
            ToggleViews(true, selectedEntity);
            
            PerformAutoSelectionAbilities(entity);
        }

        /// <summary>
        /// Update the view to display the new selected terrain
        /// </summary>
        public void UpdateSelectedTerrain(GameplayTile tile) {
            DeselectCurrentSelectable();

            SelectableGameplayTileLogic selectedTile = new SelectableGameplayTileLogic(tile);
            _displayedSelectable = selectedTile;
            UpdateEntityInfo();
            AbilityInterface.ClearInfo();

            ToggleViews(true, selectedTile);
        }

        public void HandleAbilityHotkey(string input) {
            AbilityInterface.HandleHotkey(input);
        }
        
        public void DeselectActiveAbility() {
            AbilityInterface.DeselectActiveAbility();
            TooltipView.ToggleForTargetableAbility(null, null);
        }

        public void DeselectBuildAbility() {
            if (_displayedSelectable != null) {
                AbilityInterface.SetUpForEntity(_displayedSelectable.Entity);
            }
        }

        public void SetUpBuildSelection(BuildAbilityData buildData) {
            AbilityInterface.SetUpBuildSelection(buildData, _displayedSelectable.Entity);
        }

        private void DeselectCurrentSelectable() {
            _defenseHoverableInfoIcon.HideIcon();
            _attackHoverableInfoIcon.HideIcon();
            _moveHoverableInfoIcon.HideIcon();
            if (_displayedSelectable == null) return;

            _displayedSelectable.UnregisterListeners();

            _displayedSelectable = null;
            
            // Hide everything
            ToggleViews(false, null);
            
            DeselectActiveAbility();
        }

        private void ToggleViews(bool active, ISelectableObjectLogic selectableObject) {
            View.SetActive(active);
            _tooltipView.ToggleForSelectable(selectableObject);

            if (!active) {
                AbilityInterface.ClearInfo();
                _buildQueueForStructure.Clear();
                _buildQueueForWorker.Clear();
                
                _defenseHoverableInfoIcon.HideIcon();
                _attackHoverableInfoIcon.HideIcon();
                _moveHoverableInfoIcon.HideIcon();
            } else {
                TeamColorBanner.color = selectableObject.TeamBannerColor;
            }
        }

        private void UpdateEntityInfo() {
            if (_displayedSelectable == null) return;

            _displayedSelectable.SetUpIcons(EntityIcon, EntityColorsIcon);

            NameField.text = _displayedSelectable.Name;
            DescriptionField.text = _displayedSelectable.ShortDescription;
            TagsField.text = _displayedSelectable.Tags;

            _healthDisplay.gameObject.SetActive(_displayedSelectable.DisplayHP);
            _displayedSelectable.SetUpMoveView(MovesRow, MovesField);
            _displayedSelectable.SetUpAttackView(AttackRow, AttackField);
            _displayedSelectable.SetUpRangeView(_rangeRow, _rangeField);
            _displayedSelectable.SetUpResourceView(ResourceRow, ResourceLabel, ResourceField);
            _displayedSelectable.SetUpBuildQueueView(_buildQueueForStructure, _buildQueueForWorker);
            _displayedSelectable.SetUpKillCountView(_killCountRow, _killCountField);
            _displayedSelectable.SetUpIncomeRateView(_incomeRateRow, _incomeRateField);
            _displayedSelectable.SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, _moveHoverableInfoIcon);
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
