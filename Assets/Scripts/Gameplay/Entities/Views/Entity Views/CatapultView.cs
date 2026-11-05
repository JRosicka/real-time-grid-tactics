using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class CatapultView : GridEntityParticularView {
        public GridEntityView EntityView;
        public Animator CatapultArmAnimator;

        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case CatapultAttackAbility attackAbility:
                    DoAttackAnimation(attackAbility.Performer);
                    return false;
                default:
                    return true;
            }
        }
        
        public override void UpgradeApplied(IUpgrade upgrade) { }

        private void DoAttackAnimation(GridEntity performer) {
            if (!performer.IsActive()) return;
            
            // Just handle the arm moving. Rock launching animation handled in <see cref="CatapultRocks">
            CatapultArmAnimator.Play("CatapultLaunch");
        }
    }
}