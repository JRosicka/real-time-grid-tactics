using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A setting slider for sliding a number value for a setting. Just the view portion plus tracking state.
    /// Also includes a text field component. 
    /// </summary>
    public class SettingSlider : MonoBehaviour {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_InputField _textField;
        
        private int _value;

        public event Action<int> ValueChanged;

        public void Initialize(int initialValue) {
            SetValue(initialValue, false, true, true);
        }

        public void SliderUpdated() {
            SetValue((int) _slider.value, true, false, true);
        }

        public void TextFieldUpdated() {
            if (!int.TryParse(_textField.text, out var newValue)) {
                newValue = 0;
            }
            
            newValue = Mathf.Clamp(newValue, 0, 100);
            SetValue(newValue, true, true, true);
        }

        private void SetValue(int newValue, bool notify, bool updateSlider, bool updateTextField) {
            int oldValue = _value;
            _value = newValue;
            if (updateTextField) {
                _textField.text = newValue.ToString();
            }
            if (updateSlider) {
                _slider.value = newValue;
            }

            if (notify && oldValue != newValue) {
                ValueChanged?.Invoke(newValue);
            }
        }
    }
}