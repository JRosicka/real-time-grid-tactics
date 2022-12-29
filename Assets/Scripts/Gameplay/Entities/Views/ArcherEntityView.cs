using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherEntityView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log(nameof(DoAbility));
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