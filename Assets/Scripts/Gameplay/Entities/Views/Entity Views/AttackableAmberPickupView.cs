using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class AttackableAmberPickupView : GridEntityParticularView {
        [SerializeField] private Animator _animator;
        
        public override void Initialize(GridEntity entity) {
            _animator.Play("Resource Pickup Idle", -1, Random.value);
        }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return true;
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }
    }
}