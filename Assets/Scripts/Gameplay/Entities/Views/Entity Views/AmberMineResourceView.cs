using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class AmberMineResourceView : GridEntityParticularView {
        public override void Initialize(GridEntity entity) { }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return true;
        }
    }
}