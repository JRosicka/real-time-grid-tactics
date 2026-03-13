using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Menu {
    /// <summary>
    /// Displays the current color and allows the player to change it
    /// </summary>
    public class ColorPicker : MonoBehaviour {
        [SerializeField] private Image _colorDisplay;
        [SerializeField] private GameObject _editableIcon;
        [SerializeField] private GameObject _colorPickerMenu;
        [SerializeField] private List<Image> _colorOptions;
        
        private List<PlayerColorData> _availableColors;
        private bool _locked;
        private int _slotIndex;
        private PlayerSlot _playerSlot;

        public void Initialize(List<PlayerColorData> availableColors, PlayerSlot playerSlot, int slotIndex) {
            _colorPickerMenu.SetActive(false);
            _slotIndex = slotIndex;
            _playerSlot = playerSlot;
            _availableColors = availableColors;
            
            for (int i = 0; i < availableColors.Count; i++) {
                _colorOptions[i].sprite = availableColors[i].ColoredButtonData.Normal;
            }
        }
        
        public void SetCanChangeColor(bool canChangeColor) {
            _locked = !canChangeColor;
            if (_locked) {
                _colorPickerMenu.SetActive(false);
            }
            _editableIcon.SetActive(canChangeColor);
        }
        
        public void ToggleColorPickerMenu() {
            if (_locked) return;
            _colorPickerMenu.SetActive(!_colorPickerMenu.activeInHierarchy);
        }
        
        public void SetColor(PlayerColorData colorData) {
            _colorDisplay.sprite = colorData.ColoredButtonData.Normal;
        }

        public void TryPickColor(int index) {
            if (_locked) return;
            PlayerColorData colorData = _availableColors[index];
            _playerSlot.AssignedPlayer.CmdSetColor(colorData.ID, -1, false);
            
            ToggleColorPickerMenu();
        }
    }
}