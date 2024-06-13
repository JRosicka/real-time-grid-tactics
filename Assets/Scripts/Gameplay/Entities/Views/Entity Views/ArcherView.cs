using System;
using System.Threading.Tasks;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherView : GridEntityParticularView {
        public GridEntityView EntityView;
        public ArcherArrow ArrowPrefab;
        
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
            Vector3 attackWorldPosition = GameManager.Instance.GridController.GetWorldPosition(attackLocation);
            Vector3 targetWorldPosition = GameManager.Instance.GridController.GetWorldPosition(targetLocation);

            EntityView.SetFacingDirection(attackLocation, targetLocation);
            
            ArcherArrow arrow = Instantiate(ArrowPrefab, attackWorldPosition, Quaternion.identity, 
                GameManager.Instance.CommandManager.SpawnBucket);
            MoveArrow(arrow, targetWorldPosition);
        }

        private async void MoveArrow(ArcherArrow arrow, Vector3 targetPosition) {
            await Task.Delay(TimeSpan.FromSeconds(1));
            arrow.transform.position = targetPosition;
        }
    }
}