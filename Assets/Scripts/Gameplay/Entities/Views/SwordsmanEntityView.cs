using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class SwordsmanEntityView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case SiegeAbilityData _:
                    CreateTimerView(cooldownTimer);
                    DoSiegeAnimation();
                    break;
                case MoveAbilityData moveAbility:
                    CreateTimerView(cooldownTimer);
                    DoMoveAnimation();
                    break;
                case AttackAbilityData attackAbility:
                    CreateTimerView(cooldownTimer);
                    AttackReceived();
                    break;
                default:
                    Debug.LogWarning($"Unexpected entity ability: {ability.AbilityData}");
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
        private void DoSiegeAnimation() {
            Debug.Log(nameof(DoSiegeAnimation));
        }
        private void DoMoveAnimation() {
            Debug.Log(nameof(DoMoveAnimation));
        }
    }
}