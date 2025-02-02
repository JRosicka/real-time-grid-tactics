using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="AbilityTimerFill"/> for a timer whose fill is represented by an image (of type Filled) with an
    /// adjustable fill amount. 
    /// </summary>
    public class ImageFillTimerFill : AbilityTimerFill {
        public Image FillImage;
        public float MaxFillAmount;
        
        public override void UpdateFillAmount01(float amount) {
            float invertedAmount = 1f - amount;
            FillImage.fillAmount = invertedAmount * MaxFillAmount;
        }
    }
}