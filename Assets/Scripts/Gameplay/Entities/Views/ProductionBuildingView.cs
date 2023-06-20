using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ProductionBuildingView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case BuildAbilityData _:
                    CreateTimerView(cooldownTimer);
                    DoBuildAnimation();
                    break;
                default:
                    DoGenericAbility(ability, cooldownTimer);
                    break;
            }
        }
        
        public override void Selected() {
            Debug.Log(nameof(Selected));
        }
        public override void AttackReceived() {
            Debug.Log(nameof(AttackReceived));
        }
        public override void Killed() {
            Debug.Log(nameof(Killed));
            KillAnimationFinished();
        }
        private void DoBuildAnimation() {
            Debug.Log(nameof(DoBuildAnimation));
        }
    }
}