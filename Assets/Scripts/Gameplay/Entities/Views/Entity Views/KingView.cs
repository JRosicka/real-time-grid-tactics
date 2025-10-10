using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class KingView : GridEntityParticularView {
        [SerializeField] private Animator _paradeTextAnimator;
        
        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case ParadeAbilityData _:
                    DoParadeAnimation();
                    return false;
                default:
                    return true;
            }
        }

        private void DoParadeAnimation() {
            _paradeTextAnimator.Play("ParadeActive");
        }
    }
}