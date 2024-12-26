using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class AmberMineView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;
        
        public override void Initialize(GridEntity entity) {
            IncomeAnimationBehavior.Initialize(entity);
        }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            switch (ability.AbilityData) {
                case IncomeAbilityData data:
                    IncomeAnimationBehavior.DoIncomeAnimation(data);
                    return false;
                default:
                    return true;
            }
        }
    }
}