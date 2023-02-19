using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ResourceBuildingView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case IncomeAbilityData _:
                    CreateTimerView(cooldownTimer);
                    DoIncomeAnimation();
                    break;
                default:
                    throw new UnexpectedEntityAbilityException(ability.AbilityData);
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
        private void DoIncomeAnimation() {
            Debug.Log(nameof(DoIncomeAnimation));
        }
    }
}