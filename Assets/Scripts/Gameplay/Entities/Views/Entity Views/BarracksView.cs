using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class BarracksView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return ability.AbilityData switch {
                _ => true
            };
        }
    }
}