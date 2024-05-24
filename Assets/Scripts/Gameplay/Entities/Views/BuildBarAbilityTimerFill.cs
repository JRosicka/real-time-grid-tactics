using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="AbilityTimerFill"/> for a build ability that ticks up rather than down
    /// </summary>
    public class BuildBarAbilityTimerFill : AbilityTimerFill {
        public RectTransform BarFilling;
        public float MaxFillAmount;
        
        public override void UpdateFillAmount01(float amount) {
            float invertedAmount = 1f - amount;
            BarFilling.sizeDelta = new Vector2(invertedAmount * MaxFillAmount, BarFilling.sizeDelta.y);
        }
    }
}