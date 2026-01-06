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
        public bool CaresAboutInProgressAbilities => true;
        public bool CaresAboutLeaderPosition => false;
        public bool IsAbilityTargetable => false;

        public void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability) {
            // Nothing to do
        }
        
        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, AbilitySlotBackgroundView abilitySlotBackground) {
            abilityImage.sprite = GameConfiguration.CancelButtonSprite;
            secondaryAbilityImage.gameObject.SetActive(false);
            if (abilitySlotBackground) {
                abilitySlotBackground.SetUpSlot(GameConfiguration.CancelButtonSlotSprites);
            }
        }

        public void SetUpTimerView() {
            // No timer to show
        }

        public void ClearTimerView() {
            // No timer to clear
        }

        public abstract bool AnyPlayerCanSelect { get; }
        public abstract AbilitySlotInfo AbilitySlotInfo { get; }
        public abstract bool CaresAboutAbilityChannels { get; }
        public abstract void SelectSlot(bool newlySelected);
        public abstract AbilitySlot.AvailabilityResult GetAvailability();
    }
}