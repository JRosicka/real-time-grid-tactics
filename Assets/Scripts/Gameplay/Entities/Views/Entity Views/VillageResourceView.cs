using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class VillageResourceView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return true;
        }
    }
}