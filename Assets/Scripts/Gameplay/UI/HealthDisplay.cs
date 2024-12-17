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
                _target.HPHandler.HPChangedEvent -= UpdateHealth;
            }

            _target = entity;
            _target.HPHandler.HPChangedEvent += UpdateHealth;
            UpdateHealth();
        }

        private void UpdateHealth() {
            HealthField.text = $"{_target.HPHandler.CurrentHP} / {_target.MaxHP}";
        }
    }
}