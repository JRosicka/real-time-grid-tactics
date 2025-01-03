using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.BuildQueue;

namespace Gameplay.UI {
    /// <summary>
    /// <see cref="CancelAbilitySlotBehavior"/> for when the build menu is open
    /// </summary>
    public class CancelBuildAbilitySlotBehavior : CancelAbilitySlotBehavior {
        private static SelectionInterface SelectionInterface => GameManager.Instance.SelectionInterface;

        private readonly GridEntity _selectedEntity;
        private readonly bool _canMoveOutOfBuildMenu;
        private readonly string _cancelButtonDescription;

        public CancelBuildAbilitySlotBehavior(GridEntity selectedEntity, bool canMoveOutOfBuildMenu) {
            _selectedEntity = selectedEntity;
            _canMoveOutOfBuildMenu = canMoveOutOfBuildMenu;
            _cancelButtonDescription = canMoveOutOfBuildMenu ? "Leave the build menu" : "Cancel the last build in the queue";
        }
        
        public override AbilitySlotInfo AbilitySlotInfo => new AbilitySlotInfo("Cancel", _cancelButtonDescription);
        public override bool CaresAboutAbilityChannels => true;

        public override void SelectSlot() {
            // Move out of a nested build menu if we are in one
            if (_canMoveOutOfBuildMenu) {
                SelectionInterface.DeselectBuildAbility();
                return;
            }

            IBuildQueue buildQueue = GetBuildQueueIfWeCanCancelSomethingInIt();
            buildQueue?.CancelBuild(buildQueue.Queue.Last());
        }

        public override AbilitySlot.AvailabilityResult GetAvailability() {
            if (_canMoveOutOfBuildMenu) {
                return AbilitySlot.AvailabilityResult.Selectable;
            }
            
            IBuildQueue buildQueue = GetBuildQueueIfWeCanCancelSomethingInIt();
            return buildQueue != null 
                ? AbilitySlot.AvailabilityResult.Selectable 
                : AbilitySlot.AvailabilityResult.Unselectable;
        }

        /// <summary>
        /// Get the build queue of the selected entity, but only if we have the authority to cancel entries in it and if
        /// it has any entries in it.
        /// </summary>
        private IBuildQueue GetBuildQueueIfWeCanCancelSomethingInIt() {
            if (_selectedEntity == null || _selectedEntity.InteractBehavior is not { IsLocalTeam: true }) return null;
            
            // Otherwise clear the last build in the selected entity's build queue if there is one
            IBuildQueue buildQueue = _selectedEntity.BuildQueue;
            if (buildQueue != null && buildQueue.Queue.Count > 0) {
                return buildQueue;
            }

            return null;
        }
    }
}