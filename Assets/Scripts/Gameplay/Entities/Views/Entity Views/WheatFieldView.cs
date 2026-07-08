using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;

namespace Gameplay.Entities {
    public class WheatFieldView : GridEntityParticularView {
        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return true;
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }
    }
}