using Gameplay.Entities;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Displays current/max HP of a <see cref="GridEntity"/>.
    /// </summary>
    public class HealthDisplay : MonoBehaviour {
        public TMP_Text HealthField;

        private GridEntity _target;

        public void SetTarget(GridEntity entity) {
            if (_target != null) {
                // Remove event listener from old target if assigned
                _target.HPHandler.CurrentHP.ValueChanged -= UpdateHealth;
            }

            _target = entity;
            _target.HPHandler.CurrentHP.ValueChanged += UpdateHealth;
            UpdateHealth(0, _target.HPHandler.CurrentHP.Value, null);
        }

        private void UpdateHealth(int oldValue, int newValue, object metadata) {
            HealthField.text = $"{newValue} / {_target.MaxHP}";
        }
    }
}