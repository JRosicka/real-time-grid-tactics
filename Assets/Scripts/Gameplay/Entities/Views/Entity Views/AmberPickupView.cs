using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class AmberPickupView : GridEntityParticularView {
        [SerializeField] private Animator _animator;
        
        public override void Initialize(GridEntity entity) {
            _animator.Play("Resource Pickup Idle", -1, Random.value);
        }
        public override void LethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return true;
        }
    }
}