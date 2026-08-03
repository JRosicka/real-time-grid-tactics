using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// View for a single option within a <see cref="SettingDropdownList"/>
    /// </summary>
    public class SettingDropdownOption : MonoBehaviour {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private GameObject _selectedIcon;
        
        private int _index;
        private string _optionText;
        private Action<int, string> _onOptionSelected;

        public void Initialize(int index, string optionText, Action<int, string> onOptionSelected, bool selected) {
            _index = index;
            _optionText = optionText;
            _text.text = optionText;
            _onOptionSelected = onOptionSelected;
            
            SetSelectedIcon(selected);
        }
        
        public void SetSelectedIcon(bool selected) {
            _selectedIcon.SetActive(selected);
        }
        
        public void OnOptionSelected() {
            _onOptionSelected(_index, _optionText);
        }
    }
}