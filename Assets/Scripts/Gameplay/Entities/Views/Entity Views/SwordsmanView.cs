using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class SwordsmanView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
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