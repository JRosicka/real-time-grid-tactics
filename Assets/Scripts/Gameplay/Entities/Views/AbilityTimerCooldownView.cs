using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// Visualizes an <see cref="AbilityCooldownTimer"/>
    /// </summary>
    public class AbilityTimerCooldownView : MonoBehaviour {
        public Image TimerFill;
        public Gradient FillGradient;
        
        private AbilityCooldownTimer _cooldownTimer;
        private bool _destroyWhenExpired;

        public void Initialize(AbilityCooldownTimer cooldownTimer, bool destroyWhenExpired) {
            gameObject.SetActive(true);
            _cooldownTimer = cooldownTimer;
            _destroyWhenExpired = destroyWhenExpired;
            UpdateFillAmount();
        }

        private void Update() {
            UpdateFillAmount();
        }

        private void UpdateFillAmount() {
            if (_cooldownTimer == null) return;
            
            float remaining = _cooldownTimer.TimeRemaining01;
            TimerFill.fillAmount = remaining;
            TimerFill.color = FillGradient.Evaluate(remaining);

            if (remaining <= 0) {
                if (_destroyWhenExpired) {
                    Destroy(gameObject);
                } else {
                    gameObject.SetActive(false);
                    _cooldownTimer = null;
                }
            }
        }
    }
}