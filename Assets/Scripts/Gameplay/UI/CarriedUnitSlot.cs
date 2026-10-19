using Gameplay.Config;
using Gameplay.Entities;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// A slot for displaying a carried unit (or an empty slot) 
    /// </summary>
    public class CarriedUnitSlot : MonoBehaviour {
        public Color SelectedIconColor;
        public Color DeselectedIconColor;
        public Vector2 IconUpPosition;
        public Vector2 IconDownPosition;
        
        [Header("References")]
        public Image EntityIcon;
        public Image EntityColorsIcon;
        public ListenerButton SlotButton;
        public GameObject IconsGroup;
        
        private CarriedUnitsInterface _carriedUnitsInterface;
        [CanBeNull] private GridEntity _entity;
        
        private void Start() {
            SlotButton.Pressed += SelectSlot;
            SlotButton.PressedViewLogic += ToggleClicked;
            SlotButton.NoLongerPressedViewLogic += ToggleUnClicked;
        }

        public void Initialize(CarriedUnitsInterface carriedUnitsInterface, GridEntity gridEntity) {
            _carriedUnitsInterface = carriedUnitsInterface;
            _entity = gridEntity;
            
            if (gridEntity == null) {
                ClearEntity();
                return;
            }
            
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(gridEntity)?.ColorData;
            
            EntityIcon.sprite = gridEntity.EntityData.BaseSpriteIconOverride != null ? gridEntity.EntityData.BaseSpriteIconOverride : gridEntity.EntityData.BaseSprite;
            EntityColorsIcon.sprite = gridEntity.EntityData.TeamColorSpriteIconOverride != null ? gridEntity.EntityData.TeamColorSpriteIconOverride : gridEntity.EntityData.TeamColorSprite;
            EntityColorsIcon.color = colorData!.TeamColor;

            float scale = gridEntity.EntityData.IconScaleInSelectionInterface;
            EntityIcon.transform.localScale = new Vector3(scale, scale, 1);

            EntityIcon.gameObject.SetActive(true);
        }

        private void ClearEntity() {
            // Reset the button icon state in case the button was in mid-press
            if (IconsGroup) {
                IconsGroup.transform.localPosition = IconUpPosition;
            }
            if (EntityIcon) {
                EntityIcon.color = DeselectedIconColor;
                EntityIcon.transform.localScale = new Vector3(1, 1, 1);
                EntityIcon.gameObject.SetActive(false);
            }
        }
        
        #region Listeners

        private void SelectSlot() {
            _carriedUnitsInterface.SelectUnitToDropOff(_entity);
        }
        
        private void ToggleClicked() {
            if (IconsGroup) {
                IconsGroup.transform.localPosition = IconDownPosition;
            }
            if (EntityIcon) {
                EntityIcon.color = SelectedIconColor;
            }
        }
        
        private void ToggleUnClicked() {
            if (IconsGroup) {
                IconsGroup.transform.localPosition = IconUpPosition;
            }
            if (EntityIcon) {
                EntityIcon.color = DeselectedIconColor;
            }
        }
        
        #endregion
    }
}