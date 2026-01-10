using System.Collections.Generic;
using System.Linq;
using Audio;
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
        
        public override bool AnyPlayerCanSelect => false;
        public override AbilitySlotInfo AbilitySlotInfo => new AbilitySlotInfo("Cancel", "Cancels the current command");
        public override bool CaresAboutAbilityChannels => false;

        public override void SelectSlot(bool newlySelected) {
            if (newlySelected) {
                GameAudio.Instance.ButtonClickDownSound();
            }
            
            // Deselect the selected targetable ability if there is one
            if (EntitySelectionManager.DeselectTargetableAbility()) {
                return;
            }

            // Otherwise cancel all selected entity's in-progress abilities if there are any
            _selectedEntity.CancelAllAbilities();
        }

        public override AbilitySlot.AvailabilityResult GetAvailability() {
            if (!_selectedEntity.InteractBehavior!.IsLocalTeam) {
                return AbilitySlot.AvailabilityResult.Hidden;
            }
            
            if (EntitySelectionManager.IsTargetableAbilitySelected() 
                    || _selectedEntity.GetCancelableAbilities().Count > 0
                    || _selectedEntity.HoldingPosition) {
                return AbilitySlot.AvailabilityResult.Selectable;
            }

            if (_selectedEntity.Abilities.Any(a => a.SlotLocation != AbilitySlotLocation.Unpicked)) {
                // There are displayed abilities, we just don't have any in progress or set up right now
                return AbilitySlot.AvailabilityResult.Unselectable;
            }

            return AbilitySlot.AvailabilityResult.Hidden;
        }
    }
}