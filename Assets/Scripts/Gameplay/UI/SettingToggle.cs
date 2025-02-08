using System;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// A view for a setting flag. Just the view portion plus tracking state.
    /// </summary>
    public class SettingToggle : MonoBehaviour {
        [SerializeField] private GameObject _checkBoxFilling;
        
        private bool _value;

        public event Action<bool> ValueChanged;

        public void Initialize(bool initialValue) {
            SetValue(initialValue, false);
        }

        public void ToggleValue() {
            SetValue(!_value, true);
        }
        
        private void SetValue(bool value, bool notify) {
            _value = value;
            _checkBoxFilling.SetActive(_value);
            if (notify) {
                ValueChanged?.Invoke(_value);
            }
        }
    }
}