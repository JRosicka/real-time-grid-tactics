using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class AmberMineView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;
        
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case IncomeAbilityData _:
                    IncomeAnimationBehavior.DoIncomeAnimation();
                    return false;
                default:
                    return true;
            }
        }
    }
}