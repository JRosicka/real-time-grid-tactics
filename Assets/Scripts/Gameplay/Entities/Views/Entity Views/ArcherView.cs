using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherView : GridEntityParticularView {
        public GridEntityView EntityView;
        
        public override void Initialize(GridEntity entity) { }

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
            if (attackAbility.AbilityParameters.Target.DeadOrDying) return;
            
            Vector2Int? attackLocation = attackAbility.Performer.Location;
            Vector2Int? targetLocation = attackAbility.AbilityParameters.Target.Location;

            if (attackLocation == null || targetLocation == null) return;
            EntityView.SetFacingDirection(attackLocation.Value, targetLocation.Value);
        }
    }
}