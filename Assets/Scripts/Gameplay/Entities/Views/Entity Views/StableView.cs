using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class StableView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case BuildAbilityData _:
                    DoBuildAnimation();
                    return false;
                default:
                    return true;
            }
        }
        
        private void DoBuildAnimation() {
            Debug.Log(nameof(DoBuildAnimation));
        }
    }
}