using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="AbilityTimerFill"/> for a circular visual that ticks down
    /// </summary>
    public class CircleAbilityTimerFill : AbilityTimerFill {
        public Image TimerFill;
        public Gradient FillGradient;
        
        public override void UpdateFillAmount01(float amount) {
            TimerFill.fillAmount = amount;
            TimerFill.color = FillGradient.Evaluate(amount);
        }
    }
}