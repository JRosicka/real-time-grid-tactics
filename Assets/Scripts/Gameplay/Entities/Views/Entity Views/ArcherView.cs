using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherView : GridEntityParticularView {
        public GridEntityView EntityView;

        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case AttackAbility attackAbility:
                    DoAttackAnimation(attackAbility.Performer, attackAbility.AbilityParameters.Target);
                    return false;
                case TargetAttackAbility targetAttackAbility:
                    DoAttackAnimation(targetAttackAbility.Performer, targetAttackAbility.AbilityParameters.Target);
                    return false;
                default:
                    return true;
            }
        }

        private void DoAttackAnimation(GridEntity performer, GridEntity target) {
            if (target.DeadOrDying) return;

            Vector2Int? attackLocation = performer.Location;
            Vector2Int? targetLocation = target.Location;

            if (attackLocation == null || targetLocation == null) return;
            EntityView.SetFacingDirection(attackLocation.Value, targetLocation.Value);

            GameManager.Instance.GameAudio.EntityAttackSound(performer.EntityData);
        }
    }
}