using System;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Button in the settings sub-menu picker for a particular settings group (e.g. Audio)
    /// </summary>
    public class SettingSubMenuButton : MonoBehaviour {
        [Header("Color transition")] 
        [SerializeField] private bool _enableColorTransition = true;
        [SerializeField] private Color _unselectedColor;
        [SerializeField] private Color _selectedColor;
        [SerializeField] private float _colorTransitionSeconds;
        [SerializeField] private Image _backgroundImage;
        
        [Header("Other references")]
        [SerializeField] private ListenerButton _listenerButton;
        [SerializeField] private CanvasGroup _associatedSubMenu;
        
        public float HeightWorldPosition => transform.position.y;
        public Action<SettingSubMenuButton> Entered;

        /// <summary>
        /// Current color state from unselected (0) to selected (1)
        /// </summary>
        private float _color01;
        private bool _selected;

        public void Initialize() {
            _listenerButton.Entered += OnButtonEntered;
        }

        public void SetSelected(bool selected) {
            _selected = selected;
            _associatedSubMenu?.gameObject.SetActive(selected);
            if (selected && _associatedSubMenu) {
                _associatedSubMenu.alpha = 0;
            }
        }

        private void OnButtonEntered() {
            Entered?.Invoke(this);
        }

        private void Update() {
            UpdateColorTransition();
            UpdateMenuFadeIn();
        }

        private void UpdateColorTransition() {
            if (!_enableColorTransition) return;
            if (_selected && _color01 >= 1) return;
            if (!_selected && _color01 <= 0) return;

            float sign = _selected ? 1 : -1;
            _color01 += sign * (Time.deltaTime / _colorTransitionSeconds);
            _color01 = Mathf.Clamp01(_color01);
            _backgroundImage.color = Color.Lerp(_unselectedColor, _selectedColor, _color01);
        }

        private void UpdateMenuFadeIn() {
            if (!_selected) return;
            if (_color01 >= 1) return;

            _associatedSubMenu.alpha = _color01;
        }
    }
}