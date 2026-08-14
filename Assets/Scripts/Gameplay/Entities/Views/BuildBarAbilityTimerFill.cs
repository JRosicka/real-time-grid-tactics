using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="AbilityTimerFill"/> for a build ability that ticks up rather than down
    /// </summary>
    public class BuildBarAbilityTimerFill : AbilityTimerFill {
        public Image BarFilling;
        public bool TickDown;
        
        public override void UpdateFillAmount01(float amount) {
            float fillAmount = TickDown ? amount : 1f - amount;
            BarFilling.fillAmount = fillAmount;
        }
    }
}