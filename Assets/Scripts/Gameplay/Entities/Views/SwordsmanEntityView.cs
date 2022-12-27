using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class SwordsmanEntityView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityTimer timer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability.AbilityData) {
                case SiegeAbilityData _:
                    CreateTimerView(timer);
                    DoSiegeAnimation();
                    break;
                case BuildAbilityData buildAbility:
                    CreateTimerView(timer);
                    DoBuildAnimation();
                    break;
                default:
                    throw new UnexpectedEntityAbilityException(ability.AbilityData);
            }
        }

        public override void Move(Vector2Int targetCell) {
            Debug.Log(nameof(Move));
        }

        public override void Selected() {
            Debug.Log(nameof(Selected));
        }

        public override void Attack(Vector2Int targetCell) {
            Debug.Log(nameof(Attack));
        }

        public override void AttackReceived() {
            Debug.Log(nameof(AttackReceived));

        }

        public override void Killed() {
            Debug.Log(nameof(Killed));
        }

        private void DoSiegeAnimation() {
            Debug.Log(nameof(DoSiegeAnimation));
        }
        private void DoBuildAnimation() {
            Debug.Log(nameof(DoBuildAnimation));
        }
    }
}