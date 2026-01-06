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
        /// Whether the associated ability's availability can be modified by a change in in-progress abilities
        /// </summary>
        bool CaresAboutInProgressAbilities { get; }
        bool CaresAboutLeaderPosition { get; }
        /// <summary>
        /// Whether the associated ability is targetable
        /// </summary>
        bool IsAbilityTargetable { get; }
        /// <summary>
        /// Whether this slot can be selected by any player, not just the owner
        /// </summary>
        /// <returns></returns>
        bool AnyPlayerCanSelect { get; }
        /// <summary>
        /// We just received user input to use the associated ability - do it
        /// </summary>
        void SelectSlot(bool newlySelected);
        /// <summary>
        /// We just received user input to select the slot, but we can not select it. Only gets called the first frame
        /// when holding down the hotkey for this.
        /// </summary>
        void HandleFailedToSelect(AbilitySlot.AvailabilityResult availability);
        /// <summary>
        /// Whether the associated ability is currently available
        /// </summary>
        AbilitySlot.AvailabilityResult GetAvailability();
        /// <summary>
        /// Perform implementation-specific UI setup for the given sprites
        /// </summary>
        void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, AbilitySlotBackgroundView abilitySlotBackground);
        /// <summary>
        /// Set up the cooldown timer view for this slot's associated ability if there is one that should be displayed
        /// </summary>
        void SetUpTimerView();
        /// <summary>
        /// Teardown logic for this slot's ability cooldown timer view
        /// </summary>
        void ClearTimerView();
    }
}