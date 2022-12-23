using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherEntityView : GridEntityViewBase {
        public override void DoAbilityAnimation(IAbility ability) {
            Debug.Log(nameof(DoAbilityAnimation));
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
    }
}