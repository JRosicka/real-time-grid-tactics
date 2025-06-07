using Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// <see cref="IAbilitySlotBehavior"/> for a cancel button
    /// </summary>
    public abstract class CancelAbilitySlotBehavior : IAbilitySlotBehavior {
        private static GameConfiguration GameConfiguration => GameManager.Instance.Configuration;
        
        public bool IsAvailabilitySensitiveToResources => false;
        public bool CaresAboutQueuedAbilities => true;
        public bool IsAbilityTargetable => false;

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            // Nothing to do
        }
        
        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = GameConfiguration.CancelButtonSprite;
            secondaryAbilityImage.gameObject.SetActive(false);
        }

        public abstract bool AnyPlayerCanSelect { get; }
        public abstract AbilitySlotInfo AbilitySlotInfo { get; }
        public abstract bool CaresAboutAbilityChannels { get; }
        public abstract void SelectSlot();
        public abstract AbilitySlot.AvailabilityResult GetAvailability();
    }
}