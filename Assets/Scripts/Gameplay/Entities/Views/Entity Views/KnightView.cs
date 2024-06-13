using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class KnightView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            switch (ability.AbilityData) {
                case SiegeAbilityData _:
                    DoSiegeAnimation();
                    return false;
                default:
                    return true;
            }
        }
        
        private void DoSiegeAnimation() {
            Debug.Log(nameof(DoSiegeAnimation));
        }
    }
}