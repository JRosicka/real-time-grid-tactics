using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
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

        private GridEntity _selectedEntity;
        private ITargetableAbilityData _selectedTargetableAbility;
        private IAbilitySlotBehavior _selectedTargetableAbilitySlotBehavior;

        public void ToggleForEntity(GridEntity entity) {
            _selectedEntity = entity;
            ToggleTooltip(entity != null);
            if (entity != null) {
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
                SetUpAbilityView(ability, abilitySlotBehavior);
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
                SetUpAbilityView(ability, abilitySlotBehavior);
            }
        }
        
        private void SetUpEntityView(GridEntity entity) {
            EntityData entityData = entity.EntityData;
            
            _icon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            _secondaryIcon.sprite = entityData.TeamColorSprite;
            _secondaryIcon.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            _secondaryIcon.gameObject.SetActive(true);
            _teamColorsCanvas.sortingOrder = 1;
            
            _name.text = entityData.ID;
            _description.text = entityData.Description;
        }

        private void SetUpAbilityView(IAbilityData ability, IAbilitySlotBehavior abilitySlotBehavior) {
            abilitySlotBehavior.SetUpSprites(_icon, _secondaryIcon, _teamColorsCanvas);
            _name.text = ability.ID;
            _description.text = ability.Description;
        }
        
        private void ToggleTooltip(bool toggle) {
            _view.SetActive(toggle);
        }
    }
}