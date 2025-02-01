using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Setting view for displaying/choosing an option from a dropdown list. Just the view portion plus tracking state.
    /// </summary>
    public class SettingDropdownList : MonoBehaviour {
        /// <summary>
        /// Handles capturing clicks outside of the dropdown list which are used to trigger dismissing the list
        /// </summary>
        [SerializeField] private GameObject _clickBlocker;
        [SerializeField] private SettingDropdownOption _dropdownOptionPrefab;
        [SerializeField] private GameObject _dropdownList;
        [SerializeField] private TMP_Text _selectedOptionText;
        
        private int _value;
        private readonly List<SettingDropdownOption> _options = new List<SettingDropdownOption>();
        
        public event Action<int> ValueChanged;
        
        public void Initialize(int initialValue, List<string> optionTexts) {
            // Construct options
            for(int i = 0; i < optionTexts.Count; i++) {
                SettingDropdownOption option = Instantiate(_dropdownOptionPrefab, _dropdownOptionPrefab.transform.parent);
                option.Initialize(i, optionTexts[i], SelectOption, i == initialValue);
                option.gameObject.SetActive(true);
                _options.Add(option);
            }
            _dropdownOptionPrefab.gameObject.SetActive(false);
            
            SetValue(initialValue, optionTexts[initialValue], false);
        }

        public void DisplayList() {
            _dropdownList.SetActive(true);
            _clickBlocker.SetActive(true);
        }

        public void DismissList() {
            _clickBlocker.SetActive(false);
            _dropdownList.SetActive(false);
        }

        public void SelectOption(int index, string optionText) {
            SetValue(index, optionText, true);

            for (int i = 0; i < _options.Count; i++) {
                _options[i].SetSelectedIcon(i == index);
            }
        }

        private void SetValue(int newValue, string optionText, bool notify) {
            int oldValue = _value;
            _value = newValue;
            _selectedOptionText.text = optionText;
            
            DismissList();

            if (notify && oldValue != newValue) {
                ValueChanged?.Invoke(newValue);
            }
        }
    }
}