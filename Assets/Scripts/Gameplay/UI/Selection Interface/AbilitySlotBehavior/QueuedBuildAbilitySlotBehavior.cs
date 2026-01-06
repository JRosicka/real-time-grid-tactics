using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.UI {
    /// <summary>
    /// Handles behavior of an <see cref="AbilitySlot"/> specifically for a queued build ability
    /// </summary>
    public class QueuedBuildAbilitySlotBehavior : BuildAbilitySlotBehavior {
        private readonly BuildAbility _buildAbility;
        
        public QueuedBuildAbilitySlotBehavior(BuildAbility buildAbility, GridEntity selectedEntity)
                : base((BuildAbilityData)buildAbility.AbilityData, buildAbility.AbilityParameters.Buildable, selectedEntity) {
            _buildAbility = buildAbility;
        }

        public override void SelectSlot(bool newlySelected) {
            if (newlySelected) {
                GameManager.Instance.GameAudio.ButtonClickSound();
            }

            SelectedEntity.BuildQueue.CancelBuild(_buildAbility, GameManager.Instance.LocalTeam);
        }

        public override AbilitySlot.AvailabilityResult GetAvailability() {
            return _buildAbility != null 
                ? AbilitySlot.AvailabilityResult.Selectable 
                : AbilitySlot.AvailabilityResult.Unselectable;
        }
    }
}