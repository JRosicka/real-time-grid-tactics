using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherView : GridEntityParticularView {
        public GridEntityView EntityView;
        
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            switch (ability.AbilityData) {
                case AttackAbilityData _:
                    DoAttackAnimation((AttackAbility) ability);
                    return false;
                default:
                    return true;
            }
        }

        private void DoAttackAnimation(AttackAbility attackAbility) {
            Vector2Int attackLocation = attackAbility.Performer.Location;
            Vector2Int targetLocation = attackAbility.AbilityParameters.Target.Location;

            EntityView.SetFacingDirection(attackLocation, targetLocation);
        }
    }
}