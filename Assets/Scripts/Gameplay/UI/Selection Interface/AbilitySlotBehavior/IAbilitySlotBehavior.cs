using Gameplay.Config.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Specifies generic behavior for an <see cref="AbilitySlot"/> to be able to perform
    /// </summary>
    public interface IAbilitySlotBehavior {
        AbilitySlotInfo AbilitySlotInfo { get; }
        /// <summary>
        /// Whether the associated ability's availability can be modified by a change in resources
        /// </summary>
        bool IsAvailabilitySensitiveToResources { get; }
        /// <summary>
        /// Whether the associated ability's availability can be modified by a change in ability timers
        /// </summary>
        bool CaresAboutAbilityChannels { get; }
        /// <summary>
        /// Whether the associated ability's availability can be modified by a change in queued abilities
        /// </summary>
        bool CaresAboutQueuedAbilities { get; }
        /// <summary>
        /// Whether the associated ability is targetable
        /// </summary>
        bool IsAbilityTargetable { get; }
        /// <summary>
        /// We just received user input to use the associated ability - do it
        /// </summary>
        void SelectSlot();
        /// <summary>
        /// We just received user input to select the slot, but we can not select it
        /// </summary>
        void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability);
        /// <summary>
        /// Whether the associated ability is currently available
        /// </summary>
        AbilitySlot.AvailabilityResult GetAvailability();
        /// <summary>
        /// Perform implementation-specific UI setup for the given sprites
        /// </summary>
        void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas);
    }
}