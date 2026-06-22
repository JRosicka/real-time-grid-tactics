using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="AbilityTimerFill"/> for a build ability that ticks up rather than down
    /// </summary>
    public class BuildBarAbilityTimerFill : AbilityTimerFill {
        public Image BarFilling;
        
        public override void UpdateFillAmount01(float amount) {
            float invertedAmount = 1f - amount;
            BarFilling.fillAmount = invertedAmount;
        }
    }
}