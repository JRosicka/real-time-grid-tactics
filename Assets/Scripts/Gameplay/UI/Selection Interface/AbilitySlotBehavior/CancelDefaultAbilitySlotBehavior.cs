using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.UI {
    /// <summary>
    /// <see cref="CancelAbilitySlotBehavior"/> for when an entity is selected normally (non-build abilities displayed)
    /// </summary>
    public class CancelDefaultAbilitySlotBehavior : CancelAbilitySlotBehavior {
        private static EntitySelectionManager EntitySelectionManager => GameManager.Instance.EntitySelectionManager;
        private static ICommandManager CommandManager => GameManager.Instance.CommandManager;

        private readonly GridEntity _selectedEntity;
        
        public CancelDefaultAbilitySlotBehavior(GridEntity selectedEntity) {
            _selectedEntity = selectedEntity;
        }
        
        public override AbilitySlotInfo AbilitySlotInfo => new AbilitySlotInfo("Cancel", "Cancels the current command");
        public override bool CaresAboutAbilityChannels => false;

        public override void SelectSlot() {
            // Deselect the selected targetable ability if there is one
            if (EntitySelectionManager.DeselectTargetableAbility()) {
                return;
            }

            // Otherwise cancel all selected entity's in-progress/queued abilities if there are any
            List<IAbility> cancelableAbilities = _selectedEntity.GetCancelableAbilities();
            if (cancelableAbilities.Count > 0) {
                cancelableAbilities.ForEach(a => CommandManager.CancelAbility(a));
                    
                // Update the rally point
                var currentLocation = _selectedEntity.Location;
                if (currentLocation != null) {
                    // The location might be null if the entity is being destroyed 
                    _selectedEntity.SetTargetLocation(currentLocation.Value, null);
                }
            }
        }

        public override AbilitySlot.AvailabilityResult GetAvailability() {
            if (EntitySelectionManager.IsTargetableAbilitySelected() 
                    || _selectedEntity.GetCancelableAbilities().Count > 0) {
                return AbilitySlot.AvailabilityResult.Selectable;
            }

            if (_selectedEntity.Abilities.Any(a => a.SlotLocation != AbilitySlotLocation.Unpicked)) {
                // There are displayed abilities, we just don't have any queued or set up right now
                return AbilitySlot.AvailabilityResult.Unselectable;
            }

            return AbilitySlot.AvailabilityResult.Hidden;
        }
    }
}