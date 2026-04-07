using System;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// A button for displaying the state of a control group and allowing the user to interact with that group 
    /// </summary>
    public class ControlGroupButton : MonoBehaviour {
        public int ControlGroup;
        public Color SelectedIconColor;
        public Color DeselectedIconColor;
        public Vector2 IconUpPosition;
        public Vector2 IconDownPosition;
        
        [Header("References")]
        public Image EntityIcon;
        public Image EntityColorsIcon;
        public TMP_Text HotkeyText;
        public ListenerButton SlotButton;
        public GameObject IconsGroup;

        public event Action ControlGroupUpdated;

        private ControlGroupsManager _controlGroupsManager;
        
        private void Start() {
            SlotButton.Pressed += SelectControlGroup;
            SlotButton.PressedViewLogic += ToggleClicked;
            SlotButton.NoLongerPressedViewLogic += ToggleUnClicked;
        }

        public void Initialize(ControlGroupsManager controlGroupsManager) {
            _controlGroupsManager = controlGroupsManager;
            HotkeyText.text = ControlGroup.ToString();
            
            EntityIcon.gameObject.SetActive(false);

            ControlGroup controlGroup = controlGroupsManager.GetControlGroup(ControlGroup);
            controlGroup.ControlGroupAssigned += AssignControlGroup;
            controlGroup.ControlGroupUnassigned += ClearControlGroup;
        }

        private void AssignControlGroup(GridEntity gridEntity) {
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(gridEntity)?.ColorData;
            
            EntityIcon.sprite = gridEntity.EntityData.BaseSprite;
            EntityColorsIcon.sprite = gridEntity.EntityData.TeamColorSprite;
            EntityColorsIcon.color = colorData!.TeamColor;

            float scale = gridEntity.EntityData.IconScaleInSelectionInterface;
            EntityIcon.transform.localScale = new Vector3(scale, scale, 1);

            EntityIcon.gameObject.SetActive(true);
        }

        private void ClearControlGroup() {
            // Reset the button icon state in case the button was in mid-press
            IconsGroup.transform.localPosition = IconUpPosition;
            EntityIcon.color = DeselectedIconColor;
            EntityIcon.transform.localScale = new Vector3(1, 1, 1);
            
            EntityIcon.gameObject.SetActive(false);

            ControlGroupUpdated?.Invoke();
        }
        
        public void ControlGroupHotkeyPressed(bool pressed) {
            if (pressed) {
                SlotButton.ActivateOnPointerDownView(new PointerEventData(EventSystem.current) {
                    button = PointerEventData.InputButton.Left, 
                });
            } else {
                SlotButton.ActivateOnPointerUpView(new PointerEventData(EventSystem.current) {
                    button = PointerEventData.InputButton.Left
                });
            }
        }
        
        #region Listeners

        private void SelectControlGroup() {
            _controlGroupsManager.SelectControlGroup(ControlGroup, true, false);
        }
        
        private void ToggleClicked() {
            IconsGroup.transform.localPosition = IconDownPosition;
            EntityIcon.color = SelectedIconColor;
        }
        
        private void ToggleUnClicked() {
            IconsGroup.transform.localPosition = IconUpPosition;
            EntityIcon.color = DeselectedIconColor;
        }
        
        #endregion
    }
}