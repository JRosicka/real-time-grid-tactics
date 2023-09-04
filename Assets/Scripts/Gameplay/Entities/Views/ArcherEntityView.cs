using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherEntityView : GridEntityViewBase {
        public override void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log(nameof(DoAbility));
            DoGenericAbility(ability);
        }
        
        public override void Selected() {
            Debug.Log(nameof(Selected));
        }
        
        public override void AttackReceived() {
            Debug.Log(nameof(AttackReceived));
        }

        public override void Killed() {
            Debug.Log(nameof(Killed));
        }
    }
}