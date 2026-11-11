using Audio;
using Gameplay.Config;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class CatapultView : GridEntityParticularView {
        public Animator CatapultArmAnimator;
        public float DefaultSiegeTime = 3f;

        private GridEntity _gridEntity;

        public override void Initialize(GridEntity entity) {
            _gridEntity = entity;
        }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case CatapultAttackAbility attackAbility:
                    DoAttackAnimation(attackAbility.Performer);
                    return false;
                case SiegeAbility:
                    DoSiegeAnimation(abilityTimer);
                    return false;
                case UnsiegeAbility:
                    DoUnsiegeAnimation(abilityTimer);
                    return false;
                default:
                    return true;
            }
        }
        
        public override void UpgradeApplied(IUpgrade upgrade) { }

        private void DoAttackAnimation(GridEntity performer) {
            if (!performer.IsActive()) return;
            
            // Just handle the arm moving. Rock launching animation handled in <see cref="CatapultRocks">
            CatapultArmAnimator.speed = 1;
            CatapultArmAnimator.Play("CatapultLaunch");
        }

        private void DoSiegeAnimation(AbilityTimer timer) {
            CatapultArmAnimator.speed = DefaultSiegeTime / timer.InitialTimeRemaining;
            CatapultArmAnimator.Play("CatapultSiege");

            GameAudio.Instance.SiegeSound(_gridEntity.Location);
        }
        
        private void DoUnsiegeAnimation(AbilityTimer timer) {
            CatapultArmAnimator.speed = DefaultSiegeTime / timer.InitialTimeRemaining;
            CatapultArmAnimator.Play("CatapultUnsiege");
            
            GameAudio.Instance.SiegeSound(_gridEntity.Location);
        }
    }
}