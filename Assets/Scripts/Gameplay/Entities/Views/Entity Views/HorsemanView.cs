using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class HorsemanView : GridEntityParticularView {
        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return ability.AbilityData switch {
                _ => true
            };
        }
    }
}