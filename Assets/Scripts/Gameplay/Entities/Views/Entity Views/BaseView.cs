using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class BaseView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return ability.AbilityData switch {
                _ => true
            };
        }
    }
}