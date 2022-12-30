using Gameplay.Entities;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Displays current/max HP of a <see cref="GridEntity"/>
    /// </summary>
    public class HealthBar : MonoBehaviour {
        public TMP_Text HealthField;
        public RectTransform Bar;

        private GridEntity _target;
        private float _maxWidth;

        public void Start() {
            _maxWidth = Bar.sizeDelta.x;
        }

        public void SetTarget(GridEntity entity) {
            if (_target != null) {
                // Remove event listener from old target if assigned
                _target.HPChangedEvent -= UpdateHealth;
            }

            _target = entity;
            _target.HPChangedEvent += UpdateHealth;
            UpdateHealth();
        }

        private void UpdateHealth() {
            HealthField.text = $"{_target.CurrentHP} / {_target.MaxHP}";
            Bar.sizeDelta = new Vector2((float)_target.CurrentHP / _target.MaxHP * _maxWidth, Bar.sizeDelta.y);
        }
    }
}