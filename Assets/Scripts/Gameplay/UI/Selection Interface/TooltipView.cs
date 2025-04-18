using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying a tooltip for the currently selected entity/ability/terrain
    /// </summary>
    public class TooltipView : MonoBehaviour {
        [SerializeField] private GameObject _view;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _secondaryIcon;
        [SerializeField] private Canvas _teamColorsCanvas;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;

        [SerializeField] private GameObject _basicResourceCostContainer;
        [SerializeField] private TMP_Text _basicResourceCostAmount;
        [SerializeField] private GameObject _advancedResourceCostContainer;
        [SerializeField] private TMP_Text _advancedResourceCostAmount;
        [SerializeField] private GameObject _buildTimeContainer;
        [SerializeField] private TMP_Text _buildTimeAmount;
        
        [Header("Config")]
        [SerializeField] private string _buildTimeFormat = "{0}s";

        private ISelectableObjectLogic _selectedObject;
        private ITargetableAbilityData _selectedTargetableAbility;
        private IAbilitySlotBehavior _selectedTargetableAbilitySlotBehavior;

        public void ToggleForSelectable(ISelectableObjectLogic selectableObject) {
            if (_selectedObject is { BuildQueue: not null }) {
                _selectedObject.BuildQueue.BuildQueueUpdated -= SelectedEntityBuildQueueUpdated;
            }
            _selectedObject = selectableObject;
            ToggleTooltip(selectableObject != null);
            if (selectableObject != null) {
                if (_selectedObject.BuildQueue != null) {
                    _selectedObject.BuildQueue.BuildQueueUpdated += SelectedEntityBuildQueueUpdated;
                }
                SetUpEntityView(selectableObject);
            }
        }
        
        public void ToggleForTargetableAbility(ITargetableAbilityData ability, IAbilitySlotBehavior abilitySlotBehavior) {
            if (ability == null) {
                _selectedTargetableAbility = null;
                _selectedTargetableAbilitySlotBehavior = null;

                // No ability selected, so go back to showing the selected entity if we have one
                ToggleForSelectable(_selectedObject);
            } else {
                ToggleTooltip(true);
                SetUpAbilityView(ability.AbilitySlotInfo, abilitySlotBehavior, false);
                _selectedTargetableAbility = ability;
                _selectedTargetableAbilitySlotBehavior = abilitySlotBehavior;
            }
        }

        public void ToggleForHoveredAbility(AbilitySlotInfo abilityInfo, IAbilitySlotBehavior abilitySlotBehavior) {
            if (abilityInfo == null) {
                // No ability hovered, so go back to showing the selected ability if we have one
                if (_selectedTargetableAbility != null) {
                    ToggleForTargetableAbility(_selectedTargetableAbility, _selectedTargetableAbilitySlotBehavior);
                } else {
                    // No ability selected, so go back to showing the selected entity if we have one
                    ToggleForSelectable(_selectedObject);
                }
            } else if (abilitySlotBehavior.GetAvailability() == AbilitySlot.AvailabilityResult.Hidden) {
                // The hovered ability slot is hidden - do not react
            } else {
                ToggleTooltip(true);
                SetUpAbilityView(abilityInfo, abilitySlotBehavior, abilitySlotBehavior is QueuedBuildAbilitySlotBehavior);
            }
        }
        
        private void SetUpEntityView(ISelectableObjectLogic selectableObject) {
            BuildAbility inProgressBuild = selectableObject.InProgressBuild;
            if (inProgressBuild != null) {
                SetUpForInProgressBuild(inProgressBuild);
                return;
            }
            
            selectableObject.SetUpIcons(_icon, _secondaryIcon, _teamColorsCanvas, 2);
            
            _name.text = selectableObject.Name;
            _description.text = selectableObject.LongDescription;
            
            _basicResourceCostContainer.SetActive(false);
            _advancedResourceCostContainer.SetActive(false);
            _buildTimeContainer.SetActive(false);
        }

        private void SetUpForInProgressBuild(BuildAbility buildAbility) {
            BuildAbilityData buildData = (BuildAbilityData) buildAbility.AbilityData;
            BuildAbilitySlotBehavior buildBehavior = new BuildAbilitySlotBehavior(buildData, buildAbility.AbilityParameters.Buildable, buildAbility.Performer);
            SetUpAbilityView(buildData.AbilitySlotInfo, buildBehavior, true);
        }

        private void SetUpAbilityView(AbilitySlotInfo abilityInfo, IAbilitySlotBehavior abilitySlotBehavior, bool includeInProgressMessage) {
            abilitySlotBehavior.SetUpSprites(_icon, _secondaryIcon, _teamColorsCanvas);
            if (abilitySlotBehavior is BuildAbilitySlotBehavior buildAbilitySlotBehavior) {
                _name.text = buildAbilitySlotBehavior.Buildable.ID + (includeInProgressMessage ? " (constructing)" : "");
                _description.text = buildAbilitySlotBehavior.Buildable.Description;

                int basicCost = buildAbilitySlotBehavior.Buildable.Cost.FirstOrDefault(r => r.Type == ResourceType.Basic)?.Amount ?? 0;
                int advancedCost = buildAbilitySlotBehavior.Buildable.Cost.FirstOrDefault(r => r.Type == ResourceType.Advanced)?.Amount ?? 0;
                _basicResourceCostContainer.SetActive(basicCost > 0);
                _basicResourceCostAmount.text = basicCost.ToString();
                _advancedResourceCostContainer.SetActive(advancedCost > 0);
                _advancedResourceCostAmount.text = advancedCost.ToString();
                _buildTimeContainer.SetActive(true);
                _buildTimeAmount.text = string.Format(_buildTimeFormat, Mathf.RoundToInt(buildAbilitySlotBehavior.Buildable.BuildTime));
            } else {
                _name.text = abilityInfo.ID;
                _description.text = abilityInfo.Description;
                _basicResourceCostContainer.SetActive(false);
                _advancedResourceCostContainer.SetActive(false);
                _buildTimeContainer.SetActive(false);
            }
        }
        
        private void ToggleTooltip(bool toggle) {
            _view.SetActive(toggle);
        }

        private void SelectedEntityBuildQueueUpdated(List<BuildAbility> buildAbilities) {
            if (_selectedTargetableAbility == null && _selectedTargetableAbilitySlotBehavior == null) {
                SetUpEntityView(_selectedObject);
            }
        }
    }
}