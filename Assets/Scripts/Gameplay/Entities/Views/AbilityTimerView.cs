using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// Visualizes an <see cref="AbilityTimer"/>
    /// </summary>
    public class AbilityTimerView : MonoBehaviour {
        public Image TimerFill;
        public Gradient FillGradient;
        
        private AbilityTimer _timer;

        public void Instantiate(AbilityTimer timer) {
            _timer = timer;
            UpdateFillAmount();
        }

        private void Update() {
            UpdateFillAmount();
        }

        private void UpdateFillAmount() {
            float remaining = _timer.TimeRemaining01;
            TimerFill.fillAmount = remaining;
            TimerFill.color = FillGradient.Evaluate(remaining);

            if (remaining <= 0) {
                Destroy(gameObject);
            }
        }
    }
}