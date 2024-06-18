using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class VillageView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;

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