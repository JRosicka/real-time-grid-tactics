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

        private GridEntity _selectedEntity;
        private ITargetableAbilityData _selectedTargetableAbility;
        private IAbilitySlotBehavior _selectedTargetableAbilitySlotBehavior;

        public void ToggleForEntity(GridEntity entity) {
            if (_selectedEntity != null) {
                _selectedEntity.BuildQueue.BuildQueueUpdated -= SelectedEntityBuildQueueUpdated;
            }
            _selectedEntity = entity;
            ToggleTooltip(entity != null);
            if (entity != null) {
                _selectedEntity.BuildQueue.BuildQueueUpdated += SelectedEntityBuildQueueUpdated;
                SetUpEntityView(entity);
            }
        }
        
        public void ToggleForTargetableAbility(ITargetableAbilityData ability, IAbilitySlotBehavior abilitySlotBehavior) {
            if (ability == null) {
                _selectedTargetableAbility = null;
                _selectedTargetableAbilitySlotBehavior = null;

                // No ability selected, so go back to showing the selected entity if we have one
                ToggleForEntity(_selectedEntity);
            } else {
                ToggleTooltip(true);
                SetUpAbilityView(ability, abilitySlotBehavior, false);
                _selectedTargetableAbility = ability;
                _selectedTargetableAbilitySlotBehavior = abilitySlotBehavior;
            }
        }

        public void ToggleForHoveredAbility(IAbilityData ability, IAbilitySlotBehavior abilitySlotBehavior) {
            if (ability == null) {
                // No ability hovered, so go back to showing the selected ability if we have one
                if (_selectedTargetableAbility != null) {
                    ToggleForTargetableAbility(_selectedTargetableAbility, _selectedTargetableAbilitySlotBehavior);
                } else {
                    // No ability selected, so go back to showing the selected entity if we have one
                    ToggleForEntity(_selectedEntity);
                }
            } else {
                ToggleTooltip(true);
                SetUpAbilityView(ability, abilitySlotBehavior, abilitySlotBehavior is QueuedBuildAbilitySlotBehavior);
            }
        }
        
        private void SetUpEntityView(GridEntity entity) {
            List<BuildAbility> buildQueue = entity.BuildQueue.Queue;
            if (!entity.EntityData.IsStructure && buildQueue.Count > 0 && !entity.QueuedAbilities.Select(a => a.UID).Contains(buildQueue[0].UID)) {
                // This is a non-structure builder that is actively building something. Show that. 
                SetUpForInProgressBuild(entity.BuildQueue.Queue[0], entity);
                return;
            }
            
            EntityData entityData = entity.EntityData;
            
            _icon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            _secondaryIcon.sprite = entityData.TeamColorSprite;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.Team);
            _secondaryIcon.color = player != null ? player.Data.TeamColor : Color.clear;
            _secondaryIcon.gameObject.SetActive(true);
            _teamColorsCanvas.sortingOrder = 1;
            
            _name.text = entityData.ID;
            _description.text = entityData.Description;
            
            _basicResourceCostContainer.SetActive(false);
            _advancedResourceCostContainer.SetActive(false);
        }

        private void SetUpForInProgressBuild(BuildAbility buildAbility, GridEntity entity) {
            BuildAbilityData buildData = (BuildAbilityData) buildAbility.AbilityData;
            BuildAbilitySlotBehavior buildBehavior = new BuildAbilitySlotBehavior(buildData, buildAbility.AbilityParameters.Buildable, entity);
            SetUpAbilityView(buildData, buildBehavior, true);
        }

        private void SetUpAbilityView(IAbilityData ability, IAbilitySlotBehavior abilitySlotBehavior, bool includeInProgressMessage) {
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
            } else {
                _name.text = ability.ID;
                _description.text = ability.Description;
                _basicResourceCostContainer.SetActive(false);
                _advancedResourceCostContainer.SetActive(false);
            }
        }
        
        private void ToggleTooltip(bool toggle) {
            _view.SetActive(toggle);
        }

        private void SelectedEntityBuildQueueUpdated(List<BuildAbility> buildAbilities) {
            if (_selectedTargetableAbility == null && _selectedTargetableAbilitySlotBehavior == null) {
                SetUpEntityView(_selectedEntity);
            }
        }
    }
}