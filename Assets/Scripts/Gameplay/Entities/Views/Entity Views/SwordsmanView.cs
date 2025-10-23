using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class SwordsmanView : GridEntityParticularView {
        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return ability.AbilityData switch {
                _ => true
            };
        }
    }
}